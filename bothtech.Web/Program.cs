using Microsoft.AspNetCore.Components.Authorization;
using bothtech.Shared.Services;
using bothtech.Web.Components;
using bothtech.Web.Services;

// 1. CONSTRUCTEUR SERVEUR (Remplace WebAssemblyHostBuilder)
var builder = WebApplication.CreateBuilder(args);

// Ajout des composants Razor avec le mode interactif Serveur
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 2. SERVICES SYSTÈME & MULTIPLATEFORME
// HttpClient basique pour les requêtes si besoin dans les composants partagés
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost") });

// Service spécifique au web injecté dans le projet partagé
builder.Services.AddSingleton<IFormFactor, FormFactor>();

// 3. SERVICES MÉTIER (Vos créations)
builder.Services.AddSingleton<FirebaseService>();
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<CartService>();

// --- AJOUT : CONFIGURATION CLIENT HTTP PERMISSIF POUR FIREBASE ---
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
// -----------------------------------------------------------------

// 4. SÉCURITÉ ET AUTHENTIFICATION
builder.Services.AddAuthorizationCore(); // Active la gestion des rôles (Admin, RH, etc.)

// On enregistre notre fournisseur d'identité personnalisé
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<CustomAuthStateProvider>());

// Service de connexion
builder.Services.AddScoped<AuthService>();

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
app.MapStaticAssets(); // Nouveauté .NET 9 pour les fichiers statiques

// 5. MAPPAGE DES COMPOSANTS & LIAISON AVEC LE PROJET PARTAGÉ
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    // Cette ligne indique au serveur web de charger toutes les pages de bothtech.Shared
    .AddAdditionalAssemblies(typeof(bothtech.Shared.Services.DatabaseService).Assembly);

// Lancement du serveur
app.Run();