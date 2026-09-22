using bothtech.Shared.Services;
using bothtech.Web.Components;
using bothtech.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.StaticFiles;

// 1. CONSTRUCTEUR SERVEUR
var builder = WebApplication.CreateBuilder(args);

// Ajout des composants Razor avec le mode interactif Serveur
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 2. SERVICES SYSTÈME & MULTIPLATEFORME
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost") });
builder.Services.AddSingleton<IFormFactor, FormFactor>();

// 3. SERVICES MÉTIER
builder.Services.AddSingleton<FirebaseService>();
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<CartService>();
builder.Services.AddSingleton<IPlatformService>(new PlatformService(false));
// --- CONFIGURATION CLIENT HTTP PERMISSIF POUR FIREBASE ---
builder.Services.AddHttpClient("FirebaseClient")
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        return new HttpClientHandler
        {
            // Accepte tous les certificats (uniquement utile en dev local)
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
    });

// On dit au SyncService d'utiliser ce client permissif
builder.Services.AddScoped<SyncService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var dbService = sp.GetRequiredService<DatabaseService>();
    return new SyncService(httpClientFactory.CreateClient("FirebaseClient"), dbService);
});

// 4. SÉCURITÉ ET AUTHENTIFICATION
builder.Services.AddAuthorizationCore(); // Active la gestion des rôles

// On enregistre notre fournisseur d'identité personnalisé
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<CustomAuthStateProvider>());

// Service de connexion
builder.Services.AddScoped<AuthService>();

// =========================================================================
// ?? L'ASTUCE ARCHITECTURALE EST ICI :
// Contrairement au projet MAUI, nous n'injectons PAS "NativeWebAuthenticator".
// Le projet Shared détectera qu'il est "null", et utilisera le navigateur Web standard !
// =========================================================================

// === CONSTRUCTION DE L'APPLICATION ===
var app = builder.Build();

// Configuration du pipeline HTTP (Middlewares)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();

// 5. MAPPAGE DES COMPOSANTS & LIAISON AVEC LE PROJET PARTAGÉ
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    // Indique au serveur web de charger toutes les pages de bothtech.Shared
    .AddAdditionalAssemblies(typeof(bothtech.Shared.Services.DatabaseService).Assembly);


// ... (le reste de votre code Program.cs) ...

// Remplacer "app.UseStaticFiles();" par :
var provider = new FileExtensionContentTypeProvider();
// Ajoute le type MIME officiel pour les applications Android
provider.Mappings[".apk"] = "application/vnd.android.package-archive";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});
// Lancement du serveur
app.Run();