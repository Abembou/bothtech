using System;
using System.Threading.Tasks;
using bothtech.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Collections.Generic;
using Microsoft.JSInterop;

namespace bothtech.Shared.Services
{
    public class AuthService
    {
        public static Func<string, string, Task<string?>>? NativeWebAuthenticator { get; set; }
        public static string NativeRedirectUri { get; set; } = "bothtech://callback";

        public bool IsAuthenticated { get; private set; } = false;

        private readonly CustomAuthStateProvider _authStateProvider;
        private readonly FirebaseService _firebaseService;
        private readonly NavigationManager _navigationManager;
        private readonly IJSRuntime _jsRuntime; // ✅ Indispensable pour stocker la session Auth0/Google

        // Paramètres Auth0
        private readonly string domain = "dev-051cg6iev2j3dt6l.us.auth0.com";
        private readonly string clientId = "EmV6ndD4ZTrJkOybAQDiUdtCY5S2Ptei";
        private readonly string clientSecret = "I1Mcek3Pl_Ab0mb9x6pjQYE9e3jUuKeJ_7INCKmc1LTcbzdwHcsIrSdeIOQMwumV";

        // ✅ CLÉ API FIREBASE (Authentification par email)
        private readonly string firebaseApiKey = "AIzaSyDcDvEWQtqNrqlZQIQ6Qx4wH3L5r6Yi9SI";

        public AuthService(CustomAuthStateProvider authStateProvider, FirebaseService firebaseService, NavigationManager navigationManager, IJSRuntime jsRuntime)
        {
            _authStateProvider = authStateProvider;
            _firebaseService = firebaseService;
            _navigationManager = navigationManager;
            _jsRuntime = jsRuntime;
        }

        public void RestoreSession(User user)
        {
            _authStateProvider.MarkUserAsAuthenticated(user);
            IsAuthenticated = true;
        }

        // ======================================================================
        // ✅ CONNEXION EMAIL/MOT DE PASSE AVEC CRÉATION AUTOMATIQUE (FIREBASE)
        // ======================================================================
        public async Task<User> LoginOrRegisterAsync(string email, string password, string name = "Client BothTech")
        {
            if (string.IsNullOrEmpty(firebaseApiKey) || firebaseApiKey.Contains("VOTRE_CLE_API_WEB_FIREBASE"))
            {
                throw new Exception("🛑 ERREUR CRITIQUE : Clé API Firebase manquante dans AuthService.cs !");
            }

            string loginUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={firebaseApiKey}";
            string signUpUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={firebaseApiKey}";

            using var client = new HttpClient();
            var authPayload = new { email = email, password = password, returnSecureToken = true };
            var jsonPayload = new StringContent(JsonSerializer.Serialize(authPayload), Encoding.UTF8, "application/json");

            string idToken = "";
            string uid = "";

            // 1. TENTATIVE DE CONNEXION
            var loginResponse = await client.PostAsync(loginUrl, jsonPayload);
            string loginResponseContent = await loginResponse.Content.ReadAsStringAsync();

            if (loginResponse.IsSuccessStatusCode)
            {
                // Le mot de passe est bon, on est connecté !
                var responseData = JsonSerializer.Deserialize<FirebaseAuthResponse>(loginResponseContent);
                idToken = responseData!.idToken;
                uid = responseData.localId;
            }
            else
            {
                // 2. ÉCHEC DE CONNEXION : On tente de créer le compte pour voir s'il existait.
                var signUpResponse = await client.PostAsync(signUpUrl, jsonPayload);
                string signUpResponseContent = await signUpResponse.Content.ReadAsStringAsync();

                if (signUpResponse.IsSuccessStatusCode)
                {
                    // Le compte n'existait pas, il vient d'être créé !
                    var responseData = JsonSerializer.Deserialize<FirebaseAuthResponse>(signUpResponseContent);
                    idToken = responseData!.idToken;
                    uid = responseData.localId;
                }
                else
                {
                    // 3. ÉCHEC DE CRÉATION : On analyse pourquoi.
                    if (signUpResponseContent.Contains("EMAIL_EXISTS"))
                    {
                        // C'EST LE SECRET : Si l'email existe, cela veut dire que la première étape (Connexion)
                        // a échoué NON PAS parce que le compte est introuvable, mais parce que le MOT DE PASSE EST FAUX !
                        throw new Exception("Mot de passe incorrect.");
                    }
                    else
                    {
                        throw new Exception($"Erreur Firebase : {signUpResponseContent}");
                    }
                }
            }

            // Synchro Base de Données pour récupérer le rôle, le téléphone et l'adresse
            var profileData = await _firebaseService.EnsureUserExistsInDatabaseAsync(uid, email, name, idToken);

            // ✅ Sauvegarde des clés et des infos de livraison locales
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "bothtech_userid", uid);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "bothtech_idtoken", idToken);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "bothtech_userphone", profileData.Phone);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "bothtech_useraddress", profileData.Address);

            var user = new User
            {
                FirebaseUid = uid,
                Email = email,
                Name = name,
                Role = profileData.Role
            };
            _authStateProvider.MarkUserAsAuthenticated(user);
            IsAuthenticated = true;

            return user;
        }
        public async Task LoginWithGoogleAsync() => await TriggerAuthFlow("google-oauth2");
        public async Task LoginWithAuth0Async() => await TriggerAuthFlow(null);

        private async Task TriggerAuthFlow(string? connectionType)
        {
            string connectionParam = string.IsNullOrEmpty(connectionType) ? "" : $"&connection={connectionType}";
            bool isNative = NativeWebAuthenticator != null;
            string webBaseUri = _navigationManager.BaseUri.TrimEnd('/');
            string redirectUri = isNative ? NativeRedirectUri : $"{webBaseUri}/callback";

            string authUrl = $"https://{domain}/authorize?response_type=code&client_id={clientId}{connectionParam}&redirect_uri={Uri.EscapeDataString(redirectUri)}&prompt=select_account&scope=openid%20profile%20email";

            if (isNative)
            {
                string? code = await NativeWebAuthenticator!(authUrl, redirectUri);
                if (!string.IsNullOrEmpty(code))
                {
                    bool success = await HandleAuth0CallbackAsync(code, redirectUri);
                    if (success)
                    {
                        _navigationManager.NavigateTo("/");
                    }
                }
            }
            else
            {
                _navigationManager.NavigateTo(authUrl, forceLoad: true);
            }
        }

       
        public async Task<bool> HandleAuth0CallbackAsync(string code, string? redirectUri = null)
        {
            if (string.IsNullOrEmpty(code)) throw new Exception("Code d'autorisation manquant.");

            if (string.IsNullOrEmpty(redirectUri))
            {
                redirectUri = $"{_navigationManager.BaseUri.TrimEnd('/')}/callback";
            }

            using var client = new HttpClient();

            // 1. Échange du code contre un Token Auth0
            var tokenRequest = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "code", code },
                { "redirect_uri", redirectUri }
            };

            var tokenResponse = await client.PostAsync($"https://{domain}/oauth/token", new FormUrlEncodedContent(tokenRequest));
            if (!tokenResponse.IsSuccessStatusCode)
            {
                string err = await tokenResponse.Content.ReadAsStringAsync();
                throw new Exception($"Erreur d'échange Token Auth0 : {err}");
            }

            var tokenData = JsonSerializer.Deserialize<JsonElement>(await tokenResponse.Content.ReadAsStringAsync());
            string accessToken = tokenData.GetProperty("access_token").GetString()!;

            // 2. Récupération du profil utilisateur Auth0
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var userResponse = await client.GetAsync($"https://{domain}/userinfo");
            if (!userResponse.IsSuccessStatusCode)
            {
                string err = await userResponse.Content.ReadAsStringAsync();
                throw new Exception($"Erreur de récupération profil Auth0 : {err}");
            }

            var userData = JsonSerializer.Deserialize<JsonElement>(await userResponse.Content.ReadAsStringAsync());

            string uid = userData.TryGetProperty("sub", out var subProp) ? subProp.GetString()! : Guid.NewGuid().ToString();
            string email = userData.TryGetProperty("email", out var emailProp) ? emailProp.GetString()! : "email@auth0.com";
            string name = userData.TryGetProperty("name", out var nameProp) ? nameProp.GetString()! : "Utilisateur";

            // ✅ 3. PONT AUTH0 -> FIREBASE : Création ou connexion silencieuse
            var user = await SyncAuth0ToFirebase(email, name, uid);

            // ✅ 4. SAUVEGARDE EN LOCAL POUR BLAZOR WEB
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "bothtech_useremail", user.Email);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "bothtech_username", user.Name);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "bothtech_userrole", user.Role);

            _authStateProvider.MarkUserAsAuthenticated(user);
            IsAuthenticated = true;

            return true;
        }
        public void Logout()
        {
            IsAuthenticated = false;
            _authStateProvider.MarkUserAsLoggedOut();
        }

     
        // ======================================================================
        // ✅ PONT AUTH0 / GOOGLE -> FIREBASE AUTH (Version Ultra-Stable)
        // ======================================================================
        private async Task<User> SyncAuth0ToFirebase(string email, string name, string auth0Sub)
        {
            // 1. CORRECTION : On base le mot de passe miroir sur l'EMAIL (qui ne change jamais)
            string rawPassword = $"BothTech_{email.ToLower()}_Secure_2026!";

            // On nettoie le Base64 pour éviter tout caractère spécial (+, /, =) qui pourrait buguer
            string generatedPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawPassword))
                                              .Replace("=", "").Replace("+", "").Replace("/", "");
            if (generatedPassword.Length > 20) generatedPassword = generatedPassword.Substring(0, 20);

            string loginUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={firebaseApiKey}";
            string signUpUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={firebaseApiKey}";

            using var client = new HttpClient();
            var authPayload = new { email = email, password = generatedPassword, returnSecureToken = true };
            var jsonPayload = new StringContent(JsonSerializer.Serialize(authPayload), Encoding.UTF8, "application/json");

            string idToken = "";
            string firebaseUid = "";

            // 2. On tente d'abord de se connecter
            var loginResponse = await client.PostAsync(loginUrl, jsonPayload);
            string loginResponseContent = await loginResponse.Content.ReadAsStringAsync();

            if (loginResponse.IsSuccessStatusCode)
            {
                // Connexion réussie !
                var responseData = JsonSerializer.Deserialize<FirebaseAuthResponse>(loginResponseContent);
                idToken = responseData!.idToken;
                firebaseUid = responseData.localId;
            }
            else
            {
                // Échec de connexion : On tente de créer le compte
                var signUpResponse = await client.PostAsync(signUpUrl, jsonPayload);
                string signUpResponseContent = await signUpResponse.Content.ReadAsStringAsync();

                if (signUpResponse.IsSuccessStatusCode)
                {
                    // Création réussie !
                    var responseData = JsonSerializer.Deserialize<FirebaseAuthResponse>(signUpResponseContent);
                    idToken = responseData!.idToken;
                    firebaseUid = responseData.localId;
                }
                else
                {
                    // Si ça échoue encore, on affiche la vraie erreur
                    if (signUpResponseContent.Contains("EMAIL_EXISTS"))
                    {
                        throw new Exception("Cet e-mail existe déjà. Si vous aviez créé un compte manuellement, veuillez utiliser le formulaire classique.");
                    }
                    throw new Exception($"Erreur critique Firebase : {signUpResponseContent}");
                }
            }

            // Synchro Base de Données pour récupérer le rôle, le téléphone et l'adresse
            var profileData = await _firebaseService.EnsureUserExistsInDatabaseAsync(firebaseUid, email, name, idToken);

            // ✅ Sauvegarde des clés et des infos de livraison locales
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "bothtech_userid", firebaseUid);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "bothtech_idtoken", idToken);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "bothtech_userphone", profileData.Phone);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "bothtech_useraddress", profileData.Address);

            return new User
            {
                FirebaseUid = firebaseUid,
                Email = email,
                Name = name,
                Role = profileData.Role
            };
        }
        // ======================================================================
        // ✅ PONT AUTH0 / GOOGLE -> FIREBASE AUTH (Compatible Haute Sécurité)
        // ======================================================================

    }
    // ✅ MODÈLE REQUIS POUR LIRE LA RÉPONSE DE FIREBASE AUTH REST API
    public class FirebaseAuthResponse
    {
        public string idToken { get; set; } = "";
        public string localId { get; set; } = "";
        public string email { get; set; } = "";
    }
}