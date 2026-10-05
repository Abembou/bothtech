using SQLite;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace bothtech.Shared.Models
{
    public class OrderModel
    {
        [PrimaryKey]
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("clientId")]
        public string ClientId { get; set; } = "";

        [JsonPropertyName("userId")]
        public string UserId { get; set; } = "";

        [JsonPropertyName("clientName")]
        public string ClientName { get; set; } = "";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "nouveau";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("traitePar")]
        public string TraitePar { get; set; } = "";

        // 👇 CORRECTION : SQLite déteste "decimal", on utilise "double"
        [JsonPropertyName("total")]
        public double? Total { get; set; }

        [JsonPropertyName("createdAt")]
        public long CreatedAt { get; set; }

        [JsonIgnore]
        public string ItemsJson
        {
            get => JsonSerializer.Serialize(Items);
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    try { Items = JsonSerializer.Deserialize<List<OrderItem>>(value, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new(); }
                    catch { Items = new(); }
                }
            }
        }

        [SQLite.Ignore]
        [JsonPropertyName("items")]
        public List<OrderItem> Items { get; set; } = new();

        [JsonPropertyName("recipientName")]
        public string? RecipientName { get; set; }

        [JsonPropertyName("recipientPhone")]
        public string? RecipientPhone { get; set; }

        [JsonPropertyName("recipientAddress")]
        public string? RecipientAddress { get; set; }

        [JsonPropertyName("deliveryType")]
        public string? DeliveryType { get; set; }

        [JsonPropertyName("deliveryFee")]
        public int? DeliveryFee { get; set; }

        [JsonPropertyName("discount")]
        public int? Discount { get; set; }

        [JsonPropertyName("paymentMethod")]
        public string? PaymentMethod { get; set; }

        // 👇 CORRECTION : On utilise "double"
        [JsonPropertyName("avance")]
        public double? Avance { get; set; }

        // 👇 CORRECTION : On utilise "double"
        [JsonPropertyName("reste")]
        public double? Reste { get; set; }
    }

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

        [JsonIgnore]
        public int Price
        {
            get => UnitPrice;
            set => UnitPrice = value;
        }

        [JsonIgnore]
        public int TotalPrice => Quantity * UnitPrice;
    }
}