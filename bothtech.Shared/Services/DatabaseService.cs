using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using bothtech.Shared.Models;

namespace bothtech.Shared.Services
{
    public class DatabaseService
    {
        private readonly DataSyncService _dataSync;

        // ✅ INJECTION : On confie tout à DataSyncService pour centraliser le hors-ligne
        public DatabaseService(DataSyncService dataSync)
        {
            _dataSync = dataSync;
        }

        // ============================================================
        // 1. Récupérer le catalogue des produits pour l'affichage
        // ============================================================
        public Task<List<Product>> GetProductsAsync()
        {
            // Lecture instantanée depuis le cache SQLite unifié
            var products = _dataSync.GetAllLocal<Product>();
            return Task.FromResult(products);
        }

        public Task UpdateLocalDatabaseAsync(List<Product> firebaseProducts)
        {
            // Mise à jour de la base de données unifiée
            foreach (var product in firebaseProducts)
            {
                _dataSync.SaveLocal(product);
            }
            return Task.CompletedTask;
        }

        // ============================================================
        // 2. Enregistrer une vente locale (Comptoir) avec mise à jour des stocks
        // ============================================================
        // ✅ CORRECTION 1 : Remplacement de "Order" par "OrderModel"
        // ✅ CORRECTION 2 : Ajout du paramètre "idToken"
        public Task<bool> CreateLocalOrderAsync(OrderModel order, string idToken = "")
        {
            try
            {
                order.Status = "terminee";
                order.Type = "comptoir";
                order.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                if (string.IsNullOrEmpty(order.Id))
                {
                    order.Id = "COMPT-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                }

                // Récupération de tous les produits locaux pour ajuster les stocks
                var localProducts = _dataSync.GetAllLocal<Product>();

                if (order.Items != null)
                {
                    foreach (var item in order.Items)
                    {
                        item.OrderId = order.Id;

                        // 1. Mise à jour du produit
                        var product = localProducts.FirstOrDefault(p => p.Id == item.ProductFirebaseId);
                        if (product != null)
                        {
                            product.Quantity -= item.Quantity;
                            if (product.Quantity < 0) product.Quantity = 0; // Sécurité anti-négatif

                            // Si vous avez cette propriété dans votre modèle
                            // product.SoldQuantity += item.Quantity; 

                            // On sauvegarde le produit dans SQLite
                            _dataSync.SaveLocal(product);

                            // 🔥 CRUCIAL : On met à jour le produit dans Firebase via la file d'attente
                            _dataSync.EnqueueOperation("Product", product.Id, "UPDATE", product);
                        }
                    }
                }

                // 2. Enregistrement de la commande
                _dataSync.SaveLocal(order);

                // 🔥 CRUCIAL : On envoie la commande à Firebase via la file d'attente
                _dataSync.EnqueueOperation("OrderModel", order.Id, "CREATE", order);

                // 3. Déclenchement de la synchronisation en arrière-plan
                // ✅ CORRECTION 3 : On ne lance le Push que si on a un token valide
                if (!string.IsNullOrEmpty(idToken))
                {
                    _ = _dataSync.PushPendingOperationsAsync(idToken);
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la vente : {ex.Message}");
                return Task.FromResult(false);
            }
        }
    }
}