using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SQLite;
using bothtech.Shared.Models; // Pour accéder à StaffModel, SyncOperation, etc.

namespace bothtech.Shared.Services
{
    public class DataSyncService
    {
        private readonly SQLiteConnection _sqliteDb;

        // ✅ DÉCLARATION DU VERROU (Indispensable pour éviter les blocages SQLite)
        private static readonly object _dbLock = new object();

        // ✅ CONSTRUCTEUR
        public DataSyncService()
        {
            // 👇 Changez "v5" en "v6" ici pour forcer une nouvelle base de données propre
            string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "bothtech_offline_v7.db3");
            _sqliteDb = new SQLiteConnection(dbPath);

            try
            {
                lock (_dbLock)
                {
                    _sqliteDb.CreateTable<StaffModel>();
                    _sqliteDb.CreateTable<SyncOperation>();
                    _sqliteDb.CreateTable<StockMovement>();
                    _sqliteDb.CreateTable<UserB2BModel>();
                    _sqliteDb.CreateTable<RoleConfig>();
                    _sqliteDb.CreateTable<CompanySettings>();
                    _sqliteDb.CreateTable<ExpenseModel>();
                    _sqliteDb.CreateTable<Product>();
                    _sqliteDb.CreateTable<OrderModel>(); // 👈 Cette table sera enfin créée !
                    _sqliteDb.CreateTable<ChatSession>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur création de table SQLite : {ex.Message}");
            }
        }

        // =========================================================
        // LECTURE / ÉCRITURE LOCALE RAPIDE
        // =========================================================

        public List<T> GetAllLocal<T>() where T : new()
        {
            lock (_dbLock)
            {
                return _sqliteDb.Table<T>().ToList();
            }
        }

        public void SaveLocal<T>(T data) where T : new()
        {
            lock (_dbLock)
            {
                _sqliteDb.InsertOrReplace(data);
            }
        }

        public void DeleteLocal<T>(object primaryKey) where T : new()
        {
            lock (_dbLock)
            {
                _sqliteDb.Delete<T>(primaryKey);
            }
        }

        // =========================================================
        // OUTBOX : MISE EN FILE D'ATTENTE DES OPÉRATIONS
        // =========================================================

        public void EnqueueOperation<T>(string entityType, string entityId, string operationType, T payloadObject)
        {
            try
            {
                string jsonPayload = "";
                if (payloadObject != null)
                {
                    var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    jsonPayload = JsonSerializer.Serialize(payloadObject, options);
                }

                var operation = new SyncOperation
                {
                    OperationId = Guid.NewGuid().ToString("N"),
                    EntityType = entityType,
                    EntityId = entityId,
                    OperationType = operationType,
                    Payload = jsonPayload,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Pending",
                    RetryCount = 0
                };

                lock (_dbLock)
                {
                    _sqliteDb.Insert(operation);
                }

                System.Diagnostics.Debug.WriteLine($"[OUTBOX] Opération {operationType} sur {entityType} mise en file d'attente.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OUTBOX ERROR] Impossible de mettre en file d'attente : {ex.Message}");
            }
        }

        // =========================================================
        // MOTEUR D'ENVOI (SYNC ENGINE) : PUSH VERS FIREBASE
        // =========================================================

        public async Task PushPendingOperationsAsync(string idToken)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return;

            List<SyncOperation> pendingOps;

            lock (_dbLock)
            {
                pendingOps = _sqliteDb.Table<SyncOperation>()
                    .Where(o => o.Status == "Pending")
                    .OrderBy(o => o.CreatedAt)
                    .ToList();
            }

            if (!pendingOps.Any()) return;

            using var client = new HttpClient();
            string databaseUrl = "https://bothtech-4c076-default-rtdb.firebaseio.com";

            foreach (var op in pendingOps)
            {
                try
                {
                    string firebaseNode = op.EntityType switch
                    {
                        "StaffModel" => "users",
                        "UserB2BModel" => "users",
                        "OrderModel" => "orders",
                        "Product" => "products",
                        "StockMovement" => "stockMovements",
                        "RoleConfig" => "roles",
                        "CompanySettings" => "settings",
                        "chats" => "chats",
                        "ExpenseModel" => "expenses",
                        _ => op.EntityType.ToLower()
                    };

                    string url = op.EntityType == "CompanySettings"
                        ? $"{databaseUrl}/{firebaseNode}.json?auth={idToken}"
                        : $"{databaseUrl}/{firebaseNode}/{op.EntityId}.json?auth={idToken}";

                    string safePayload = string.IsNullOrEmpty(op.Payload) ? "{}" : op.Payload;
                    var content = new StringContent(safePayload, System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = null;

                    // CORRECTION : Utilisation de HttpRequestMessage pour garantir la compatibilité PATCH en Blazor WASM
                    if (op.OperationType == "CREATE" || op.OperationType == "UPDATE")
                    {
                        var request = new HttpRequestMessage(new HttpMethod("PATCH"), url) { Content = content };
                        response = await client.SendAsync(request);
                    }
                    else if (op.OperationType == "DELETE")
                    {
                        response = await client.DeleteAsync(url);
                    }

                    if (response != null && response.IsSuccessStatusCode)
                    {
                        op.Status = "Synced";
                        lock (_dbLock) { _sqliteDb.Update(op); }
                        System.Diagnostics.Debug.WriteLine($"[SYNC SUCCESS] {op.OperationType} sur {op.EntityId}");
                    }
                    else
                    {
                        op.RetryCount++;
                        lock (_dbLock) { _sqliteDb.Update(op); }
                        string errorMsg = response != null ? await response.Content.ReadAsStringAsync() : "Pas de réponse";
                        System.Diagnostics.Debug.WriteLine($"[SYNC FIREBASE ERROR] {errorMsg}");
                    }
                }
                catch (Exception ex)
                {
                    op.RetryCount++;
                    lock (_dbLock) { _sqliteDb.Update(op); }
                    System.Diagnostics.Debug.WriteLine($"[SYNC FAILED] {ex.Message}");
                }
            }
        }

        // =========================================================
        // MOTEUR D'ASPIRATION : PULL DEPUIS FIREBASE
        // =========================================================

        public async Task PullProductsFromFirebaseAsync()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return;

            try
            {
                string url = "https://bothtech-4c076-default-rtdb.firebaseio.com/products.json";
                using var client = new HttpClient();
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(json) && json != "null")
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                        };

                        var firebaseProducts = JsonSerializer.Deserialize<Dictionary<string, Product>>(json, options);

                        if (firebaseProducts != null)
                        {
                            lock (_dbLock)
                            {
                                foreach (var kvp in firebaseProducts)
                                {
                                    var product = kvp.Value;
                                    if (product != null)
                                    {
                                        product.Id = kvp.Key;
                                        _sqliteDb.InsertOrReplace(product);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PULL PRODUCTS FAILED] {ex.Message}");
            }
        }

        // CORRECTION ANTI-CRASH INTÉGRÉE ICI
        public async Task PullRolesFromFirebaseAsync(string idToken)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return;

            try
            {
                string url = $"https://bothtech-4c076-default-rtdb.firebaseio.com/roles.json?auth={idToken}";
                using var client = new HttpClient();
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(json) && json != "null")
                    {
                        var firebaseRoles = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                        if (firebaseRoles != null)
                        {
                            lock (_dbLock)
                            {
                                foreach (var roleKvp in firebaseRoles)
                                {
                                    var roleConfig = new RoleConfig
                                    {
                                        Id = roleKvp.Key.ToLower().Trim(),
                                        PermissionsJson = roleKvp.Value.GetRawText() // Stockage de l'arbre brut en texte
                                    };
                                    _sqliteDb.InsertOrReplace(roleConfig);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PULL ROLES FAILED] {ex.Message}");
            }
        }

        public async Task PullStaffFromFirebaseAsync(string idToken)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return;

            try
            {
                string url = $"https://bothtech-4c076-default-rtdb.firebaseio.com/users.json?auth={idToken}";
                using var client = new HttpClient();
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(json) && json != "null")
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var firebaseUsers = JsonSerializer.Deserialize<Dictionary<string, StaffModel>>(json, options);

                        if (firebaseUsers != null)
                        {
                            lock (_dbLock)
                            {
                                foreach (var kvp in firebaseUsers)
                                {
                                    var staff = kvp.Value;
                                    staff.Id = kvp.Key;
                                    _sqliteDb.InsertOrReplace(staff);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PULL STAFF FAILED] {ex.Message}");
            }
        }

        // ✅ NOUVEAU : ASPIRATION DES CLIENTS B2B (Détaillants)
        public async Task PullB2BClientsFromFirebaseAsync(string idToken)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return;

            try
            {
                string url = $"https://bothtech-4c076-default-rtdb.firebaseio.com/users.json?auth={idToken}";
                using var client = new HttpClient();
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(json) && json != "null")
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var firebaseUsers = JsonSerializer.Deserialize<Dictionary<string, UserB2BModel>>(json, options);

                        if (firebaseUsers != null)
                        {
                            lock (_dbLock)
                            {
                                foreach (var kvp in firebaseUsers)
                                {
                                    var user = kvp.Value;
                                    user.Id = kvp.Key;
                                    // On ne garde que les détaillants/clients pour le module de Vente B2B
                                    if (user.Role != null && (user.Role == "detaillant" || user.Role == "client"))
                                    {
                                        _sqliteDb.InsertOrReplace(user);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PULL B2B CLIENTS FAILED] {ex.Message}");
            }
        }

        // ✅ NOUVEAU : ASPIRATION DES COMMANDES (Historique Ventes et Comptoir)
        public async Task PullOrdersFromFirebaseAsync(string idToken)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return;

            try
            {
                string url = $"https://bothtech-4c076-default-rtdb.firebaseio.com/orders.json?auth={idToken}";
                using var client = new HttpClient();
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(json) && json != "null")
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var firebaseOrders = JsonSerializer.Deserialize<Dictionary<string, OrderModel>>(json, options);

                        if (firebaseOrders != null)
                        {
                            lock (_dbLock)
                            {
                                foreach (var kvp in firebaseOrders)
                                {
                                    var order = kvp.Value;
                                    order.Id = kvp.Key;
                                    _sqliteDb.InsertOrReplace(order);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PULL ORDERS FAILED] {ex.Message}");
            }
        }
    }
}