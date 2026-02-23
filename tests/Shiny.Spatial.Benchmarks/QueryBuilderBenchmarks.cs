using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Shiny.Spatial.Database;
using Shiny.Spatial.Geometry;

namespace Shiny.Spatial.Benchmarks;

[MemoryDiagnoser]
public class QueryBuilderBenchmarks
{
    private SpatialDatabase _db = null!;
    private SpatialTable _table = null!;

    private Envelope _envelope;
    private Coordinate _center;

    // Full envelope covering all data — forces property-only filtering
    private Envelope _fullEnvelope;

    [GlobalSetup]
    public void Setup()
    {
        _db = new SpatialDatabase(":memory:");
        _table = _db.CreateTable("points", CoordinateSystem.Wgs84,
            new PropertyDefinition("name", PropertyType.Text),
            new PropertyDefinition("value", PropertyType.Integer));

        var rng = new Random(42);
        var features = new SpatialFeature[100_000];
        for (var i = 0; i < features.Length; i++)
        {
            var lon = rng.NextDouble() * 360 - 180;
            var lat = rng.NextDouble() * 180 - 90;
            features[i] = new SpatialFeature(new Point(lon, lat))
            {
                Properties =
                {
                    ["name"] = $"point_{i}",
                    ["value"] = (long)rng.Next(0, 1_000_000)
                }
            };
        }
        _table.BulkInsert(features);

        _envelope = new Envelope(-20, 20, -10, 10);
        _center = new Coordinate(0, 0);
        _fullEnvelope = new Envelope(-180, 180, -90, 90);
    }

    [GlobalCleanup]
    public void Cleanup() => _db.Dispose();

    [Benchmark]
    public List<SpatialFeature> SpatialOnly() =>
        _table.Query()
            .InEnvelope(_envelope)
            .ToList();

    [Benchmark]
    public List<SpatialFeature> SpatialPlusPropertyFilter() =>
        _table.Query()
            .InEnvelope(_envelope)
            .WhereProperty("value", ">", 500_000L)
            .ToList();

    [Benchmark]
    public List<SpatialFeature> DistanceWithOrderAndLimit() =>
        _table.Query()
            .WithinDistance(_center, 500_000)
            .OrderByDistance(_center)
            .Limit(100)
            .ToList();

    [Benchmark]
    public List<SpatialFeature> PropertyFilterOnly() =>
        _table.Query()
            .InEnvelope(_fullEnvelope)
            .WhereProperty("value", ">", 900_000L)
            .ToList();
}
