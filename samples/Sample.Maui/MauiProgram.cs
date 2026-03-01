using Shiny;

namespace Sample.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseShiny()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSpatialGps<SampleGeofenceDelegate>(cfg => cfg
            .Add(CopyAssetToAppData("ca-cities.db"), "cities")
        );

        builder.Services.AddNotifications();

        return builder.Build();
    }

    static string CopyAssetToAppData(string assetFileName)
    {
        var destPath = Path.Combine(FileSystem.AppDataDirectory, assetFileName);
        using var source = FileSystem.OpenAppPackageFileAsync(assetFileName).GetAwaiter().GetResult();
        using var dest = File.Create(destPath);
        source.CopyTo(dest);
        return destPath;
    }
}
