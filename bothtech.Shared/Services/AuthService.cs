using System;
using System.Threading.Tasks;
using bothtech.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;

namespace bothtech.Shared.Services
{
    public class AuthService
    {
        // 1. Délégué pour l'exécution du navigateur natif (injecté par MAUI)
        public static Func<string, string, Task<string?>>? NativeWebAuthenticator { get; set; }

        // 2. L'URL par défaut pour Mobile, qui sera remplacée par le port 8080 sous Windows
        public static string NativeRedirectUri { get; set; } = "bothtech://callback";

        private readonly CustomAuthStateProvider _authStateProvider;
        private readonly FirebaseService _firebaseService;
        private readonly NavigationManager _navigationManager;

        private readonly string domain = "dev-051cg6iev2j3dt6l.us.auth0.com";
        private readonly string clientId = "EmV6ndD4ZTrJkOybAQDiUdtCY5S2Ptei";
        private readonly string clientSecret = "I1Mcek3Pl_Ab0mb9x6pjQYE9e3jUuKeJ_7INCKmc1LTcbzdwHcsIrSdeIOQMwumV";

        public AuthService(CustomAuthStateProvider authStateProvider, FirebaseService firebaseService, NavigationManager navigationManager)
        {
            _authStateProvider = authStateProvider;
            _firebaseService = firebaseService;
            _navigationManager = navigationManager;
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            // Simulation de connexion par email/mot de passe classique
            await Task.Delay(500);
            var user = new User { FirebaseUid = "XrvteGbdzWWQml7Tg1mRPHJsHg03", Email = email, Name = "Utilisateur Test", Role = "client" };
            _authStateProvider.MarkUserAsAuthenticated(user);
            return true;
        }

        public async Task LoginWithGoogleAsync() => await TriggerAuthFlow("google-oauth2");
        public async Task LoginWithAuth0Async() => await TriggerAuthFlow(null);

        private async Task TriggerAuthFlow(string? connectionType)
        {
            string connectionParam = string.IsNullOrEmpty(connectionType) ? "" : $"&connection={connectionType}";

            // Si NativeWebAuthenticator n'est pas nul, on est sur MAUI (Windows, Android ou iOS)
            bool isNative = NativeWebAuthenticator != null;

            // Récupère dynamiquement l'URL de base (Localhost ou Render) pour le Web
            string webBaseUri = _navigationManager.BaseUri.TrimEnd('/');
            string redirectUri = isNative ? NativeRedirectUri : $"{webBaseUri}/callback";

            string authUrl = $"https://{domain}/authorize?response_type=code&client_id={clientId}{connectionParam}&redirect_uri={Uri.EscapeDataString(redirectUri)}&prompt=select_account&scope=openid%20profile%20email";

            if (isNative)
            {
                // Appelle le navigateur natif de MAUI et attend le code
                string? code = await NativeWebAuthenticator!(authUrl, redirectUri);
                if (!string.IsNullOrEmpty(code))
                {
                    bool success = await HandleAuth0CallbackAsync(code, redirectUri);
                    if (success)
                    {
                        _navigationManager.NavigateTo("/");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("❌ ERREUR : La connexion a réussi, mais l'échange du Token Auth0 a échoué.");
                    }
                }
            }
            else
            {
                // Navigation standard pour le site Web avec l'URI dynamique
                _navigationManager.NavigateTo(authUrl, forceLoad: true);
            }
        }

        public void Logout() => _authStateProvider.MarkUserAsLoggedOut();

        public async Task<bool> HandleAuth0CallbackAsync(string code, string? redirectUri = null)
        {
            if (string.IsNullOrEmpty(code)) return false;

            // Si aucun redirectUri n'est passé, on utilise dynamiquement le BaseUri courant
            if (string.IsNullOrEmpty(redirectUri))
            {
                redirectUri = $"{_navigationManager.BaseUri.TrimEnd('/')}/callback";
            }

            using var client = new HttpClient();

            try
            {
                var tokenRequest = new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "code", code },
                    { "redirect_uri", redirectUri }
                };

                var tokenResponse = await client.PostAsync($"https://{domain}/oauth/token", new FormUrlEncodedContent(tokenRequest));
                if (!tokenResponse.IsSuccessStatusCode) return false;

                var tokenData = JsonSerializer.Deserialize<JsonElement>(await tokenResponse.Content.ReadAsStringAsync());
                string accessToken = tokenData.GetProperty("access_token").GetString()!;

                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var userResponse = await client.GetAsync($"https://{domain}/userinfo");
                if (!userResponse.IsSuccessStatusCode) return false;

                var userData = JsonSerializer.Deserialize<JsonElement>(await userResponse.Content.ReadAsStringAsync());

                var user = new User
                {
                    FirebaseUid = userData.TryGetProperty("sub", out var subProp) ? subProp.GetString()! : Guid.NewGuid().ToString(),
                    Email = userData.TryGetProperty("email", out var emailProp) ? emailProp.GetString()! : "email@auth0.com",
                    Name = userData.TryGetProperty("name", out var nameProp) ? nameProp.GetString()! : "Utilisateur",
                    Role = "client"
                };

                // Valide l'état d'authentification dans l'application
                _authStateProvider.MarkUserAsAuthenticated(user);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}