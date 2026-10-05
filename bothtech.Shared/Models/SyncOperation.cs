using SQLite;
using System;

namespace bothtech.Shared.Models
{
    public class SyncOperation
    {
        // Identifiant local auto-généré par SQLite
        [PrimaryKey, AutoIncrement]
        public int LocalId { get; set; }

        // Identifiant unique universel de l'opération (pour éviter les doublons sur Firebase)
        public string OperationId { get; set; } = "";

        // Le type de donnée (Ex: "Staff", "Order", "Product")
        public string EntityType { get; set; } = "";

        // L'ID de l'objet concerné (Ex: l'UID Firebase du client)
        public string EntityId { get; set; } = "";

        // Le type d'action : "CREATE", "UPDATE", "DELETE"
        public string OperationType { get; set; } = "";

        // Les données complètes converties en texte JSON
        public string Payload { get; set; } = "";

        public DateTime CreatedAt { get; set; }

        // Statut : "Pending" (En attente), "Synced" (Envoyé), "Failed" (Échec)
        public string Status { get; set; } = "Pending";

        // Nombre de tentatives d'envoi échouées (pour gérer les réseaux instables)
        public int RetryCount { get; set; } = 0;
    }
}