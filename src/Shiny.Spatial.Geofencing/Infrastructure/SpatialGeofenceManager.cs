using Shiny.Locations;
using Shiny.Spatial.Database;
using Shiny.Spatial.Geometry;

namespace Shiny.Spatial.Geofencing.Infrastructure;


public class SpatialGeofenceManager(
    IGpsManager gpsManager,
    SpatialMonitorConfig config
) : ISpatialGeofenceManager
{
    public bool IsStarted => gpsManager.IsListening();
    public Task<AccessState> RequestAccess() => gpsManager.RequestAccess(GpsRequest.Realtime(true));
    public Task Start() => gpsManager.StartListener(GpsRequest.Realtime(true));
    public Task Stop() => gpsManager.StopListener();

    public async Task<IReadOnlyList<SpatialCurrentRegion>> GetCurrent(CancellationToken cancelToken = default)
    {
        var reading = await gpsManager.GetCurrentPosition(cancelToken).ConfigureAwait(false);
        var point = new Point(reading.Position.Longitude, reading.Position.Latitude);
        var results = new List<SpatialCurrentRegion>();

        foreach (var entry in config.Entries)
        {
            using var db = new SpatialDatabase(entry.DatabasePath);
            var table = db.GetTable(entry.TableName);
            var feature = table.Query().Intersecting(point).FirstOrDefault();
            results.Add(new SpatialCurrentRegion(entry.TableName, feature));
        }

        return results;
    }
}
