using System.Collections.Generic;
using Shouldly;
using Shiny.Spatial.Geometry;
using Shiny.Spatial.Serialization;
using Xunit;

namespace Shiny.Spatial.Tests;

public class WkbTests
{
    [Fact]
    public void Point_Roundtrip()
    {
        var original = new Point(-104.99, 39.74);
        var wkb = WkbWriter.Write(original);
        var result = WkbReader.Read(wkb) as Point;

        result.ShouldNotBeNull();
        result!.X.ShouldBe(-104.99);
        result.Y.ShouldBe(39.74);
    }

    [Fact]
    public void LineString_Roundtrip()
    {
        var original = new LineString(new[]
        {
            new Coordinate(0, 0),
            new Coordinate(10, 10),
            new Coordinate(20, 0)
        });

        var wkb = WkbWriter.Write(original);
        var result = WkbReader.Read(wkb) as LineString;

        result.ShouldNotBeNull();
        result!.Coordinates.Count.ShouldBe(3);
        result.Coordinates[0].ShouldBe(new Coordinate(0, 0));
        result.Coordinates[2].ShouldBe(new Coordinate(20, 0));
    }

    [Fact]
    public void Polygon_Without_Holes_Roundtrip()
    {
        var original = new Polygon(new[]
        {
            new Coordinate(0, 0), new Coordinate(10, 0),
            new Coordinate(10, 10), new Coordinate(0, 10),
            new Coordinate(0, 0)
        });

        var wkb = WkbWriter.Write(original);
        var result = WkbReader.Read(wkb) as Polygon;

        result.ShouldNotBeNull();
        result!.ExteriorRing.Count.ShouldBe(5);
        result.InteriorRings.Count.ShouldBe(0);
    }

    [Fact]
    public void Polygon_With_Holes_Roundtrip()
    {
        var exterior = new[]
        {
            new Coordinate(0, 0), new Coordinate(20, 0),
            new Coordinate(20, 20), new Coordinate(0, 20),
            new Coordinate(0, 0)
        };
        var hole = new[]
        {
            new Coordinate(5, 5), new Coordinate(15, 5),
            new Coordinate(15, 15), new Coordinate(5, 15),
            new Coordinate(5, 5)
        };
        var original = new Polygon(exterior, new[] { (IReadOnlyList<Coordinate>)hole });

        var wkb = WkbWriter.Write(original);
        var result = WkbReader.Read(wkb) as Polygon;

        result.ShouldNotBeNull();
        result!.ExteriorRing.Count.ShouldBe(5);
        result.InteriorRings.Count.ShouldBe(1);
        result.InteriorRings[0].Count.ShouldBe(5);
    }

    [Fact]
    public void MultiPoint_Roundtrip()
    {
        var original = new MultiPoint(new[]
        {
            new Point(1, 2),
            new Point(3, 4)
        });

        var wkb = WkbWriter.Write(original);
        var result = WkbReader.Read(wkb) as MultiPoint;

        result.ShouldNotBeNull();
        result!.Points.Count.ShouldBe(2);
        result.Points[0].X.ShouldBe(1);
    }

    [Fact]
    public void MultiLineString_Roundtrip()
    {
        var original = new MultiLineString(new[]
        {
            new LineString(new[] { new Coordinate(0, 0), new Coordinate(1, 1) }),
            new LineString(new[] { new Coordinate(2, 2), new Coordinate(3, 3) })
        });

        var wkb = WkbWriter.Write(original);
        var result = WkbReader.Read(wkb) as MultiLineString;

        result.ShouldNotBeNull();
        result!.LineStrings.Count.ShouldBe(2);
    }

    [Fact]
    public void MultiPolygon_Roundtrip()
    {
        var ring1 = new[]
        {
            new Coordinate(0, 0), new Coordinate(1, 0),
            new Coordinate(1, 1), new Coordinate(0, 1),
            new Coordinate(0, 0)
        };
        var ring2 = new[]
        {
            new Coordinate(10, 10), new Coordinate(11, 10),
            new Coordinate(11, 11), new Coordinate(10, 11),
            new Coordinate(10, 10)
        };

        var original = new MultiPolygon(new[]
        {
            new Polygon(ring1),
            new Polygon(ring2)
        });

        var wkb = WkbWriter.Write(original);
        var result = WkbReader.Read(wkb) as MultiPolygon;

        result.ShouldNotBeNull();
        result!.Polygons.Count.ShouldBe(2);
    }

    [Fact]
    public void GeometryCollection_Roundtrip()
    {
        var original = new GeometryCollection(new Geometry.Geometry[]
        {
            new Point(1, 2),
            new LineString(new[] { new Coordinate(0, 0), new Coordinate(5, 5) })
        });

        var wkb = WkbWriter.Write(original);
        var result = WkbReader.Read(wkb) as GeometryCollection;

        result.ShouldNotBeNull();
        result!.Geometries.Count.ShouldBe(2);
        result.Geometries[0].ShouldBeOfType<Point>();
        result.Geometries[1].ShouldBeOfType<LineString>();
    }
}
