using System;
using BenchmarkDotNet.Attributes;
using Shiny.Spatial.Algorithms;
using Shiny.Spatial.Geometry;

namespace Shiny.Spatial.Benchmarks;

[MemoryDiagnoser]
public class AlgorithmBenchmarks
{
    private Coordinate _london;
    private Coordinate _paris;

    private Polygon _simplePolygon = null!;
    private Polygon _complexPolygon = null!;
    private Coordinate _insidePoint;

    private Coordinate _segA1, _segA2, _segB1, _segB2;

    private Polygon _predicatePolygonA = null!;
    private Polygon _predicatePolygonB = null!;

    [GlobalSetup]
    public void Setup()
    {
        _london = new Coordinate(-0.1278, 51.5074);
        _paris = new Coordinate(2.3522, 48.8566);

        // Simple 5-vertex polygon (square with closing vertex)
        _simplePolygon = new Polygon(new Coordinate[]
        {
            new(-10, -10),
            new(10, -10),
            new(10, 10),
            new(-10, 10),
            new(-10, -10)
        });

        // Complex 100-vertex polygon (approximate circle)
        var complexCoords = new Coordinate[101];
        for (var i = 0; i < 100; i++)
        {
            var angle = 2 * Math.PI * i / 100;
            complexCoords[i] = new Coordinate(
                10 * Math.Cos(angle),
                10 * Math.Sin(angle));
        }
        complexCoords[100] = complexCoords[0]; // close the ring
        _complexPolygon = new Polygon(complexCoords);

        _insidePoint = new Coordinate(0, 0);

        // Crossing segments
        _segA1 = new Coordinate(-1, -1);
        _segA2 = new Coordinate(1, 1);
        _segB1 = new Coordinate(-1, 1);
        _segB2 = new Coordinate(1, -1);

        // Overlapping polygons for predicate tests
        _predicatePolygonA = new Polygon(new Coordinate[]
        {
            new(-5, -5),
            new(5, -5),
            new(5, 5),
            new(-5, 5),
            new(-5, -5)
        });

        _predicatePolygonB = new Polygon(new Coordinate[]
        {
            new(0, 0),
            new(10, 0),
            new(10, 10),
            new(0, 10),
            new(0, 0)
        });
    }

    [Benchmark]
    public double Haversine() => DistanceCalculator.Haversine(_london, _paris);

    [Benchmark]
    public double Euclidean() => DistanceCalculator.Euclidean(_london, _paris);

    [Benchmark]
    public bool PointInPolygon_Simple() => PointInPolygon.Contains(_simplePolygon, _insidePoint);

    [Benchmark]
    public bool PointInPolygon_Complex() => PointInPolygon.Contains(_complexPolygon, _insidePoint);

    [Benchmark]
    public bool SegmentIntersection() => Algorithms.SegmentIntersection.Intersects(_segA1, _segA2, _segB1, _segB2);

    [Benchmark]
    public bool SpatialPredicates_PointInPolygon() =>
        SpatialPredicates.Intersects(new Point(_insidePoint.X, _insidePoint.Y), _simplePolygon);

    [Benchmark]
    public bool SpatialPredicates_PolygonIntersectsPolygon() =>
        SpatialPredicates.Intersects(_predicatePolygonA, _predicatePolygonB);
}
