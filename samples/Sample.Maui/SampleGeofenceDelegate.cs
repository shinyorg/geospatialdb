using Microsoft.Extensions.Logging;
using Shiny.Notifications;
using Shiny.Spatial.Geofencing;

namespace Sample.Maui;

public class SampleGeofenceDelegate(
    ILogger<SampleGeofenceDelegate> logger, 
    INotificationManager notifications
) : ISpatialGeofenceDelegate
{
    public static event EventHandler<SpatialRegionChange>? RegionChanged;

    public async Task OnRegionChanged(SpatialRegionChange change)
    {
        var regionName = change.Region.Properties.GetValueOrDefault("name") ?? "Unknown";
        var action = change.Entered ? "Entered" : "Exited";

        logger.LogInformation(
            "{Action} region in {Table}: {Region}",
            action,
            change.TableName,
            regionName
        );

        RegionChanged?.Invoke(this, change);

        await notifications.Send("Geofence Alert", $"{action}: {regionName}");
    }
}
