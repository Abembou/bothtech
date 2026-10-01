using SQLite;
using System;
using System.Collections.Generic;

namespace bothtech.Shared.Models
{
    public class Product
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Specs { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Badge { get; set; } = string.Empty;

        public int Price { get; set; }
        public int? OldPrice { get; set; }
        public int? PartnerPrice { get; set; }
        public int? CostPrice { get; set; }
        public int? MaxDiscount { get; set; }

        public int Quantity { get; set; }
        public int SoldQuantity { get; set; }

        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public int RatingTotal { get; set; }

        public bool Active { get; set; } = true;
        public bool Validated { get; set; } = false;
        public string ValidatedBy { get; set; } = string.Empty;

        public long CreatedAt { get; set; }
        public long? UpdatedAt { get; set; }
        public long? ValidatedAt { get; set; }

        [Ignore]
        public List<string> Images { get; set; } = new List<string>();

        [Ignore]
        public Dictionary<string, Review> Reviews { get; set; } = new Dictionary<string, Review>();

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public string ReviewsJson
        {
            get => System.Text.Json.JsonSerializer.Serialize(Reviews);
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    try
                    {
                        // ✅ On force la lecture même si les majuscules ne correspondent pas
                        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        Reviews = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Review>>(value, options) ?? new Dictionary<string, Review>();
                    }
                    catch { Reviews = new Dictionary<string, Review>(); }
                }
            }
        }
    }

    public class Review
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [System.Text.Json.Serialization.JsonPropertyName("userId")]
        public string UserId { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("userName")]
        public string UserName { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("userEmail")]
        public string UserEmail { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("rating")]
        public int Rating { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("comment")]
        public string Comment { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("date")]
        public DateTime Date { get; set; } = DateTime.Now;
    }
}