using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;

namespace CarnApprenti
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

            // Ajouter les services nécessaires
            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddHttpClient();
            builder.Services.AddAuthorizationCore();

            // Ajouter des gestionnaires d'erreurs
            builder.Services.AddLogging(config =>
            {
                config.ClearProviders();
                config.AddConsole();
                config.AddDebug();
            });

            return builder.Build();
        }
    }
}