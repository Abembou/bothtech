using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using bothtech.Shared.Models; // 👈 INDISPENSABLE ICI
using bothtech.Services;
using bothtech.Shared.Services;

using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Configuration;
using System.Reflection;

#if WINDOWS
using System.Net; // Indispensable pour le serveur d'écoute
#else
using Microsoft.Maui.Authentication; // Pour Android/iOS


#endif

// 👇 Requis pour intercepter les permissions et fichiers du WebView sur Android
#if ANDROID
using Android.Webkit;
#endif

namespace bothtech
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            // 1. Charger appsettings.json depuis les ressources incorporées
            var assembly = typeof(MauiProgram).Assembly;
            using var stream = assembly.GetManifestResourceStream("bothtech.appsettings.json");
            if (stream != null)
            {
                var config = new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();

                builder.Configuration.AddConfiguration(config);
            }

            // 2. Enregistrer la configuration pour l'injection par IOptions<AppSettings>
            builder.Services.Configure<AppSettings>(builder.Configuration);

#if WINDOWS
            // 👇 L'ASTUCE EST ICI : On écrase l'URL par défaut uniquement pour Windows
            bothtech.Shared.Services.AuthService.NativeRedirectUri = "http://localhost:8080/callback/";
#endif

            // INJECTION NATIVE
            bothtech.Shared.Services.AuthService.NativeWebAuthenticator = async (authUrl, redirectUri) =>
            {
                try
                {
#if WINDOWS
                    // SOLUTION FIABLE POUR WINDOWS (Serveur local 8080)
                    using var listener = new HttpListener();
                    listener.Prefixes.Add(redirectUri); 
                    listener.Start();

                    // Ouvre Edge ou Chrome
                    await Launcher.Default.OpenAsync(new Uri(authUrl));

                    // Attend la réponse d'Auth0
                    var context = await listener.GetContextAsync();
                    string codeResult = context.Request.QueryString["code"];

                    // 👇 PAGE HTML PROPRE AVEC INSTRUCTION MANUELLE 👇
                    string html = "<html><head><meta charset='utf-8'></head><body style='text-align:center; margin-top:100px; font-family:sans-serif; background-color: #f4f6f9;'>" +
                                  "<div style='background: white; padding: 40px; border-radius: 8px; display: inline-block; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>" +
                                  "<h2 style='color: #28a745;'>Connexion MAUI réussie ! 🎉</h2>" +
                                  "<p style='color: #6c757d; margin-bottom: 10px;'>Vous êtes connecté avec succès à BothTech.</p>" +
                                  "<p style='color: #adb5bd; font-size: 14px;'>Vous pouvez maintenant fermer cet onglet et revenir à l'application.</p>" +
                                  "</div></body></html>";

                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(html);
                    context.Response.ContentLength64 = buffer.Length;
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    context.Response.OutputStream.Close();

                    listener.Stop();
                    return codeResult;
#else
                    // CODE NORMAL POUR ANDROID / IOS (Utilisera bothtech://callback)
                    var result = await WebAuthenticator.Default.AuthenticateAsync(new Uri(authUrl), new Uri(redirectUri));
                    if (result != null && result.Properties.TryGetValue("code", out string authCode))
                    {
                        return authCode;
                    }
                    return null;
#endif
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur Auth : {ex.Message}");
                    return null;
                }
            };

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddSingleton<IFormFactor, FormFactor>();
            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            // Plus besoin de pointer vers https://localhost pour MAUI
            builder.Services.AddScoped(sp => new HttpClient());

            // Le client Firebase configuré pour dialoguer directement avec Firebase
            builder.Services.AddHttpClient("FirebaseClient")
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    return new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                    };
                });

            builder.Services.AddSingleton<FirebaseService>();
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<CartService>();
            builder.Services.AddSingleton<IPlatformService>(new PlatformService(true));
            builder.Services.AddScoped<SyncService>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var dbService = sp.GetRequiredService<DatabaseService>();
                return new SyncService(httpClientFactory.CreateClient("FirebaseClient"), dbService);
            });

            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<CustomAuthStateProvider>();
            builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<CustomAuthStateProvider>());
            builder.Services.AddScoped<AuthService>();

            // ====================================================================
            // 🔥 LE SECRET EST ICI : CONSERVER LES FICHIERS ET AJOUTER LE MICRO
            // ====================================================================
#if ANDROID
            BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping("MicrophoneAndFiles", (handler, view) =>
            {
                // 1. Sauvegarde du client WebChrome d'origine (qui gère l'explorateur de fichiers)
                var originalClient = handler.PlatformView.WebChromeClient;

                // 2. On injecte notre client modifié
                handler.PlatformView.SetWebChromeClient(new MyWebChromeClient(originalClient));
            });
#endif

            return builder.Build();
        }
    }
}

// ====================================================================
// 🔥 CLASSE QUI INTERCEPTE LE MICRO MAIS LAISSE BLAZOR GÉRER LES FICHIERS
// ====================================================================
#if ANDROID
internal class MyWebChromeClient : Android.Webkit.WebChromeClient
{
    private readonly Android.Webkit.WebChromeClient _originalClient;

    public MyWebChromeClient(Android.Webkit.WebChromeClient originalClient)
    {
        _originalClient = originalClient;
    }

    public override void OnPermissionRequest(Android.Webkit.PermissionRequest request)
    {
        // ✅ On accorde silencieusement et automatiquement l'accès au Micro à Blazor
        request?.Grant(request.GetResources());
    }

    // ✅ CORRECTION CS0104 : On utilise Android.Webkit.WebView explicitement
    public override bool OnShowFileChooser(Android.Webkit.WebView webView, Android.Webkit.IValueCallback filePathCallback, Android.Webkit.WebChromeClient.FileChooserParams fileChooserParams)
    {
        if (_originalClient != null)
        {
            return _originalClient.OnShowFileChooser(webView, filePathCallback, fileChooserParams);
        }
        return base.OnShowFileChooser(webView, filePathCallback, fileChooserParams);
    }
}
#endif