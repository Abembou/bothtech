using SQLite;
using System.Collections.Generic;
using System.Text.Json;

namespace bothtech.Shared.Models
{
    public class RoleConfig
    {
        [PrimaryKey]
        public string Id { get; set; } // Le nom du rôle (ex: "secretaire")

        [Ignore]
        public Dictionary<string, Dictionary<string, bool>> Permissions { get; set; } = new();

        // Propriété fantôme pour stocker le dictionnaire en texte dans SQLite
        public string PermissionsJson
        {
            get => JsonSerializer.Serialize(Permissions);
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    Permissions = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, bool>>>(value) ?? new();
                }
            }
        }
    }
}