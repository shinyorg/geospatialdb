using Shiny.Locations;

namespace Shiny.Spatial.Geofencing.Infrastructure;


public class SpatialGeofenceManager(IGpsManager gpsManager) : ISpatialGeofenceManager
{
    public bool IsStarted => gpsManager.IsListening();
    public Task<AccessState> RequestAccess() => gpsManager.RequestAccess(GpsRequest.Realtime(true));
    public Task Start() => gpsManager.StartListener(GpsRequest.Realtime(true));
    public Task Stop() => gpsManager.StopListener();
}