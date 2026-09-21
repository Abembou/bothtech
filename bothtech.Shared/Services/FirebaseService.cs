using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using bothtech.Shared.Models;

namespace bothtech.Shared.Services
{
    public class FirebaseService
    {
        private readonly FirebaseClient _firebase;

        public FirebaseService()
        {
            // URL mise à jour avec la base de données de votre projet bothtech-4c076
            _firebase = new FirebaseClient("https://bothtech-4c076-default-rtdb.firebaseio.com/");
        }

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
                // CORRECTION : On assigne la clé générée par Firebase (-Pxxx) à notre propriété 'Id'
                return products.Select(p =>
                {
                    p.Object.Id = p.Key;
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

                // CORRECTION : On utilise 'Id' au lieu de 'FirebaseId' pour les commandes aussi
                order.Id = result.Key; // On récupère l'ID généré en ligne

                // Si la commande contient des articles, on peut les lier ou mettre à jour le stock en ligne ici

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur Firebase (PushOrder) : {ex.Message}");
                return false;
            }
        }
    }
}