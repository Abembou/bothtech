using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http; // Requis pour HttpClient et HttpClientHandler
using bothtech.Services;
using bothtech.Shared.Services;

namespace bothtech
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Service spécifique à l'appareil pour le projet partagé
            builder.Services.AddSingleton<IFormFactor, FormFactor>();

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            // Permet d'utiliser la touche F12 pour ouvrir la console dans l'appli Windows
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            // ============================================================
            // CONFIGURATION HTTP (Ajoutée pour corriger l'écran Loading...)
            // ============================================================

            // 1. HttpClient de base pour les requêtes génériques
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost") });

            // 2. Client HTTP permissif spécifique pour Firebase
            builder.Services.AddHttpClient("FirebaseClient")
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    return new HttpClientHandler
                    {
                        // Accepte tous les certificats (utile en dev local)
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                    };
                });

            // ============================================================
            // SERVICES MÉTIER
            // ============================================================

            // L'ordre compte : Firebase d'abord, puis DatabaseService
            builder.Services.AddSingleton<FirebaseService>();
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<CartService>();

            // 3. Déclaration correcte du SyncService utilisant la Factory
            builder.Services.AddScoped<SyncService>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var dbService = sp.GetRequiredService<DatabaseService>();
                return new SyncService(httpClientFactory.CreateClient("FirebaseClient"), dbService);
            });

            // ============================================================
            // SÉCURITÉ ET AUTHENTIFICATION
            // ============================================================
            builder.Services.AddAuthorizationCore(); // Active <AuthorizeView>

            // Fournisseur d'identité personnalisé
            builder.Services.AddScoped<CustomAuthStateProvider>();
            builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<CustomAuthStateProvider>());

            // Service de connexion
            builder.Services.AddScoped<AuthService>();

            return builder.Build();
        }
    }
}