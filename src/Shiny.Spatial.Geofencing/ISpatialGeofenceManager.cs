using Shiny.Spatial.Database;

namespace Shiny.Spatial.Geofencing;

public record SpatialCurrentRegion(string TableName, SpatialFeature? Region);

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

    /// <summary>
    /// Gets the current GPS location and queries all monitored tables
    /// to determine which region(s) the device is currently in
    /// </summary>
    /// <param name="cancelToken"></param>
    /// <returns></returns>
    Task<IReadOnlyList<SpatialCurrentRegion>> GetCurrent(CancellationToken cancelToken = default);
}