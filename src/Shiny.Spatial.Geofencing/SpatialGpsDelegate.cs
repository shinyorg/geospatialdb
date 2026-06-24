using Microsoft.Extensions.Logging;
using Shiny.Locations;
using Shiny.Spatial.Database;
using Shiny.Spatial.Geometry;

namespace Shiny.Spatial.Geofencing;

public class SpatialGpsDelegate : GpsDelegate, IDisposable
{
    readonly ILogger<SpatialGpsDelegate> logger;
    readonly ISpatialGeofenceDelegate geofenceDelegate;
    readonly SpatialMonitorConfig config;
    readonly Dictionary<string, SpatialFeature?> currentRegions = new();
    readonly Dictionary<string, (SpatialDatabase Db, SpatialTable Table)> tables = new();
    bool disposed;

    public SpatialGpsDelegate(
        ILogger<SpatialGpsDelegate> logger,
        ISpatialGeofenceDelegate geofenceDelegate,
        SpatialMonitorConfig config
    ) : base(logger)
    {
        this.logger = logger;
        this.geofenceDelegate = geofenceDelegate;
        this.config = config;

        if (config.MinimumDistance != null)
            this.MinimumDistance = config.MinimumDistance;

        if (config.MinimumTime != null)
            this.MinimumTime = config.MinimumTime;
        
        if (config.MaximumDistance != null)
            this.MaximumDistance = config.MaximumDistance;
        
        if (config.MaximumTime != null)
            this.MaximumTime = config.MaximumTime;
    }

    void EnsureTablesLoaded()
    {
        if (tables.Count > 0)
            return;

        foreach (var entry in config.Entries)
        {
            var key = $"{entry.DatabasePath}|{entry.TableName}";
            if (!tables.ContainsKey(key))
            {
                var db = new SpatialDatabase(entry.DatabasePath);
                var table = db.GetTable(entry.TableName);
                tables[key] = (db, table);
            }
        }
    }

    protected override async Task OnGpsReading(GpsReading reading)
    {
        try
        {
            EnsureTablesLoaded();

            var point = new Point(reading.Position.Longitude, reading.Position.Latitude);

            foreach (var (key, (_, table)) in tables)
            {
                var feature = table.Query().Intersecting(point).FirstOrDefault();
                var previousFeature = currentRegions.GetValueOrDefault(key);

                var previousId = previousFeature?.Id;
                var currentId = feature?.Id;

                if (previousId != currentId)
                {
                    currentRegions[key] = feature;
                    var tableName = key.Split('|')[1];

                    if (previousFeature != null)
                    {
                        logger.LogInformation(
                            "Exited region in {TableName}: {Region}",
                            tableName,
                            previousFeature.Properties.GetValueOrDefault("name")
                        );
                        await geofenceDelegate
                            .OnRegionChanged(new SpatialRegionChange(tableName, previousFeature, false))
                            .ConfigureAwait(false);
                    }

                    if (feature != null)
                    {
                        logger.LogInformation(
                            "Entered region in {TableName}: {Region}",
                            tableName,
                            feature.Properties.GetValueOrDefault("name")
                        );
                        await geofenceDelegate
                            .OnRegionChanged(new SpatialRegionChange(tableName, feature, true))
                            .ConfigureAwait(false);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing GPS reading for spatial geofence");
        }
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        foreach (var (_, (db, _)) in tables)
            db.Dispose();

        tables.Clear();
        GC.SuppressFinalize(this);
    }
}
