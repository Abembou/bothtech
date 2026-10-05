using SQLite;
using System;
using System.Collections.Generic;

using SQLite;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization; // Requis pour le mapping Firebase

namespace bothtech.Shared.Models; // <-- UNE SEULE DÉCLARATION ICI
                                  // Modèle principal de commande (utilisé pour SQLite et Firebase)
public class Order
    {
        [PrimaryKey]
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("firebaseId")]
        public string FirebaseId { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("createdAt")]
        public long CreatedAt { get; set; }

        [JsonPropertyName("syncStatus")]
        public string SyncStatus { get; set; }

        [JsonPropertyName("clientId")]
        public string ClientId { get; set; }

        [JsonPropertyName("clientName")]
        public string ClientName { get; set; }

        [JsonPropertyName("clientPhone")]
        public string ClientPhone { get; set; }

        [JsonPropertyName("clientAddress")]
        public string ClientAddress { get; set; }

        [JsonPropertyName("paymentMethod")]
        public string PaymentMethod { get; set; }

        [JsonPropertyName("traitePar")]
        public string TraitePar { get; set; }

        [JsonPropertyName("avance")]
        public int Avance { get; set; }

        [JsonPropertyName("reste")]
        public int Reste { get; set; }

        [JsonPropertyName("recipientName")]
        public string RecipientName { get; set; }

        [JsonPropertyName("recipientPhone")]
        public string RecipientPhone { get; set; }

        [JsonPropertyName("recipientAddress")]
        public string RecipientAddress { get; set; }

        [JsonPropertyName("venteId")]
        public string VenteId { get; set; } // ID de la vente générée lors de la conclusion

        [Ignore] // Empêche SQLite de créer une colonne
        [JsonPropertyName("items")] // Permet à Firebase de mapper la liste
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }

    // Modèle DTO (souvent utilisé pour l'interface utilisateur)
    public class OrderModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("clientId")]
        public string ClientId { get; set; } = "";

        [JsonPropertyName("userId")]
        public string UserId { get; set; } = "";

        [JsonPropertyName("clientName")]
        public string ClientName { get; set; } = "";

        [JsonPropertyName("recipientName")]
        public string RecipientName { get; set; } = "";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "nouveau";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("traitePar")]
        public string TraitePar { get; set; } = "";

        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("reste")]
        public decimal Reste { get; set; }

        [JsonPropertyName("createdAt")]
        public long CreatedAt { get; set; }

        [JsonPropertyName("items")]
        public List<OrderItem> Items { get; set; } = new();
    }

    // Modèle des articles contenus dans une commande
    public class OrderItem
    {
        [PrimaryKey]
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("productFirebaseId")]
        public string ProductFirebaseId { get; set; } = string.Empty;

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = "";

        // On ignore l'alias à la désérialisation JSON pour éviter les conflits de clés multiples
        [JsonIgnore]
        public string Name
        {
            get => ProductName;
            set => ProductName = value;
        }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("unitPrice")]
        public int UnitPrice { get; set; }

        // ✅ L'alias ajouté pour corriger l'erreur dans MainLayout.razor (Ignoré par le JSON Firebase)
        [JsonIgnore]
        public decimal Price
        {
            get => (decimal)UnitPrice;
            set => UnitPrice = (int)value;
        }

        // Total calculé automatiquement (Ignoré par le JSON car il n'y a pas de 'set')
        [JsonIgnore]
        public int TotalPrice => Quantity * UnitPrice;
    }
