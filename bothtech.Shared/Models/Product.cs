using SQLite;
using System;
using System.Collections.Generic;

namespace bothtech.Shared.Models
{
    public class Product
    {
        // --- IDENTIFIANTS ---
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // --- INFORMATIONS GÉNÉRALES ---
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Specs { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Badge { get; set; } = string.Empty;

        // --- TARIFICATION ---
        public int Price { get; set; }
        public int? OldPrice { get; set; }
        public int? PartnerPrice { get; set; } // Prix B2B
        public int? CostPrice { get; set; }    // Prix de revient
        public int? MaxDiscount { get; set; }

        // --- STOCKS & VENTES ---
        public int Quantity { get; set; }
        public int SoldQuantity { get; set; }

        // --- ÉVALUATIONS ---
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public int RatingTotal { get; set; }

        // --- STATUTS & ADMINISTRATION ---
        public bool Active { get; set; } = true;
        public bool Validated { get; set; } = false;
        public string ValidatedBy { get; set; } = string.Empty;

        // --- DATES ---
        public long CreatedAt { get; set; }
        public long? UpdatedAt { get; set; }
        public long? ValidatedAt { get; set; }

        // ==========================================
        // NOUVELLES DONNÉES DÉTECTÉES DANS FIREBASE
        // ==========================================

        // [Ignore] empêche SQLite de planter en essayant de créer une colonne pour une Liste
        [Ignore]
        public List<string> Images { get; set; } = new List<string>();

        // [Ignore] empêche SQLite de planter sur le dictionnaire, mais Newtonsoft le lira depuis Firebase !
        [Ignore]
        public Dictionary<string, Review> Reviews { get; set; } = new Dictionary<string, Review>();
    }

    // Sous-classe pour gérer la structure des commentaires (reviews) vus dans le JSON
    public class Review
    {
        public string Author { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public long CreatedAt { get; set; }
        public int Rating { get; set; }
        public string UserId { get; set; } = string.Empty;
    }
}