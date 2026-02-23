using System;
using BenchmarkDotNet.Attributes;
using Shiny.Spatial.Geometry;
using Shiny.Spatial.Serialization;
using GeometryBase = Shiny.Spatial.Geometry.Geometry;

namespace Shiny.Spatial.Benchmarks;

[MemoryDiagnoser]
public class SerializationBenchmarks
{
    private Point _point = null!;
    private Polygon _polygon = null!;
    private Polygon _complexPolygon = null!;
    private LineString _lineString = null!;

    private byte[] _pointWkb = null!;
    private byte[] _polygonWkb = null!;
    private byte[] _complexPolygonWkb = null!;
    private byte[] _lineStringWkb = null!;

    [GlobalSetup]
    public void Setup()
    {
        _point = new Point(-104.99, 39.74);

        // Simple 5-vertex polygon
        _polygon = new Polygon(new Coordinate[]
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
        complexCoords[100] = complexCoords[0];
        _complexPolygon = new Polygon(complexCoords);

        // LineString with 50 coordinates
        var lineCoords = new Coordinate[50];
        for (var i = 0; i < 50; i++)
            lineCoords[i] = new Coordinate(i * 0.1, i * 0.05);
        _lineString = new LineString(lineCoords);

        // Pre-serialize for read benchmarks
        _pointWkb = WkbWriter.Write(_point);
        _polygonWkb = WkbWriter.Write(_polygon);
        _complexPolygonWkb = WkbWriter.Write(_complexPolygon);
        _lineStringWkb = WkbWriter.Write(_lineString);
    }

    [Benchmark]
    public byte[] WritePoint() => WkbWriter.Write(_point);

    [Benchmark]
    public GeometryBase ReadPoint() => WkbReader.Read(_pointWkb);

    [Benchmark]
    public byte[] WritePolygon() => WkbWriter.Write(_polygon);

    [Benchmark]
    public GeometryBase ReadPolygon() => WkbReader.Read(_polygonWkb);

    [Benchmark]
    public byte[] WriteComplexPolygon() => WkbWriter.Write(_complexPolygon);

    [Benchmark]
    public GeometryBase ReadComplexPolygon() => WkbReader.Read(_complexPolygonWkb);

    [Benchmark]
    public byte[] WriteLineString() => WkbWriter.Write(_lineString);

    [Benchmark]
    public GeometryBase ReadLineString() => WkbReader.Read(_lineStringWkb);
}
