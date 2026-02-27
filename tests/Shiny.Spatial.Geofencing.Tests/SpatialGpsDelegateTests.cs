using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Shiny.Locations;
using Shiny.Spatial.Database;
using Shiny.Spatial.Geofencing;
using Shiny.Spatial.Geometry;
using Xunit;

namespace Shiny.Spatial.Geofencing.Tests;

public class TestGeofenceDelegate : ISpatialGeofenceDelegate
{
    public List<SpatialRegionChange> Changes { get; } = [];

    public Task OnRegionChanged(SpatialRegionChange change)
    {
        Changes.Add(change);
        return Task.CompletedTask;
    }
}

public class TestableSpatialGpsDelegate : SpatialGpsDelegate
{
    public TestableSpatialGpsDelegate(
        ISpatialGeofenceDelegate geofenceDelegate,
        SpatialMonitorConfig config
    ) : base(NullLogger<SpatialGpsDelegate>.Instance, geofenceDelegate, config)
    {
    }

    public Task SimulateGpsReading(GpsReading reading) => OnGpsReading(reading);
}

public class SpatialGpsDelegateTests : IDisposable
{
    readonly List<string> _tempFiles = [];

    public void Dispose()
    {
        foreach (var f in _tempFiles)
        {
            try { File.Delete(f); } catch { }
        }
    }

    string CreateTestDatabase(string tableName, params SpatialFeature[] features)
    {
        var path = Path.Combine(Path.GetTempPath(), $"geofence_test_{Guid.NewGuid()}.db");
        _tempFiles.Add(path);

        using var db = new SpatialDatabase(path);
        var table = db.CreateTable(tableName, CoordinateSystem.Wgs84,
            new PropertyDefinition("name", PropertyType.Text));

        foreach (var feature in features)
            table.Insert(feature);

        return path;
    }

    static SpatialFeature MakePolygonRegion(string name, double minX, double minY, double maxX, double maxY)
    {
        return new SpatialFeature(new Polygon([
            new Coordinate(minX, minY),
            new Coordinate(maxX, minY),
            new Coordinate(maxX, maxY),
            new Coordinate(minX, maxY),
            new Coordinate(minX, minY)
        ]))
        {
            Properties = { ["name"] = name }
        };
    }

    static GpsReading MakeReading(double longitude, double latitude)
    {
        return new GpsReading(
            new Position(latitude, longitude),
            0d,                      // positionAccuracy
            DateTimeOffset.UtcNow,   // timestamp
            0d, 0d, 0d, 0d, 0d,     // altitude, heading, headingAccuracy, speed, speedAccuracy
            0,                       // satellites
            false                    // isFromMockProvider
        );
    }

    [Fact]
    public async Task Enter_Region_Fires_Change()
    {
        var regionA = MakePolygonRegion("Region A", -10, -10, 10, 10);
        var dbPath = CreateTestDatabase("zones", regionA);

        var testDelegate = new TestGeofenceDelegate();
        var config = new SpatialMonitorConfig().Add(dbPath, "zones");

        using var gpsDelegate = new TestableSpatialGpsDelegate(testDelegate, config);

        await gpsDelegate.SimulateGpsReading(MakeReading(0, 0));

        testDelegate.Changes.Count.ShouldBe(1);
        testDelegate.Changes[0].Entered.ShouldBeTrue();
        testDelegate.Changes[0].Region.Properties["name"].ShouldBe("Region A");
        testDelegate.Changes[0].TableName.ShouldBe("zones");
    }

    [Fact]
    public async Task Exit_Region_Fires_Change()
    {
        var regionA = MakePolygonRegion("Region A", -10, -10, 10, 10);
        var dbPath = CreateTestDatabase("zones", regionA);

        var testDelegate = new TestGeofenceDelegate();
        var config = new SpatialMonitorConfig().Add(dbPath, "zones");

        using var gpsDelegate = new TestableSpatialGpsDelegate(testDelegate, config);

        // Enter region
        await gpsDelegate.SimulateGpsReading(MakeReading(0, 0));
        // Exit region
        await gpsDelegate.SimulateGpsReading(MakeReading(50, 50));

        testDelegate.Changes.Count.ShouldBe(2);

        // First change: enter
        testDelegate.Changes[0].Entered.ShouldBeTrue();
        testDelegate.Changes[0].Region.Properties["name"].ShouldBe("Region A");

        // Second change: exit
        testDelegate.Changes[1].Entered.ShouldBeFalse();
        testDelegate.Changes[1].Region.Properties["name"].ShouldBe("Region A");
    }

    [Fact]
    public async Task Move_Between_Regions_Fires_Exit_And_Enter()
    {
        var regionA = MakePolygonRegion("Region A", -10, -10, 0, 0);
        var regionB = MakePolygonRegion("Region B", 10, 10, 20, 20);
        var dbPath = CreateTestDatabase("zones", regionA, regionB);

        var testDelegate = new TestGeofenceDelegate();
        var config = new SpatialMonitorConfig().Add(dbPath, "zones");

        using var gpsDelegate = new TestableSpatialGpsDelegate(testDelegate, config);

        // Enter region A
        await gpsDelegate.SimulateGpsReading(MakeReading(-5, -5));
        // Move to region B
        await gpsDelegate.SimulateGpsReading(MakeReading(15, 15));

        testDelegate.Changes.Count.ShouldBe(3);

        // Enter A
        testDelegate.Changes[0].Entered.ShouldBeTrue();
        testDelegate.Changes[0].Region.Properties["name"].ShouldBe("Region A");

        // Exit A
        testDelegate.Changes[1].Entered.ShouldBeFalse();
        testDelegate.Changes[1].Region.Properties["name"].ShouldBe("Region A");

        // Enter B
        testDelegate.Changes[2].Entered.ShouldBeTrue();
        testDelegate.Changes[2].Region.Properties["name"].ShouldBe("Region B");
    }

    [Fact]
    public async Task Stay_In_Same_Region_No_Change()
    {
        var regionA = MakePolygonRegion("Region A", -10, -10, 10, 10);
        var dbPath = CreateTestDatabase("zones", regionA);

        var testDelegate = new TestGeofenceDelegate();
        var config = new SpatialMonitorConfig().Add(dbPath, "zones");

        using var gpsDelegate = new TestableSpatialGpsDelegate(testDelegate, config);

        await gpsDelegate.SimulateGpsReading(MakeReading(0, 0));
        await gpsDelegate.SimulateGpsReading(MakeReading(1, 1));
        await gpsDelegate.SimulateGpsReading(MakeReading(2, 2));

        // Only 1 change: the initial enter
        testDelegate.Changes.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Stay_Outside_All_Regions_No_Change()
    {
        var regionA = MakePolygonRegion("Region A", -10, -10, 10, 10);
        var dbPath = CreateTestDatabase("zones", regionA);

        var testDelegate = new TestGeofenceDelegate();
        var config = new SpatialMonitorConfig().Add(dbPath, "zones");

        using var gpsDelegate = new TestableSpatialGpsDelegate(testDelegate, config);

        await gpsDelegate.SimulateGpsReading(MakeReading(50, 50));
        await gpsDelegate.SimulateGpsReading(MakeReading(60, 60));

        testDelegate.Changes.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Multiple_Tables_Independent_Tracking()
    {
        var city = MakePolygonRegion("Denver", -105, 39, -104, 40);
        var state = MakePolygonRegion("Colorado", -109, 37, -102, 41);

        var cityDbPath = CreateTestDatabase("cities", city);
        var stateDbPath = CreateTestDatabase("states", state);

        var testDelegate = new TestGeofenceDelegate();
        var config = new SpatialMonitorConfig()
            .Add(cityDbPath, "cities")
            .Add(stateDbPath, "states");

        using var gpsDelegate = new TestableSpatialGpsDelegate(testDelegate, config);

        // Inside both Denver and Colorado
        await gpsDelegate.SimulateGpsReading(MakeReading(-104.5, 39.5));

        testDelegate.Changes.Count.ShouldBe(2);
        testDelegate.Changes.ShouldAllBe(c => c.Entered);
        testDelegate.Changes.ShouldContain(c => c.TableName == "cities");
        testDelegate.Changes.ShouldContain(c => c.TableName == "states");
    }

    [Fact]
    public async Task Multiple_Tables_Exit_One_Stay_In_Other()
    {
        var city = MakePolygonRegion("Denver", -105, 39, -104, 40);
        var state = MakePolygonRegion("Colorado", -109, 37, -102, 41);

        var cityDbPath = CreateTestDatabase("cities", city);
        var stateDbPath = CreateTestDatabase("states", state);

        var testDelegate = new TestGeofenceDelegate();
        var config = new SpatialMonitorConfig()
            .Add(cityDbPath, "cities")
            .Add(stateDbPath, "states");

        using var gpsDelegate = new TestableSpatialGpsDelegate(testDelegate, config);

        // Inside Denver and Colorado
        await gpsDelegate.SimulateGpsReading(MakeReading(-104.5, 39.5));
        testDelegate.Changes.Clear();

        // Move outside Denver, still in Colorado
        await gpsDelegate.SimulateGpsReading(MakeReading(-106, 38));

        testDelegate.Changes.Count.ShouldBe(1);
        testDelegate.Changes[0].TableName.ShouldBe("cities");
        testDelegate.Changes[0].Entered.ShouldBeFalse();
        testDelegate.Changes[0].Region.Properties["name"].ShouldBe("Denver");
    }

    [Fact]
    public async Task Same_Database_Different_Tables()
    {
        var path = Path.Combine(Path.GetTempPath(), $"geofence_test_{Guid.NewGuid()}.db");
        _tempFiles.Add(path);

        using (var db = new SpatialDatabase(path))
        {
            var citiesTable = db.CreateTable("cities", CoordinateSystem.Wgs84,
                new PropertyDefinition("name", PropertyType.Text));
            citiesTable.Insert(MakePolygonRegion("City A", -10, -10, 10, 10));

            var zonesTable = db.CreateTable("zones", CoordinateSystem.Wgs84,
                new PropertyDefinition("name", PropertyType.Text));
            zonesTable.Insert(MakePolygonRegion("Zone 1", -20, -20, 20, 20));
        }

        var testDelegate = new TestGeofenceDelegate();
        var config = new SpatialMonitorConfig()
            .Add(path, "cities")
            .Add(path, "zones");

        using var gpsDelegate = new TestableSpatialGpsDelegate(testDelegate, config);

        await gpsDelegate.SimulateGpsReading(MakeReading(0, 0));

        testDelegate.Changes.Count.ShouldBe(2);
        testDelegate.Changes.ShouldAllBe(c => c.Entered);
    }

    [Fact]
    public async Task Dispose_Allows_Multiple_Calls()
    {
        var regionA = MakePolygonRegion("A", -10, -10, 10, 10);
        var dbPath = CreateTestDatabase("zones", regionA);

        var testDelegate = new TestGeofenceDelegate();
        var config = new SpatialMonitorConfig().Add(dbPath, "zones");

        var gpsDelegate = new TestableSpatialGpsDelegate(testDelegate, config);

        // Trigger table loading
        await gpsDelegate.SimulateGpsReading(MakeReading(0, 0));

        // Multiple dispose should not throw
        gpsDelegate.Dispose();
        gpsDelegate.Dispose();
    }

    [Fact]
    public async Task Region_Change_Includes_Correct_Table_Name()
    {
        var region = MakePolygonRegion("Test Region", -10, -10, 10, 10);
        var dbPath = CreateTestDatabase("my_custom_table", region);

        var testDelegate = new TestGeofenceDelegate();
        var config = new SpatialMonitorConfig().Add(dbPath, "my_custom_table");

        using var gpsDelegate = new TestableSpatialGpsDelegate(testDelegate, config);

        await gpsDelegate.SimulateGpsReading(MakeReading(0, 0));

        testDelegate.Changes[0].TableName.ShouldBe("my_custom_table");
    }

    [Fact]
    public async Task Enter_Exit_Reenter_Same_Region()
    {
        var region = MakePolygonRegion("Region A", -10, -10, 10, 10);
        var dbPath = CreateTestDatabase("zones", region);

        var testDelegate = new TestGeofenceDelegate();
        var config = new SpatialMonitorConfig().Add(dbPath, "zones");

        using var gpsDelegate = new TestableSpatialGpsDelegate(testDelegate, config);

        // Enter
        await gpsDelegate.SimulateGpsReading(MakeReading(0, 0));
        // Exit
        await gpsDelegate.SimulateGpsReading(MakeReading(50, 50));
        // Re-enter
        await gpsDelegate.SimulateGpsReading(MakeReading(5, 5));

        testDelegate.Changes.Count.ShouldBe(3);

        // Enter
        testDelegate.Changes[0].Entered.ShouldBeTrue();
        testDelegate.Changes[0].Region.Properties["name"].ShouldBe("Region A");

        // Exit
        testDelegate.Changes[1].Entered.ShouldBeFalse();
        testDelegate.Changes[1].Region.Properties["name"].ShouldBe("Region A");

        // Re-enter
        testDelegate.Changes[2].Entered.ShouldBeTrue();
        testDelegate.Changes[2].Region.Properties["name"].ShouldBe("Region A");
    }
}
