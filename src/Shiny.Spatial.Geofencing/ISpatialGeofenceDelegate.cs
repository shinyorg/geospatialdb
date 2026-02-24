namespace Shiny.Spatial.Geofencing;

public interface ISpatialGeofenceDelegate
{
    Task OnRegionChanged(SpatialRegionChange change);
}
