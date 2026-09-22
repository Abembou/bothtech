namespace bothtech.Shared.Services
{
    public static class ApiConfig
    {
        /// <summary>
        /// Fournit l'URL de base uniquement pour le projet Web (Développement vs Production).
        /// Les applications MAUI dialoguent directement avec Firebase et ne nécessitent pas cette URL.
        /// </summary>
        public static string GetWebBaseUrl(bool isDevelopment = true)
        {
            return isDevelopment ? "https://localhost:7197" : "https://bothtechburkina.onrender.com";
        }
    }
}