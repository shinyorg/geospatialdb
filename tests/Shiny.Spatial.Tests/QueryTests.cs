using System;
using FluentAssertions;
using Shiny.Spatial.Database;
using Shiny.Spatial.Geometry;
using Xunit;

namespace Shiny.Spatial.Tests;

public class QueryTests : IDisposable
{
    private readonly SpatialDatabase _db;
    private readonly SpatialTable _table;

    public QueryTests()
    {
        _db = new SpatialDatabase(":memory:");
        _table = _db.CreateTable("cities", CoordinateSystem.Wgs84,
            new PropertyDefinition("name", PropertyType.Text),
            new PropertyDefinition("population", PropertyType.Integer));

        // Denver
        _table.Insert(new SpatialFeature(new Point(-104.99, 39.74))
        {
            Properties = { ["name"] = "Denver", ["population"] = 715000L }
        });
        // Colorado Springs
        _table.Insert(new SpatialFeature(new Point(-104.82, 38.83))
        {
            Properties = { ["name"] = "Colorado Springs", ["population"] = 478000L }
        });
        // Boulder
        _table.Insert(new SpatialFeature(new Point(-105.27, 40.015))
        {
            Properties = { ["name"] = "Boulder", ["population"] = 105000L }
        });
        // New York
        _table.Insert(new SpatialFeature(new Point(-74.006, 40.7128))
        {
            Properties = { ["name"] = "New York", ["population"] = 8336000L }
        });
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void FluentQuery_WithinDistance()
    {
        var center = new Coordinate(-104.99, 39.74);
        var results = _table.Query()
            .WithinDistance(center, 150_000)
            .ToList();

        results.Count.Should().Be(3); // Denver, C.Springs, Boulder
    }

    [Fact]
    public void FluentQuery_WithinDistance_And_PropertyFilter()
    {
        var center = new Coordinate(-104.99, 39.74);
        var results = _table.Query()
            .WithinDistance(center, 150_000)
            .WhereProperty("population", ">", 200000L)
            .ToList();

        results.Count.Should().Be(2); // Denver and C.Springs
    }

    [Fact]
    public void FluentQuery_OrderByDistance_And_Limit()
    {
        var center = new Coordinate(-104.99, 39.74); // Denver
        var results = _table.Query()
            .WithinDistance(center, 200_000)
            .OrderByDistance(center)
            .Limit(2)
            .ToList();

        results.Count.Should().Be(2);
        results[0].Properties["name"].Should().Be("Denver"); // closest
    }

    [Fact]
    public void FluentQuery_Intersecting_Polygon()
    {
        // Colorado bounding box
        var colorado = new Polygon(new[]
        {
            new Coordinate(-109.05, 37.0), new Coordinate(-102.05, 37.0),
            new Coordinate(-102.05, 41.0), new Coordinate(-109.05, 41.0),
            new Coordinate(-109.05, 37.0)
        });

        var results = _table.Query()
            .Intersecting(colorado)
            .ToList();

        results.Count.Should().Be(3); // Denver, C.Springs, Boulder
        results.Should().NotContain(f => (string)f.Properties["name"]! == "New York");
    }

    [Fact]
    public void FluentQuery_PropertyFilter_Text()
    {
        var results = _table.Query()
            .InEnvelope(new Envelope(-180, 180, -90, 90))
            .WhereProperty("name", "=", "Denver")
            .ToList();

        results.Count.Should().Be(1);
        results[0].Properties["name"].Should().Be("Denver");
    }

    [Fact]
    public void FluentQuery_Offset_And_Limit()
    {
        var center = new Coordinate(-104.99, 39.74);
        var results = _table.Query()
            .WithinDistance(center, 200_000)
            .OrderByDistance(center)
            .Offset(1)
            .Limit(1)
            .ToList();

        results.Count.Should().Be(1);
        results[0].Properties["name"].Should().NotBe("Denver"); // skipped Denver
    }

    [Fact]
    public void FluentQuery_Count()
    {
        var count = _table.Query()
            .InEnvelope(new Envelope(-180, 180, -90, 90))
            .Count();

        count.Should().Be(4);
    }

    [Fact]
    public void FluentQuery_FirstOrDefault()
    {
        var center = new Coordinate(-104.99, 39.74);
        var result = _table.Query()
            .WithinDistance(center, 1)
            .OrderByDistance(center)
            .FirstOrDefault();

        result.Should().NotBeNull();
        result!.Properties["name"].Should().Be("Denver");
    }

    [Fact]
    public void FluentQuery_FirstOrDefault_NoResults()
    {
        var result = _table.Query()
            .InEnvelope(new Envelope(100, 110, 100, 110))
            .FirstOrDefault();

        result.Should().BeNull();
    }
}
