using System.Text;
using System.Text.Json;
using Firebase.Database;
using Firebase.Database.Query;
using bothtech.Shared.Models;

namespace bothtech.Shared.Services
{
    public class FirebaseService
    {
        private readonly FirebaseClient _firebase;
        private readonly string _databaseUrl = "https://bothtech-4c076-default-rtdb.firebaseio.com";

        public FirebaseService()
        {
            // URL de la base de données de votre projet bothtech-4c076
            _firebase = new FirebaseClient($"{_databaseUrl}/");
        }

        // ======================================================================
        // ✅ NOUVEAU : GESTION DES UTILISATEURS (Appelé par AuthService)
        // ======================================================================
        public async Task<(string Role, string Phone, string Address)> EnsureUserExistsInDatabaseAsync(string uid, string email, string name, string idToken)
        {
            string url = $"{_databaseUrl}/users/{uid}.json?auth={idToken}";
            using var httpClient = new HttpClient();

            // 1. Vérifier si l'utilisateur existe en base de données
            var response = await httpClient.GetAsync(url);
            string jsonResponse = await response.Content.ReadAsStringAsync();

            // Si le compte n'existe pas (Firebase renvoie la chaîne "null")
            if (jsonResponse == "null")
            {
                var newUser = new
                {
                    email = email.ToLower(),
                    name = name,
                    role = "client", // Rôle par défaut
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

                // Nouveau compte : On retourne le rôle par défaut, et des chaînes vides pour tél/adresse
                return ("client", "", "");
            }
            else
            {
                // Si le compte existe, on lit son rôle, son téléphone et son adresse
                using var doc = JsonDocument.Parse(jsonResponse);

                string role = doc.RootElement.TryGetProperty("role", out var roleProp) ? (roleProp.GetString() ?? "client") : "client";
                string phone = doc.RootElement.TryGetProperty("phone", out var phoneProp) ? (phoneProp.GetString() ?? "") : "";
                string address = doc.RootElement.TryGetProperty("address", out var addrProp) ? (addrProp.GetString() ?? "") : "";

                return (role, phone, address);
            }
        }
        // ==========================================
        // LECTURE : Récupération du profil complet (Téléphone & Adresse)
        // ==========================================


        // ==========================================
        // LECTURE : Téléchargement du catalogue
        // ==========================================
        public async Task<List<Product>> GetAllProductsAsync()
        {
            try
            {
                var products = await _firebase
                    .Child("products")
                    .OnceAsync<Product>();

                // On transforme le dictionnaire Firebase en Liste classique
                return products.Select(p =>
                {
                    p.Object.Id = p.Key; // On assigne la clé générée par Firebase (-Pxxx)
                    return p.Object;
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur Firebase (GetProducts) : {ex.Message}");
                return new List<Product>();
            }
        }

        // ==========================================
        // ÉCRITURE : Synchronisation des commandes
        // ==========================================
        public async Task<bool> PushOrderAsync(Order order)
        {
            try
            {
                // Envoi de la commande à Firebase
                var result = await _firebase
                    .Child("orders")
                    .PostAsync(order);

                order.Id = result.Key; // On récupère l'ID généré en ligne

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur Firebase (PushOrder) : {ex.Message}");
                return false;
            }
        }
        // ==========================================
        // ÉCRITURE : Mise à jour du profil (Téléphone & Adresse)
        // ==========================================
        public async Task<bool> UpdateUserProfileAsync(string uid, string email, string phone, string address, string idToken)
        {
            try
            {
                string url = $"{_databaseUrl}/users/{uid}.json?auth={idToken}";
                using var httpClient = new HttpClient();

                var updateData = new
                {
                    email = email.ToLower(), // Requis par votre règle de sécurité .validate
                    phone = phone,
                    address = address
                };

                // L'utilisation de PATCH permet d'ajouter/modifier ces champs sans effacer le rôle ou le nom
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(updateData), System.Text.Encoding.UTF8, "application/json");
                var response = await httpClient.PatchAsync(url, content);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur Firebase (UpdateUserProfile) : {ex.Message}");
                return false;
            }
        }
        // ==========================================
        // GESTION DES AVIS PRODUITS (REVIEWS)
        // ==========================================
        public async Task AddProductReviewAsync(string productId, Review review, string idToken = "")
        {
            try
            {
                string url = $"{_databaseUrl}/products/{productId}/Reviews/{review.Id}.json";
                if (!string.IsNullOrEmpty(idToken))
                {
                    url += $"?auth={idToken}";
                }

                using var httpClient = new HttpClient();
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(review), System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PutAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Erreur HTTP {response.StatusCode} : {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur API REST (AddProductReview) : {ex.Message}");
                throw;
            }
        }

        public async Task UpdateProductReviewAsync(string productId, string reviewId, Review review, string idToken = "")
        {
            try
            {
                string url = $"{_databaseUrl}/products/{productId}/Reviews/{reviewId}.json";
                if (!string.IsNullOrEmpty(idToken))
                {
                    url += $"?auth={idToken}";
                }

                using var httpClient = new HttpClient();
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(review), System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PutAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Erreur HTTP {response.StatusCode} : {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur API REST (UpdateProductReview) : {ex.Message}");
                throw;
            }
        }
        public async Task DeleteProductReviewAsync(string productId, string reviewId, string idToken = "")
        {
            try
            {
                string url = $"{_databaseUrl}/products/{productId}/Reviews/{reviewId}.json";
                if (!string.IsNullOrEmpty(idToken))
                {
                    url += $"?auth={idToken}";
                }

                using var httpClient = new HttpClient();
                var response = await httpClient.DeleteAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Erreur HTTP {response.StatusCode} : {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur API REST (DeleteProductReview) : {ex.Message}");
                throw;
            }
        }


        // ==========================================
        // GESTION DU CATALOGUE (PRODUITS)
        // ==========================================

        // 1. Récupérer tous les produits (Lecture publique, pas besoin de Token)
        public async Task<List<Product>> GetProductsAsync()
        {
            try
            {
                string url = $"{_databaseUrl}/products.json";
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return new List<Product>();
                }

                string json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json) || json == "null")
                {
                    return new List<Product>();
                }

                // Firebase renvoie un dictionnaire de produits, on le convertit en Liste
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Product>>(json, options);

                if (dict != null)
                {
                    return dict.Values.ToList();
                }

                return new List<Product>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur API REST (GetProducts) : {ex.Message}");
                return new List<Product>();
            }
        }

        // 2. Ajouter un nouveau produit
        public async Task AddProductAsync(Product product, string idToken = "")
        {
            try
            {
                string url = $"{_databaseUrl}/products/{product.Id}.json";
                if (!string.IsNullOrEmpty(idToken))
                {
                    url += $"?auth={idToken}";
                }

                using var httpClient = new HttpClient();
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(product), System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PutAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Erreur HTTP {response.StatusCode} : {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur API REST (AddProduct) : {ex.Message}");
                throw;
            }
        }

        // 3. Mettre à jour un produit existant (ou Valider le stock)
        public async Task UpdateProductAsync(string productId, Product product, string idToken = "")
        {
            try
            {
                string url = $"{_databaseUrl}/products/{productId}.json";
                if (!string.IsNullOrEmpty(idToken))
                {
                    url += $"?auth={idToken}";
                }

                using var httpClient = new HttpClient();
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(product), System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PutAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Erreur HTTP {response.StatusCode} : {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur API REST (UpdateProduct) : {ex.Message}");
                throw;
            }
        }

        // 4. Supprimer un produit
        public async Task DeleteProductAsync(string productId, string idToken = "")
        {
            try
            {
                string url = $"{_databaseUrl}/products/{productId}.json";
                if (!string.IsNullOrEmpty(idToken))
                {
                    url += $"?auth={idToken}";
                }

                using var httpClient = new HttpClient();
                var response = await httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Erreur HTTP {response.StatusCode} : {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur API REST (DeleteProduct) : {ex.Message}");
                throw;
            }
        }
        // ==========================================
        // GESTION DES COMMANDES (VENTES)
        // ==========================================

        public async Task<Dictionary<string, OrderModel>> GetOrdersAsync()
        {
            try
            {
                string url = $"{_databaseUrl}/orders.json";
                using var client = new HttpClient();
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) return new();

                string json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(json) || json == "null") return new();

                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, OrderModel>>(json, options) ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur API REST (GetOrders) : {ex.Message}");
                return new();
            }
        }

        public async Task UpdateOrderStatusAndAgentAsync(string orderId, string status, string agentName, string idToken = "")
        {
            try
            {
                string url = $"{_databaseUrl}/orders/{orderId}.json";
                if (!string.IsNullOrEmpty(idToken))
                {
                    url += $"?auth={idToken}";
                }

                var data = new Dictionary<string, object>
            {
                { "status", status },
                { "traitePar", agentName }
            };

                using var client = new HttpClient();
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(data), System.Text.Encoding.UTF8, "application/json");

                // Utilisation d'un PUT ou PATCH ciblé sur les propriétés modifiées
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), url) { Content = content };
                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    // Solution de repli avec un PUT global si le PATCH n'est pas pris en charge par votre version
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Erreur HTTP {response.StatusCode} : {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur API REST (UpdateOrderStatus) : {ex.Message}");
                throw;
            }
        }
        // ==========================================
        // AJOUTER UNE NOUVELLE COMMANDE (CLIENT)
        // ==========================================
        public async Task AddOrderAsync(OrderModel order, string idToken)
        {
            try
            {
                string url = $"{_databaseUrl}/orders/{order.Id}.json?auth={idToken}";
                using var client = new HttpClient();

                // 👇 AJOUTEZ CES OPTIONS POUR CONVERTIR "ClientId" EN "clientId"
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };

                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(order, options);
                var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PutAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception(error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception Firebase AddOrder : {ex.Message}");
                throw;
            }
        }
        // ==========================================
        // ENVOI D'UN MESSAGE DANS LE CHAT
        // ==========================================
        public async Task SendChatMessageAsync(string clientId, object chatMessage, string idToken)
        {
            try
            {
                string url = $"{_databaseUrl}/chats/{clientId}/messages.json?auth={idToken}";
                using var client = new HttpClient();

                // 👇 AJOUTEZ LES MÊMES OPTIONS ICI
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };

                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(chatMessage, options);
                var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception(error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur Firebase SendChatMessage : {ex.Message}");
                throw;
            }
        }
        public async Task<Dictionary<string, ChatSession>> GetAllChatsAsync(string idToken)
        {
            try
            {
                string url = $"{_databaseUrl}/chats.json?auth={idToken}";
                using var client = new HttpClient();
                var response = await client.GetAsync(url);

                // Si Firebase bloque, on lève une VRAIE erreur pour la voir à l'écran
                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Erreur Firebase ({response.StatusCode}) : {error}");
                }

                string json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(json) || json == "null") return new();

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };

                return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, ChatSession>>(json, options) ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur GetAllChats : {ex.Message}");
                throw; // On renvoie l'erreur vers la page
            }
        }
        // ==========================================
        // GESTION DE L'ÉQUIPE (RH)
        // ==========================================
        public async Task<List<StaffMemberModel>> GetStaffMembersAsync(string idToken = "")
        {
            try
            {
                // On récupère les utilisateurs depuis le nœud "users"
                string url = $"{_databaseUrl}/users.json";
                if (!string.IsNullOrEmpty(idToken)) url += $"?auth={idToken}";

                using var client = new HttpClient();
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) return new List<StaffMemberModel>();

                string json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(json) || json == "null") return new List<StaffMemberModel>();

                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, StaffMemberModel>>(json, options);

                if (dict != null)
                {
                    var list = new List<StaffMemberModel>();
                    foreach (var kvp in dict)
                    {
                        var staff = kvp.Value;
                        // On s'assure que l'ID correspond à la clé Firebase
                        if (string.IsNullOrEmpty(staff.Id)) staff.Id = kvp.Key;

                        // On filtre pour ne pas afficher les clients normaux dans l'équipe
                        if (staff.Role != "client" && staff.Role != "detaillant")
                        {
                            list.Add(staff);
                        }
                    }
                    return list;
                }
                return new List<StaffMemberModel>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur API REST (GetStaffMembers) : {ex.Message}");
                return new List<StaffMemberModel>();
            }
        }
        public async Task AddStaffMemberAsync(StaffMemberModel staff, string idToken = "")
        {
            try
            {
                string url = $"{_databaseUrl}/users/{staff.Id}.json";
                if (!string.IsNullOrEmpty(idToken)) url += $"?auth={idToken}";

                using var client = new HttpClient();
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(staff), System.Text.Encoding.UTF8, "application/json");

                var response = await client.PutAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Erreur HTTP {response.StatusCode} : {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur API REST (AddStaffMember) : {ex.Message}");
                throw;
            }
        }
        public async Task UpdateStaffMemberAsync(string id, StaffMemberModel staff, string idToken = "")
        {
            try
            {
                string url = $"{_databaseUrl}/users/{id}.json";
                if (!string.IsNullOrEmpty(idToken)) url += $"?auth={idToken}";

                using var client = new HttpClient();
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(staff), System.Text.Encoding.UTF8, "application/json");

                // On utilise PUT pour écraser/mettre à jour les informations de l'employé
                var response = await client.PutAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Erreur HTTP {response.StatusCode} : {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur API REST (UpdateStaffMember) : {ex.Message}");
                throw;
            }
        }
       
    }
}