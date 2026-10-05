using System;
using System.Threading.Tasks;

namespace bothtech.Shared.Services
{
    public class SyncService
    {
        private readonly DataSyncService _dataSync;

        // ✅ INJECTION : On confie tout à DataSyncService
        public SyncService(DataSyncService dataSync)
        {
            _dataSync = dataSync;
        }

        /// <summary>
        /// Synchronise les produits (Relais vers le DataSyncService unifié)
        /// </summary>
        public async Task SyncProductsSilentlyAsync(string userRole)
        {
            bool isWeb = OperatingSystem.IsBrowser();

            if (isWeb)
            {
                Console.WriteLine("🛑 Synchro annulée : Plateforme Web détectée.");
                return;
            }

            if (string.Equals(userRole, "client", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("🛑 Synchro annulée : Rôle 'client' détecté. Pas de mode hors-ligne.");
                return;
            }

            try
            {
                // 🔥 CRUCIAL : On délègue l'aspiration au moteur unifié sécurisé
                await _dataSync.PullProductsFromFirebaseAsync();
                Console.WriteLine("✅ Synchro Firebase relayée au moteur DataSync avec succès !");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur globale Synchro : {ex.Message}");
            }
        }
    }
}