namespace Shiny.Spatial.Geofencing;

public interface ISpatialGeofenceManager
{
    /// <summary>
    /// 
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