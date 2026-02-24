using Microsoft.Extensions.Logging;
using Shiny.Spatial.Geofencing;

namespace Sample.Maui;

public class SampleGeofenceDelegate : ISpatialGeofenceDelegate
{
    readonly ILogger<SampleGeofenceDelegate> logger;

    public static event EventHandler<SpatialRegionChange>? RegionChanged;

    public SampleGeofenceDelegate(ILogger<SampleGeofenceDelegate> logger)
    {
        this.logger = logger;
    }

    public Task OnRegionChanged(SpatialRegionChange change)
    {
        logger.LogInformation(
            "Region changed in {Table}: {Previous} -> {Current}",
            change.TableName,
            change.PreviousRegion?.Properties.GetValueOrDefault("name"),
            change.CurrentRegion?.Properties.GetValueOrDefault("name")
        );

        RegionChanged?.Invoke(this, change);
        return Task.CompletedTask;
    }
}
