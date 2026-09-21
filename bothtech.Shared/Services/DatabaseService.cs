using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SQLite;
using bothtech.Shared.Models;

namespace bothtech.Shared.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _db;
        private bool _isInitialized = false;

        public DatabaseService()
        {
            // IMPORTANT : Ne jamais utiliser .Wait() dans le constructeur
        }

        private async Task InitAsync()
        {
            if (_isInitialized && _db != null) return;

            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "bothtech_local.db3");
            _db = new SQLiteAsyncConnection(dbPath);

            // Création asynchrone propre, sans blocage du thread
            await _db.CreateTableAsync<Product>();
            await _db.CreateTableAsync<Order>();
            await _db.CreateTableAsync<OrderItem>();

            _isInitialized = true;
        }

        // ============================================================
        // 1. Récupérer le catalogue des produits pour l'affichage
        // ============================================================
        public async Task<List<Product>> GetProductsAsync()
        {
            await InitAsync();
            return await _db.Table<Product>().ToListAsync();
        }

        public async Task UpdateLocalDatabaseAsync(List<Product> firebaseProducts)
        {
            await InitAsync();

            // 1. Destruction et recréation de la table EN DEHORS de la transaction
            // Cela empêche le crash SQLiteException (Database is locked)
            await _db.DropTableAsync<Product>();
            await _db.CreateTableAsync<Product>();

            // 2. Insertion des nouvelles données en lot
            await _db.RunInTransactionAsync(tran =>
            {
                foreach (var product in firebaseProducts)
                {
                    // InsertOrReplace est la méthode la plus sûre pour éviter les conflits d'ID
                    tran.InsertOrReplace(product);
                }
            });
        }

        // ============================================================
        // 2. Enregistrer une vente locale (Comptoir) avec mise à jour des stocks
        // ============================================================
        public async Task<bool> CreateLocalOrderAsync(Order order)
        {
            try
            {
                await InitAsync();

                order.Status = "terminee";
                order.Type = "comptoir";
                order.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                order.SyncStatus = "pending";

                await _db.RunInTransactionAsync(tran =>
                {
                    tran.Insert(order);
                    var orderId = order.Id;

                    foreach (var item in order.Items)
                    {
                        item.OrderId = orderId;
                        tran.Insert(item);

                        var product = tran.Table<Product>().FirstOrDefault(p => p.Id == item.ProductFirebaseId);

                        if (product != null)
                        {
                            product.Quantity -= item.Quantity;
                            product.SoldQuantity += item.Quantity;
                            tran.Update(product);
                        }
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la vente : {ex.Message}");
                return false;
            }
        }
    }
}