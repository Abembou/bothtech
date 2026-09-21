using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using bothtech.Shared.Models;

namespace bothtech.Shared.Services
{
    // Cette classe indique à Blazor si un utilisateur est connecté et quel est son rôle
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        private ClaimsPrincipal _currentUser;

        public CustomAuthStateProvider()
        {
            // Par défaut, l'utilisateur est un visiteur anonyme
            _currentUser = _anonymous;
        }

        // Méthode appelée automatiquement par Blazor pour vérifier les droits
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return Task.FromResult(new AuthenticationState(_currentUser));
        }

        // Méthode à appeler quand la connexion Firebase (Google, Email) réussit
        public void MarkUserAsAuthenticated(User userModel)
        {
            // On crée une "carte d'identité" (Claims) pour l'utilisateur
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userModel.FirebaseUid),
                new Claim(ClaimTypes.Name, userModel.Name ?? userModel.Email),
                new Claim(ClaimTypes.Email, userModel.Email),
                new Claim(ClaimTypes.Role, userModel.Role) // TRÈS IMPORTANT : Définit s'il est admin, client, ou detaillantB2B
            }, "FirebaseAuthentication");

            _currentUser = new ClaimsPrincipal(identity);

            // On prévient toute l'application que quelqu'un vient de se connecter
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }

        // Méthode à appeler lors du clic sur "Déconnexion"
        public void MarkUserAsLoggedOut()
        {
            _currentUser = _anonymous;
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }
    }
}