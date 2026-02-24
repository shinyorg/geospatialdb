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
        logger.LogInformation(
            "Region changed in {Table}: {Previous} -> {Current}",
            change.TableName,
            change.PreviousRegion?.Properties.GetValueOrDefault("name"),
            change.CurrentRegion?.Properties.GetValueOrDefault("name")
        );

        RegionChanged?.Invoke(this, change);

        var prev = change.PreviousRegion?.Properties.GetValueOrDefault("name") ?? "None";
        var current = change.CurrentRegion?.Properties.GetValueOrDefault("name") ?? "None";
            
        await notifications.Send("Geofence Alert", $"Region changed: {prev} -> {current}");
    }
}
