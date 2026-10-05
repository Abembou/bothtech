using SQLite;

namespace bothtech.Shared.Models
{
    public class CompanySettings
    {
        [PrimaryKey]
        public string Id { get; set; } = "MAIN"; // Un seul paramètre général

        public string Name { get; set; } = "";
        public string Slogan { get; set; } = "";
        public string Ifu { get; set; } = "";
        public string Currency { get; set; } = "FCFA";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public string Country { get; set; } = "";
        public string FooterText { get; set; } = "";

        // NOUVEAU : Propriété pour stocker l'image du logo
        public string LogoBase64 { get; set; } = "";
    }
}