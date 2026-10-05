using SQLite;
using System;

namespace bothtech.Shared.Models
{
    public class StockMovement
    {
        // Clé primaire indispensable pour SQLite
        [PrimaryKey]
        public string MovementId { get; set; } = "";

        // L'ID du produit concerné
        public string ProductId { get; set; } = "";

        // La valeur du mouvement : -5 (Vente), +50 (Réapprovisionnement), -1 (Perte/Casse)
        public int QuantityChange { get; set; }

        // Nature de l'opération : "SALE" (Vente), "RESTOCK" (Appro), "RETURN" (Retour client)
        public string MovementType { get; set; } = "";

        // L'ID de la commande associée (pour la traçabilité)
        public string OrderId { get; set; } = "";

        // L'agent (UID) qui a fait l'opération
        public string AgentId { get; set; } = "";

        public DateTime Date { get; set; }
    }
}