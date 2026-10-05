using System;
using System.Threading.Tasks;
using bothtech.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Collections.Generic;
using Microsoft.JSInterop;

namespace bothtech.Shared.Services;

public class AuthService
{
    public static Func<string, string, Task<string?>>? NativeWebAuthenticator { get; set; }
    public static string NativeRedirectUri { get; set; } = "bothtech://callback";

    public bool IsAuthenticated { get; private set; } = false;

    private readonly CustomAuthStateProvider _authStateProvider;
    private readonly FirebaseService _firebaseService;
    private readonly NavigationManager _navigationManager;
    private readonly IJSRuntime _jsRuntime;

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
            var responseData = JsonSerializer.Deserialize<FirebaseAuthResponse>(loginResponseContent);
            idToken = responseData!.idToken;
            uid = responseData.localId;
        }
        else
        {
            // 2. ÉCHEC DE CONNEXION : On tente de créer le compte
            var signUpResponse = await client.PostAsync(signUpUrl, jsonPayload);
            string signUpResponseContent = await signUpResponse.Content.ReadAsStringAsync();

            if (signUpResponse.IsSuccessStatusCode)
            {
                var responseData = JsonSerializer.Deserialize<FirebaseAuthResponse>(signUpResponseContent);
                idToken = responseData!.idToken;
                uid = responseData.localId;
            }
            else
            {
                if (signUpResponseContent.Contains("EMAIL_EXISTS"))
                {
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

        // ✅ SAUVEGARDE COMPLÈTE DE LA SESSION LOCALE
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "bothtech_userid", uid);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "bothtech_idtoken", idToken);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "bothtech_userphone", profileData.Phone);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "bothtech_useraddress", profileData.Address);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "bothtech_useremail", email);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "bothtech_username", name);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "bothtech_userrole", profileData.Role);

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

        // 3. PONT AUTH0 -> FIREBASE
        var user = await SyncAuth0ToFirebase(email, name, uid);

        // 4. SAUVEGARDE EN LOCAL
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "bothtech_useremail", user.Email);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "bothtech_username", user.Name);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "bothtech_userrole", user.Role);

        _authStateProvider.MarkUserAsAuthenticated(user);
        IsAuthenticated = true;

        return true;
    }

    public async Task LogoutAsync()
    {
        IsAuthenticated = false;
        _authStateProvider.MarkUserAsLoggedOut();

        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "bothtech_userid");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "bothtech_idtoken");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "bothtech_userphone");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "bothtech_useraddress");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "bothtech_useremail");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "bothtech_username");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "bothtech_userrole");
        }
        catch { }
    }

    // ======================================================================
    // ✅ PONT AUTH0 / GOOGLE -> FIREBASE AUTH
    // ======================================================================
    private async Task<User> SyncAuth0ToFirebase(string email, string name, string auth0Sub)
    {
        string rawPassword = $"BT_{auth0Sub}_{email.ToLower()}!2026";
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

        var loginResponse = await client.PostAsync(loginUrl, jsonPayload);
        string loginResponseContent = await loginResponse.Content.ReadAsStringAsync();

        if (loginResponse.IsSuccessStatusCode)
        {
            var responseData = JsonSerializer.Deserialize<FirebaseAuthResponse>(loginResponseContent);
            idToken = responseData!.idToken;
            firebaseUid = responseData.localId;
        }
        else
        {
            var signUpResponse = await client.PostAsync(signUpUrl, jsonPayload);
            string signUpResponseContent = await signUpResponse.Content.ReadAsStringAsync();

            if (signUpResponse.IsSuccessStatusCode)
            {
                var responseData = JsonSerializer.Deserialize<FirebaseAuthResponse>(signUpResponseContent);
                idToken = responseData!.idToken;
                firebaseUid = responseData.localId;
            }
            else
            {
                if (signUpResponseContent.Contains("EMAIL_EXISTS"))
                {
                    throw new Exception("Cet e-mail existe déjà. Si vous aviez créé un compte manuellement, veuillez utiliser le formulaire classique.");
                }
                throw new Exception($"Erreur critique Firebase : {signUpResponseContent}");
            }
        }

        var profileData = await _firebaseService.EnsureUserExistsInDatabaseAsync(firebaseUid, email, name, idToken);

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
    // ✅ RÉINITIALISATION DU MOT DE PASSE (Vérification du code OOB)
    // ======================================================================
    public async Task<string> ConfirmPasswordResetAsync(string oobCode, string newPassword)
    {
        string url = $"https://identitytoolkit.googleapis.com/v1/accounts:resetPassword?key={firebaseApiKey}";
        using var client = new HttpClient();

        var payload = new { oobCode = oobCode, newPassword = newPassword };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content);
        string responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            if (responseContent.Contains("EXPIRED_OOB_CODE")) throw new Exception("Ce lien a expiré. Veuillez refaire une demande.");
            if (responseContent.Contains("INVALID_OOB_CODE")) throw new Exception("Ce lien est invalide ou a déjà été utilisé.");
            throw new Exception("Erreur de réinitialisation Firebase.");
        }

        // En cas de succès, Firebase renvoie l'email de l'utilisateur
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
        return responseData.GetProperty("email").GetString() ?? "";
    }
}

public class FirebaseAuthResponse
{
    public string idToken { get; set; } = "";
    public string localId { get; set; } = "";
    public string email { get; set; } = "";
}