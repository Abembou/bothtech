using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using bothtech.Shared.Models;

namespace bothtech.Shared.Services
{
    public class FirebaseService
    {
        private readonly string _databaseUrl = "https://bothtech-4c076-default-rtdb.firebaseio.com";
        private readonly DataSyncService _dataSync;

        // ✅ INJECTION DE DATASYNC : FirebaseService devient un relais Offline-First !
        public FirebaseService(DataSyncService dataSync)
        {
            _dataSync = dataSync;
        }

        // ======================================================================
        // GESTION DES UTILISATEURS (Seule méthode qui reste Online, car appelée au login)
        // ======================================================================
        public async Task<(string Role, string Phone, string Address)> EnsureUserExistsInDatabaseAsync(string uid, string email, string name, string idToken)
        {
            string url = $"{_databaseUrl}/users/{uid}.json?auth={idToken}";
            using var httpClient = new HttpClient();

            var response = await httpClient.GetAsync(url);
            string jsonResponse = await response.Content.ReadAsStringAsync();

            if (jsonResponse == "null")
            {
                var newUser = new
                {
                    email = email.ToLower(),
                    name = name,
                    role = "client",
                    active = true,
                    createdAt = DateTime.UtcNow.ToString("o")
                };

                var content = new StringContent(JsonSerializer.Serialize(newUser), Encoding.UTF8, "application/json");
                var putResponse = await httpClient.PutAsync(url, content);

                if (!putResponse.IsSuccessStatusCode)
                {
                    string error = await putResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Erreur lors de la création du profil : {error}");
                }
                return ("client", "", "");
            }
            else
            {
                using var doc = JsonDocument.Parse(jsonResponse);
                string role = doc.RootElement.TryGetProperty("role", out var roleProp) ? (roleProp.GetString() ?? "client") : "client";
                string phone = doc.RootElement.TryGetProperty("phone", out var phoneProp) ? (phoneProp.GetString() ?? "") : "";
                string address = doc.RootElement.TryGetProperty("address", out var addrProp) ? (addrProp.GetString() ?? "") : "";
                return (role, phone, address);
            }
        }

        // ==========================================
        // CATALOGUE (PRODUITS) : Lecture Instantanée SQLite
        // ==========================================
        public Task<List<Product>> GetAllProductsAsync() => GetProductsAsync();

        public Task<List<Product>> GetProductsAsync()
        {
            // LECTURE OFFLINE : On retourne immédiatement le contenu SQLite
            var products = _dataSync.GetAllLocal<Product>();
            return Task.FromResult(products);
        }

        public Task AddProductAsync(Product product, string idToken = "")
        {
            _dataSync.SaveLocal(product);
            _dataSync.EnqueueOperation("Product", product.Id, "CREATE", product);
            if (!string.IsNullOrEmpty(idToken)) _ = _dataSync.PushPendingOperationsAsync(idToken);
            return Task.CompletedTask;
        }

        public Task UpdateProductAsync(string productId, Product product, string idToken = "")
        {
            _dataSync.SaveLocal(product);
            _dataSync.EnqueueOperation("Product", productId, "UPDATE", product);
            if (!string.IsNullOrEmpty(idToken)) _ = _dataSync.PushPendingOperationsAsync(idToken);
            return Task.CompletedTask;
        }

        public Task DeleteProductAsync(string productId, string idToken = "")
        {
            _dataSync.DeleteLocal<Product>(productId);
            _dataSync.EnqueueOperation<Product>("Product", productId, "DELETE", null);
            if (!string.IsNullOrEmpty(idToken)) _ = _dataSync.PushPendingOperationsAsync(idToken);
            return Task.CompletedTask;
        }

        // ==========================================
        // COMMANDES (ORDERS) : Synchronisation SQLite
        // ==========================================
        public Task<Dictionary<string, OrderModel>> GetOrdersAsync()
        {
            var orders = _dataSync.GetAllLocal<OrderModel>().ToDictionary(o => o.Id, o => o);
            return Task.FromResult(orders);
        }

        public Task<bool> PushOrderAsync(OrderModel order)
        {
            if (string.IsNullOrEmpty(order.Id)) order.Id = Guid.NewGuid().ToString("N");
            _dataSync.SaveLocal(order);
            _dataSync.EnqueueOperation("OrderModel", order.Id, "CREATE", order);
            _ = _dataSync.PushPendingOperationsAsync("");
            return Task.FromResult(true);
        }

        public Task AddOrderAsync(OrderModel order, string idToken)
        {
            _dataSync.SaveLocal(order);
            _dataSync.EnqueueOperation("OrderModel", order.Id, "CREATE", order);
            if (!string.IsNullOrEmpty(idToken)) _ = _dataSync.PushPendingOperationsAsync(idToken);
            return Task.CompletedTask;
        }

        public Task UpdateOrderStatusAndAgentAsync(string orderId, string status, string agentName, string idToken = "")
        {
            // Mise à jour visuelle locale immédiate
            var localOrder = _dataSync.GetAllLocal<OrderModel>().FirstOrDefault(o => o.Id == orderId);
            if (localOrder != null)
            {
                localOrder.Status = status;
                // Ajustez si votre propriété C# s'appelle différemment (ex: TraitePar, Agent, etc.)
                // localOrder.TraitePar = agentName; 
                _dataSync.SaveLocal(localOrder);
            }

            var payload = new Dictionary<string, object> { { "status", status }, { "traitePar", agentName } };
            _dataSync.EnqueueOperation("OrderModel", orderId, "UPDATE", payload);

            if (!string.IsNullOrEmpty(idToken)) _ = _dataSync.PushPendingOperationsAsync(idToken);
            return Task.CompletedTask;
        }

        // ==========================================
        // PROFIL & ÉQUIPE
        // ==========================================
        public Task<bool> UpdateUserProfileAsync(string uid, string email, string phone, string address, string idToken)
        {
            var payload = new Dictionary<string, object> { { "email", email.ToLower() }, { "phone", phone }, { "address", address } };
            _dataSync.EnqueueOperation("StaffModel", uid, "UPDATE", payload);
            if (!string.IsNullOrEmpty(idToken)) _ = _dataSync.PushPendingOperationsAsync(idToken);
            return Task.FromResult(true);
        }

        public Task<List<StaffModel>> GetStaffMembersAsync(string idToken = "")
        {
            var staff = _dataSync.GetAllLocal<StaffModel>()
                                 .Where(s => s.Role != "client" && s.Role != "detaillant")
                                 .ToList();
            return Task.FromResult(staff);
        }

        public Task AddStaffMemberAsync(StaffModel staff, string idToken = "")
        {
            _dataSync.SaveLocal(staff);
            _dataSync.EnqueueOperation("StaffModel", staff.Id, "CREATE", staff);
            if (!string.IsNullOrEmpty(idToken)) _ = _dataSync.PushPendingOperationsAsync(idToken);
            return Task.CompletedTask;
        }

        public Task UpdateStaffMemberAsync(string id, StaffModel staff, string idToken = "")
        {
            _dataSync.SaveLocal(staff);
            _dataSync.EnqueueOperation("StaffModel", id, "UPDATE", staff);
            if (!string.IsNullOrEmpty(idToken)) _ = _dataSync.PushPendingOperationsAsync(idToken);
            return Task.CompletedTask;
        }

        // ==========================================
        // AVIS & CHAT
        // ==========================================
        public Task AddProductReviewAsync(string productId, Review review, string idToken = "")
        {
            // On met à jour le chemin spécifique dans Firebase
            _dataSync.EnqueueOperation("Product", $"{productId}/reviews/{review.Id}", "UPDATE", review);
            if (!string.IsNullOrEmpty(idToken)) _ = _dataSync.PushPendingOperationsAsync(idToken);
            return Task.CompletedTask;
        }

        public Task UpdateProductReviewAsync(string productId, string reviewId, Review review, string idToken = "")
        {
            _dataSync.EnqueueOperation("Product", $"{productId}/reviews/{reviewId}", "UPDATE", review);
            if (!string.IsNullOrEmpty(idToken)) _ = _dataSync.PushPendingOperationsAsync(idToken);
            return Task.CompletedTask;
        }

        public Task DeleteProductReviewAsync(string productId, string reviewId, string idToken = "")
        {
            _dataSync.EnqueueOperation<Review>("Product", $"{productId}/reviews/{reviewId}", "DELETE", null);
            if (!string.IsNullOrEmpty(idToken)) _ = _dataSync.PushPendingOperationsAsync(idToken);
            return Task.CompletedTask;
        }

        public Task SendChatMessageAsync(string clientId, object chatMessage, string idToken)
        {
            string msgId = Guid.NewGuid().ToString("N");
            _dataSync.EnqueueOperation("chats", $"{clientId}/messages/{msgId}", "UPDATE", chatMessage);
            if (!string.IsNullOrEmpty(idToken)) _ = _dataSync.PushPendingOperationsAsync(idToken);
            return Task.CompletedTask;
        }

        public Task<Dictionary<string, ChatSession>> GetAllChatsAsync(string idToken)
        {
            var chats = _dataSync.GetAllLocal<ChatSession>().ToDictionary(c => c.Id, c => c);
            return Task.FromResult(chats);
        }
    }
}