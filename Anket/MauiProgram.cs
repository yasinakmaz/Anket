using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.Core;
using System.Diagnostics.CodeAnalysis;

namespace Anket;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("Gentleman.ttf", "Gentleman");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("EthosNova-Bold.ttf", "EthosNovaBold");
                fonts.AddFont("EthosNova-Medium.ttf", "EthosNovaMedium");
                fonts.AddFont("EthosNova-Heavy.ttf", "EthosNovaHeavy");
                fonts.AddFont("EthosNova-Regular.ttf", "EthosNovaRegular");
            });

        RegisterServices(builder.Services);

#if DEBUG
        builder.Logging.AddDebug();
#endif
        var app = builder.Build();

        return app;
    }

    private static void RegisterServices(IServiceCollection services)
    {
        // DbContext singleton olarak kaydet (tek instance)
        services.AddSingleton<AnketDbContext>();

        // Diğer servisler
        services.AddSingleton<IConnectivityService, ConnectivityService>();
        services.AddSingleton<SyncBackgroundService>();
        services.AddSingleton<SqliteDatabaseService>();
        services.AddSingleton<FirebaseDatabaseService>();
        services.AddSingleton<IFileSaver, FileSaverService>();

        // Sayfalar
        services.AddTransient<PopupPage>();
        services.AddTransient<AnketPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<Anket.Pages.Report.ReportPage>();
    }
}