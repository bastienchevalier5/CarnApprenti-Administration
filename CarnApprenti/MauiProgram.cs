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
using Microsoft.Maui.LifecycleEvents;

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
                config.SetMinimumLevel(LogLevel.Information); // Assurez-vous que vous capturez les logs de niveau Information et supérieur

            });

            var connectionString = "server=10.192.154.1;database=carnapprenti;user=root;password=Not24get;";

            builder.Services.AddDbContext<LivretApprentissageContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

            builder.Services.AddScoped<DatabaseService>();


            builder.Services.AddScoped<PdfService>();

            builder.Services.AddScoped<ModeleService>();


            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

#if WINDOWS
                        builder.ConfigureLifecycleEvents(events =>
                        {
                            events.AddWindows(windows => windows.OnWindowCreated(window =>
                            {
                                window.ExtendsContentIntoTitleBar = true;
                                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
                                appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
                            }));
                        });
#endif

#if ANDROID
            builder.ConfigureLifecycleEvents(events =>
            {
                events.AddAndroid(android => android.OnCreate((activity, savedInstanceState) =>
                {
                    activity.Window?.AddFlags(Android.Views.WindowManagerFlags.Fullscreen);
                }));
            });
#endif
            builder.Services.AddMauiBlazorWebView();
#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
#endif
            return builder.Build();
        }
    }
}
