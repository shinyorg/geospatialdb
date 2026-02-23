using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Shiny.Spatial.Database;
using Shiny.Spatial.Geometry;

namespace Shiny.Spatial.Benchmarks;

[MemoryDiagnoser]
public class SpatialQueryBenchmarks
{
    private SpatialDatabase _db = null!;
    private SpatialTable _table = null!;
    private long _knownId;

    private Envelope _smallEnvelope;
    private Envelope _largeEnvelope;
    private Polygon _queryPolygon = null!;
    private Coordinate _queryCenter;

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

        _knownId = 1;

        // Small envelope: ~1% of the total area (lon +-18, lat +-9)
        _smallEnvelope = new Envelope(-18, 18, -9, 9);

        // Large envelope: ~25% of the total area (lon +-90, lat +-45)
        _largeEnvelope = new Envelope(-90, 90, -45, 45);

        // Query polygon centered around 0,0
        _queryPolygon = new Polygon(new Coordinate[]
        {
            new(-10, -10),
            new(10, -10),
            new(10, 10),
            new(-10, 10),
            new(-10, -10)
        });

        _queryCenter = new Coordinate(0, 0);
    }

    [GlobalCleanup]
    public void Cleanup() => _db.Dispose();

    [Benchmark]
    public List<SpatialFeature> FindInEnvelope_Small() => _table.FindInEnvelope(_smallEnvelope);

    [Benchmark]
    public List<SpatialFeature> FindInEnvelope_Large() => _table.FindInEnvelope(_largeEnvelope);

    [Benchmark]
    public List<SpatialFeature> FindIntersecting_Polygon() => _table.FindIntersecting(_queryPolygon);

    [Benchmark]
    public List<SpatialFeature> FindWithinDistance() => _table.FindWithinDistance(_queryCenter, 500_000);

    [Benchmark]
    public List<SpatialFeature> FindContainedBy() => _table.FindContainedBy(_queryPolygon);

    [Benchmark]
    public SpatialFeature? GetById() => _table.GetById(_knownId);
}
