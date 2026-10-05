    using SQLite;
   using System;
    using System;
    using System.Collections.Generic;
    using System.Collections.Generic;
using System.Text.Json;
    using System.Text.Json.Serialization; // Requis pour la désérialisation stricte
    using System.Text.Json.Serialization;

namespace bothtech.Shared.Models; // <-- UNE SEULE DÉCLARATION ICI
public class ProductModel
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }

    // Quantité en stock
    public int Quantity { get; set; }

    // Réduction maximale autorisée par unité
    public decimal MaxDiscount { get; set; }
}
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
            public string VenteId { get; set; }

            [Ignore]
            [JsonPropertyName("items")]
            public List<OrderItem> Items { get; set; } = new List<OrderItem>();
        }


// ==========================================
// 2. MODÈLES DE MESSAGERIE (CHAT)
// ==========================================
public class ChatSession
        {
            [PrimaryKey]
            public string Id { get; set; } = ""; // 👈 C'est cette propriété qui manquait

            [Ignore]
            public Dictionary<string, ChatMessageFirebase> Messages { get; set; } = new();
        }
public class ChatMessageFirebase
        {
            [JsonPropertyName("senderId")]
            public string SenderId { get; set; } = "";

            [JsonPropertyName("senderName")]
            public string SenderName { get; set; } = "";

            [JsonPropertyName("text")]
            public string Text { get; set; } = "";

            [JsonPropertyName("timestamp")]
            public long Timestamp { get; set; }

    // 👇 AJOUTE CES 4 LIGNES POUR L'AUDIO ET LES FICHIERS 👇

            [System.Text.Json.Serialization.JsonPropertyName("isAudio")]
            public bool IsAudio { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("audioDuration")]
            public int AudioDuration { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("attachmentDataUrl")]
            public string? AttachmentDataUrl { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("attachmentName")]
            public string? AttachmentName { get; set; }
        }
public class ChatMessage
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }
            public string FirebaseId { get; set; }
            public string UserUid { get; set; }
            public string Sender { get; set; }
            public string Status { get; set; }
            public string Text { get; set; }
            public string AudioBase64 { get; set; }
            public long Timestamp { get; set; }
        }

// ==========================================
// 3. MODÈLES DU PANIER (CART)
// ==========================================
public class CartItem
        {
            [JsonPropertyName("productFirebaseId")]
            public string ProductFirebaseId { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("price")]
            public int Price { get; set; }

            [JsonPropertyName("quantity")]
            public int Quantity { get; set; }
        }

public class StaffModel
{
    [SQLite.PrimaryKey]
    public string Id { get; set; } = "";
    public string Address { get; set; } = ""; // <-- Ajoutez cette ligne
    // NOUVEAU CHAMP POUR LE OFFLINE
    public string LocalPassword { get; set; } = "";
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string Sexe { get; set; } = "";
    public bool Active { get; set; } = true;
    public string Phone1 { get; set; } = "";
    public string Phone2 { get; set; } = "";
    public string EmergencyName { get; set; } = "";
    public string EmergencyPhone { get; set; } = "";
    public string? Phone { get; set; }
}

public class UserB2BModel
{
    [PrimaryKey]
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string ShopName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string Role { get; set; } = "detaillant";
    public bool Active { get; set; } = true;
}

public class DetaillantFormModel
{
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string ShopName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public bool Active { get; set; } = true;
}