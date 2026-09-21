using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using bothtech.Shared.Models;
using Newtonsoft.Json;

namespace bothtech.Shared.Services
{
    public class SyncService
    {
        private readonly HttpClient _http;
        private readonly DatabaseService _dbService;

        private const string FirebaseUrl = "https://bothtech-4c076-default-rtdb.firebaseio.com/products.json";

        public SyncService(HttpClient http, DatabaseService dbService)
        {
            _http = http;
            _dbService = dbService;
        }

        public async Task SyncProductsSilentlyAsync()
        {
            try
            {
                var response = await _http.GetAsync(FirebaseUrl);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Erreur Firebase: {response.StatusCode}");
                    return;
                }

                string rawJson = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(rawJson) || rawJson == "null")
                {
                    return;
                }

                List<Product> firebaseProducts = new List<Product>();

                // --- CONFIGURATION INVINCIBLE ---
                // Si une erreur de conversion survient (ex: nombre trop grand), on l'ignore et on continue
                var settings = new JsonSerializerSettings
                {
                    Error = (sender, args) =>
                    {
                        Console.WriteLine($"⚠️ Erreur ignorée sur {args.ErrorContext.Member} : {args.ErrorContext.Error.Message}");
                        args.ErrorContext.Handled = true;
                    },
                    NullValueHandling = NullValueHandling.Ignore
                };

                // On parse directement avec la protection activée
                try
                {
                    // Tentative 1 : Format Dictionnaire (format naturel de Firebase)
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, Product>>(rawJson, settings);
                    if (dict != null)
                    {
                        foreach (var kvp in dict)
                        {
                            if (kvp.Value != null)
                            {
                                var product = kvp.Value;
                                product.Id = kvp.Key;
                                firebaseProducts.Add(product);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Tentative 2 : Format Tableau (si Firebase a indexé les produits 0, 1, 2...)
                    var list = JsonConvert.DeserializeObject<List<Product>>(rawJson, settings);
                    if (list != null)
                    {
                        foreach (var product in list)
                        {
                            if (product != null)
                            {
                                if (string.IsNullOrEmpty(product.Id))
                                    product.Id = Guid.NewGuid().ToString();

                                firebaseProducts.Add(product);
                            }
                        }
                    }
                }

                if (firebaseProducts.Any())
                {
                    await _dbService.UpdateLocalDatabaseAsync(firebaseProducts);
                    Console.WriteLine($"✅ Synchro Firebase réussie : {firebaseProducts.Count} produits chargés !");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur globale Synchro : {ex.Message}");
            }
        }
    }
}
//using System.Net.Http.Json;
//using bothtech.Shared.Models;

//namespace bothtech.Shared.Services
//{
//    public class SyncService
//    {
//        private readonly HttpClient _http;
//        private readonly DatabaseService _dbService;

//        private const string FirebaseUrl = "https://bothtech-4c076-default-rtdb.firebaseio.com/products.json";

//        public SyncService(HttpClient http, DatabaseService dbService)
//        {
//            _http = http;
//            _dbService = dbService;
//        }

//        public async Task SyncProductsSilentlyAsync()
//        {
//            try
//            {
//                // On utilise GetAsync pour pouvoir lire le code d'erreur HTTP si ça échoue
//                var response = await _http.GetAsync(FirebaseUrl);

//                if (!response.IsSuccessStatusCode)
//                {
//                    Console.WriteLine($"Firebase a bloqué la requête. Code d'erreur : {response.StatusCode}");
//                    return;
//                }

//                var firebaseProductsDict = await response.Content.ReadFromJsonAsync<Dictionary<string, Product>>();

//                if (firebaseProductsDict != null && firebaseProductsDict.Any())
//                {
//                    var firebaseProducts = new List<Product>();

//                    // On parcourt le dictionnaire pour assigner manuellement la clé Firebase à notre propriété 'Id'
//                    foreach (var kvp in firebaseProductsDict)
//                    {
//                        if (kvp.Value != null)
//                        {
//                            var product = kvp.Value;
//                            product.Id = kvp.Key; // CRUCIAL : On sauvegarde l'ID Firebase
//                            firebaseProducts.Add(product);
//                        }
//                    }

//                    // Mise à jour de SQLite
//                    await _dbService.UpdateLocalDatabaseAsync(firebaseProducts);
//                    Console.WriteLine($"Synchro réussie : {firebaseProducts.Count} produits récupérés.");
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Mode hors-ligne actif. Synchronisation reportée. Erreur: {ex.Message}");
//            }
//        }
//    }
//}