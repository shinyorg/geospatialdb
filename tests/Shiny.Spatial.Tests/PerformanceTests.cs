using System;
using System.Collections.Generic;
using System.Diagnostics;
using FluentAssertions;
using Shiny.Spatial.Database;
using Shiny.Spatial.Geometry;
using Xunit;

namespace Shiny.Spatial.Tests;

public class PerformanceTests : IDisposable
{
    private readonly SpatialDatabase _db;

    public PerformanceTests()
    {
        _db = new SpatialDatabase(":memory:");
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void Insert_100K_Points_And_Query()
    {
        var table = _db.CreateTable("perf", CoordinateSystem.Cartesian,
            new PropertyDefinition("value", PropertyType.Integer));

        var random = new Random(42);
        var features = new List<SpatialFeature>();
        for (int i = 0; i < 100_000; i++)
        {
            features.Add(new SpatialFeature(new Point(random.NextDouble() * 360 - 180, random.NextDouble() * 180 - 90))
            {
                Properties = { ["value"] = (long)i }
            });
        }

        var insertSw = Stopwatch.StartNew();
        table.BulkInsert(features);
        insertSw.Stop();

        table.Count().Should().Be(100_000);

        // Spatial query: small envelope
        var querySw = Stopwatch.StartNew();
        var results = table.FindInEnvelope(new Envelope(-10, 10, -10, 10));
        querySw.Stop();

        results.Count.Should().BeGreaterThan(0);

        // These are rough bounds — mainly ensuring queries are fast
        insertSw.Elapsed.TotalSeconds.Should().BeLessThan(30, "100K inserts should complete in reasonable time");
        querySw.Elapsed.TotalSeconds.Should().BeLessThan(1, "spatial query on 100K points should be fast");
    }
}
