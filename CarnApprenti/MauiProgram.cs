using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Text;

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

            // Ajouter Blazored.SessionStorage
            builder.Services.AddBlazoredSessionStorage();

            // Ajouter des gestionnaires d'erreurs
            builder.Services.AddLogging(config =>
            {
                config.ClearProviders();
                config.AddConsole();
                config.AddDebug();
            });

            var connectionString = "server=192.168.56.56;database=carnapprenti;user=homestead;password=secret;";

            builder.Services.AddDbContext<LivretApprentissageContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

            builder.Services.AddScoped<DatabaseService>();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            return builder.Build();


        }
    }
}
