using System;
using BenchmarkDotNet.Attributes;
using Shiny.Spatial.Database;
using Shiny.Spatial.Geometry;

namespace Shiny.Spatial.Benchmarks;

[MemoryDiagnoser]
public class InsertBenchmarks
{
    [Params(1_000, 10_000, 100_000)]
    public int Count;

    private SpatialFeature[] _features = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        _features = new SpatialFeature[Count];
        for (var i = 0; i < Count; i++)
        {
            var lon = rng.NextDouble() * 360 - 180;
            var lat = rng.NextDouble() * 180 - 90;
            _features[i] = new SpatialFeature(new Point(lon, lat))
            {
                Properties =
                {
                    ["name"] = $"feature_{i}",
                    ["value"] = (long)rng.Next(0, 1_000_000)
                }
            };
        }
    }

    [Benchmark]
    public void SingleInsert()
    {
        using var db = new SpatialDatabase(":memory:");
        var table = db.CreateTable("bench", CoordinateSystem.Wgs84,
            new PropertyDefinition("name", PropertyType.Text),
            new PropertyDefinition("value", PropertyType.Integer));

        for (var i = 0; i < _features.Length; i++)
            table.Insert(_features[i]);
    }

    [Benchmark]
    public void BulkInsert()
    {
        using var db = new SpatialDatabase(":memory:");
        var table = db.CreateTable("bench", CoordinateSystem.Wgs84,
            new PropertyDefinition("name", PropertyType.Text),
            new PropertyDefinition("value", PropertyType.Integer));

        table.BulkInsert(_features);
    }
}
