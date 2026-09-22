using System;
using System.Threading.Tasks;
using SQLite;
using Firebase.Database;
using Firebase.Database.Query;

namespace bothtech.Shared.Services
{
    public class DataSyncService
    {
        private readonly SQLiteConnection _sqliteDb;
        private readonly FirebaseClient _firebaseClient;

        public DataSyncService(SQLiteConnection sqliteDb, FirebaseClient firebaseClient)
        {
            _sqliteDb = sqliteDb;
            _firebaseClient = firebaseClient;
        }

        public async Task SaveDataAsync<T>(T data, string firebaseTable) where T : new()
        {
            // 1. Toujours enregistrer en local d'abord (Garantie Offline-First)
            try
            {
                _sqliteDb.InsertOrReplace(data);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur SQLite : {ex.Message}");
            }

            // 2. Vérification de la disponibilité du réseau
            bool hasInternet = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();

            if (hasInternet)
            {
                try
                {
                    // Envoi direct de l'objet (FirebaseDatabase.Net s'occupe de la sérialisation)
                    await _firebaseClient
                        .Child(firebaseTable)
                        .PostAsync(data);

                    System.Diagnostics.Debug.WriteLine("Données synchronisées avec succès (SQLite + Firebase).");
                }
                catch (Exception ex)
                {
                    // Si Firebase échoue, les données restent en sécurité dans SQLite
                    System.Diagnostics.Debug.WriteLine($"Erreur de synchronisation Firebase : {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Mode hors-ligne : Données enregistrées uniquement dans SQLite.");
            }
        }
    }
}