using System;
using System.Collections.Generic;
using System.Linq;
using bothtech.Shared.Models;

namespace bothtech.Shared.Services
{
    public class CartService
    {
        // Liste des articles actuellement dans le panier
        public List<OrderItem> CartItems { get; private set; } = new List<OrderItem>();

        // Événement déclenché à chaque modification (pour mettre à jour l'interface instantanément)
        public event Action OnCartChanged;

        public string AddToCart(Product product, string userId = null)
        {
            if (product.Quantity <= 0) return "Rupture de stock.";

            // Vérifier si le produit est déjà dans le panier
            var existingItem = CartItems.FirstOrDefault(i => i.ProductFirebaseId == product.Id);

            if (existingItem != null)
            {
                // Vérification du stock avant d'augmenter la quantité
                if (existingItem.Quantity + 1 > product.Quantity)
                    return $"Désolé, limite de stock atteinte ({product.Quantity}).";

                existingItem.Quantity += 1;
            }
            else
            {
                // Calcul du prix final (prend en compte le prix B2B s'il existe et si on veut l'appliquer plus tard)
                int finalPrice = product.Price;
                if (product.OldPrice.HasValue && product.OldPrice < product.Price)
                {
                    // Sécurité anti-erreur de saisie (si ancien prix < nouveau prix)
                    finalPrice = product.Price;
                }

                // Ajout d'un nouvel article
                CartItems.Add(new OrderItem
                {
                    ProductFirebaseId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = finalPrice,
                    Quantity = 1
                });
            }

            NotifyStateChanged();
            return $"{product.Name} ajouté au panier !";
        }

        public void RemoveFromCart(string productId)
        {
            var item = CartItems.FirstOrDefault(i => i.ProductFirebaseId == productId);
            if (item != null)
            {
                CartItems.Remove(item);
                NotifyStateChanged();
            }
        }

        // NOUVELLE MÉTHODE : Pour les boutons + et - du menu
        public void UpdateQuantity(string productId, int change)
        {
            var item = CartItems.FirstOrDefault(i => i.ProductFirebaseId == productId);
            if (item != null)
            {
                int newQuantity = item.Quantity + change;

                // Si on descend à 0 ou moins, on supprime l'article
                if (newQuantity <= 0)
                {
                    CartItems.Remove(item);
                }
                else
                {
                    item.Quantity = newQuantity;
                }

                NotifyStateChanged();
            }
        }

        public int GetTotal() => CartItems.Sum(i => i.TotalPrice);

        public int GetItemCount() => CartItems.Sum(i => i.Quantity);

        // Déclenche l'événement pour dire à Blazor de rafraîchir l'écran
        private void NotifyStateChanged() => OnCartChanged?.Invoke();
    }
}