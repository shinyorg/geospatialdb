using System;
using System.Collections.Generic;
using Shouldly;
using Shiny.Spatial.Database;
using Shiny.Spatial.Geometry;
using Xunit;

namespace Shiny.Spatial.Tests;

public class DatabaseTests : IDisposable
{
    private readonly SpatialDatabase _db;

    public DatabaseTests()
    {
        _db = new SpatialDatabase(":memory:");
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void CreateTable_And_TableExists()
    {
        var table = _db.CreateTable("cities", CoordinateSystem.Wgs84,
            new PropertyDefinition("name", PropertyType.Text),
            new PropertyDefinition("population", PropertyType.Integer));

        _db.TableExists("cities").ShouldBeTrue();
        _db.TableExists("nonexistent").ShouldBeFalse();
        table.Name.ShouldBe("cities");
        table.CoordinateSystem.ShouldBe(CoordinateSystem.Wgs84);
    }

    [Fact]
    public void CreateTable_Duplicate_Throws()
    {
        _db.CreateTable("cities", CoordinateSystem.Wgs84);
        var act = () => _db.CreateTable("cities", CoordinateSystem.Wgs84);
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void GetTable_Existing()
    {
        _db.CreateTable("cities", CoordinateSystem.Wgs84,
            new PropertyDefinition("name", PropertyType.Text));

        var table = _db.GetTable("cities");
        table.Name.ShouldBe("cities");
        table.Properties.Count.ShouldBe(1);
    }

    [Fact]
    public void GetTable_Nonexistent_Throws()
    {
        var act = () => _db.GetTable("nonexistent");
        Should.Throw<KeyNotFoundException>(act);
    }

    [Fact]
    public void DropTable()
    {
        _db.CreateTable("temp", CoordinateSystem.Cartesian);
        _db.TableExists("temp").ShouldBeTrue();
        _db.DropTable("temp");
        _db.TableExists("temp").ShouldBeFalse();
    }

    [Fact]
    public void Insert_And_GetById()
    {
        var table = _db.CreateTable("cities", CoordinateSystem.Wgs84,
            new PropertyDefinition("name", PropertyType.Text),
            new PropertyDefinition("population", PropertyType.Integer));

        var feature = new SpatialFeature(new Point(-104.99, 39.74))
        {
            Properties = { ["name"] = "Denver", ["population"] = (long)715000 }
        };

        long id = table.Insert(feature);
        id.ShouldBeGreaterThan(0);
        feature.Id.ShouldBe(id);

        var retrieved = table.GetById(id);
        retrieved.ShouldNotBeNull();
        retrieved!.Id.ShouldBe(id);
        retrieved.Properties["name"].ShouldBe("Denver");
        retrieved.Properties["population"].ShouldBe(715000L);

        var geom = retrieved.Geometry as Point;
        geom.ShouldNotBeNull();
        geom!.X.ShouldBe(-104.99);
        geom.Y.ShouldBe(39.74);
    }

    [Fact]
    public void Update_Feature()
    {
        var table = _db.CreateTable("cities", CoordinateSystem.Wgs84,
            new PropertyDefinition("name", PropertyType.Text));

        var feature = new SpatialFeature(new Point(0, 0))
        {
            Properties = { ["name"] = "Old Name" }
        };
        table.Insert(feature);

        var updated = new SpatialFeature(new Point(1, 1))
        {
            Id = feature.Id,
            Properties = { ["name"] = "New Name" }
        };
        table.Update(updated);

        var retrieved = table.GetById(feature.Id);
        retrieved!.Properties["name"].ShouldBe("New Name");
        (retrieved.Geometry as Point)!.X.ShouldBe(1);
    }

    [Fact]
    public void Delete_Feature()
    {
        var table = _db.CreateTable("cities", CoordinateSystem.Wgs84);
        var feature = new SpatialFeature(new Point(0, 0));
        table.Insert(feature);

        table.Count().ShouldBe(1);
        table.Delete(feature.Id).ShouldBeTrue();
        table.Count().ShouldBe(0);
        table.GetById(feature.Id).ShouldBeNull();
    }

    [Fact]
    public void Count()
    {
        var table = _db.CreateTable("cities", CoordinateSystem.Wgs84);
        table.Count().ShouldBe(0);

        table.Insert(new SpatialFeature(new Point(0, 0)));
        table.Insert(new SpatialFeature(new Point(1, 1)));
        table.Count().ShouldBe(2);
    }

    [Fact]
    public void BulkInsert()
    {
        var table = _db.CreateTable("points", CoordinateSystem.Cartesian);
        var features = new List<SpatialFeature>();
        for (int i = 0; i < 100; i++)
            features.Add(new SpatialFeature(new Point(i, i)));

        table.BulkInsert(features);
        table.Count().ShouldBe(100);

        foreach (var f in features)
            f.Id.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void FindInEnvelope()
    {
        var table = _db.CreateTable("cities", CoordinateSystem.Wgs84);
        table.Insert(new SpatialFeature(new Point(5, 5)));
        table.Insert(new SpatialFeature(new Point(15, 15)));
        table.Insert(new SpatialFeature(new Point(25, 25)));

        var results = table.FindInEnvelope(new Envelope(0, 10, 0, 10));
        results.Count.ShouldBe(1);
        (results[0].Geometry as Point)!.X.ShouldBe(5);
    }

    [Fact]
    public void FindIntersecting_Polygon()
    {
        var table = _db.CreateTable("cities", CoordinateSystem.Wgs84);
        table.Insert(new SpatialFeature(new Point(5, 5)));
        table.Insert(new SpatialFeature(new Point(15, 15)));
        table.Insert(new SpatialFeature(new Point(50, 50)));

        var searchArea = new Polygon(new[]
        {
            new Coordinate(0, 0), new Coordinate(20, 0),
            new Coordinate(20, 20), new Coordinate(0, 20),
            new Coordinate(0, 0)
        });

        var results = table.FindIntersecting(searchArea);
        results.Count.ShouldBe(2);
    }

    [Fact]
    public void FindWithinDistance_WGS84()
    {
        var table = _db.CreateTable("cities", CoordinateSystem.Wgs84,
            new PropertyDefinition("name", PropertyType.Text));

        // Denver
        table.Insert(new SpatialFeature(new Point(-104.99, 39.74))
        {
            Properties = { ["name"] = "Denver" }
        });
        // Colorado Springs (~100 km south of Denver)
        table.Insert(new SpatialFeature(new Point(-104.82, 38.83))
        {
            Properties = { ["name"] = "Colorado Springs" }
        });
        // New York (~2500 km away)
        table.Insert(new SpatialFeature(new Point(-74.006, 40.7128))
        {
            Properties = { ["name"] = "New York" }
        });

        // Search within 150km of Denver
        var results = table.FindWithinDistance(new Coordinate(-104.99, 39.74), 150_000);
        results.Count.ShouldBe(2);
        results.ShouldContain(f => (string)f.Properties["name"]! == "Denver");
        results.ShouldContain(f => (string)f.Properties["name"]! == "Colorado Springs");
    }

    [Fact]
    public void FindContainedBy()
    {
        var table = _db.CreateTable("cities", CoordinateSystem.Wgs84);
        table.Insert(new SpatialFeature(new Point(5, 5)));
        table.Insert(new SpatialFeature(new Point(15, 15)));
        table.Insert(new SpatialFeature(new Point(50, 50)));

        var container = new Polygon(new[]
        {
            new Coordinate(0, 0), new Coordinate(20, 0),
            new Coordinate(20, 20), new Coordinate(0, 20),
            new Coordinate(0, 0)
        });

        var results = table.FindContainedBy(container);
        results.Count.ShouldBe(2);
    }
}
