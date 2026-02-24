#if IOS || ANDROID
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Spatial.Geofencing;
using Shiny.Spatial.Geofencing.Infrastructure;

namespace Shiny;

public static class SpatialGpsServiceCollectionExtensions
{
    public static IServiceCollection AddSpatialGps<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IServiceCollection services,
        Action<SpatialMonitorConfig> configure
    ) where T : class, ISpatialGeofenceDelegate
    {
        var config = new SpatialMonitorConfig();
        configure(config);

        services.AddSingleton(config);
        services.AddSingleton<ISpatialGeofenceManager, SpatialGeofenceManager>();
        services.AddSingleton<ISpatialGeofenceDelegate, T>();
        services.AddGps<SpatialGpsDelegate>();

        return services;
    }
}
#endif