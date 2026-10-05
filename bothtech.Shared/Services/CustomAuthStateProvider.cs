using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using bothtech.Shared.Models;

namespace bothtech.Shared.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        private readonly IJSRuntime _jsRuntime;

        // ✅ INJECTION : On ajoute IJSRuntime pour lire le localStorage
        public CustomAuthStateProvider(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        // ✅ LECTURE LOCALE : Blazor vérifie le localStorage au lieu de la mémoire vive
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // On tente de restaurer la session depuis le stockage local
                var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "bothtech_idtoken");
                var role = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "bothtech_userrole");
                var uid = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "bothtech_userid");

                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(uid))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, uid),
                        new Claim(ClaimTypes.Role, role ?? "client")
                    };

                    var identity = new ClaimsIdentity(claims, "CustomAuth");
                    var principal = new ClaimsPrincipal(identity);

                    return new AuthenticationState(principal);
                }
            }
            catch
            {
                // Capture silencieuse (ex: si le JSInterop n'est pas encore prêt pendant le pré-rendu)
            }

            return new AuthenticationState(_anonymous);
        }

        public void MarkUserAsAuthenticated(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.FirebaseUid),
                new Claim(ClaimTypes.Name, user.Name ?? "Utilisateur"),
                new Claim(ClaimTypes.Email, user.Email ?? "email@introuvable.com"),
                new Claim(ClaimTypes.Role, user.Role ?? "client")
            };

            var identity = new ClaimsIdentity(claims, "CustomAuth");
            var principal = new ClaimsPrincipal(identity);

            // On prévient toute l'application que quelqu'un vient de se connecter
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
        }

        public void MarkUserAsLoggedOut()
        {
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
        }
    }
}