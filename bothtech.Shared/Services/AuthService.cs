using System.Threading.Tasks;
using bothtech.Shared.Models;

namespace bothtech.Shared.Services
{
    public class AuthService
    {
        private readonly CustomAuthStateProvider _authStateProvider;
        private readonly FirebaseService _firebaseService;

        public AuthService(CustomAuthStateProvider authStateProvider, FirebaseService firebaseService)
        {
            _authStateProvider = authStateProvider;
            _firebaseService = firebaseService;
        }

        // Simule la connexion Firebase (à remplacer par le vrai SDK Firebase .NET)
        public async Task<bool> LoginAsync(string email, string password)
        {
            // 1. Appel à Firebase Auth pour vérifier l'email/mot de passe
            await Task.Delay(500); // Simulation réseau

            // 2. Si succès, on récupère le profil dans Firebase Database
            // user = await _firebaseService.GetUserProfileAsync(firebaseUid);

            // Simulation d'un profil récupéré
            var user = new User
            {
                FirebaseUid = "XrvteGbdzWWQml7Tg1mRPHJsHg03",
                Email = email,
                Name = "Dodoyéo Issouf",
                Role = "maintenance" // Testez en changeant ce rôle par "client" ou "detaillantB2B"
            };

            // 3. On connecte l'utilisateur dans Blazor
            _authStateProvider.MarkUserAsAuthenticated(user);

            return true;
        }

        public void Logout()
        {
            _authStateProvider.MarkUserAsLoggedOut();
        }
    }
}