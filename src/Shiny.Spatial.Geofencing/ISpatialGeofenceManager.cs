namespace Shiny.Spatial.Geofencing;

public interface ISpatialGeofenceManager
{
    // TODO: ability to add shapes to monitor
    // TODO: ability to load an existing database of shapes to monitor
    
    /// <summary>
    /// True if geofence detection is active
    /// </summary>
    bool IsStarted { get; }
    
    /// <summary>
    /// Requests access against GPS
    /// </summary>
    /// <returns></returns>
    Task<AccessState> RequestAccess();
    
    /// <summary>
    /// Geofence detection with this library is large, so we don't configure individual fences
    /// </summary>
    /// <returns></returns>
    Task Start();
    
    /// <summary>
    /// Stops geofence detection
    /// </summary>
    /// <returns></returns>
    Task Stop();
}