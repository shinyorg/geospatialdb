using System;
using System.Collections.Generic;
using Shouldly;
using Shiny.Spatial.Geometry;
using Xunit;

namespace Shiny.Spatial.Tests;

public class GeometryTests
{
    [Fact]
    public void Coordinate_Equality()
    {
        var a = new Coordinate(1.0, 2.0);
        var b = new Coordinate(1.0, 2.0);
        var c = new Coordinate(3.0, 4.0);

        a.ShouldBe(b);
        a.ShouldNotBe(c);
        (a == b).ShouldBeTrue();
        (a != c).ShouldBeTrue();
    }

    [Fact]
    public void Coordinate_LonLat_Aliases()
    {
        var c = new Coordinate(-104.99, 39.74);
        c.Longitude.ShouldBe(-104.99);
        c.Latitude.ShouldBe(39.74);
    }

    [Fact]
    public void Point_Properties()
    {
        var p = new Point(-104.99, 39.74);
        p.X.ShouldBe(-104.99);
        p.Y.ShouldBe(39.74);
        p.Type.ShouldBe(GeometryType.Point);
        p.IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public void Point_Envelope_Is_Degenerate()
    {
        var p = new Point(10, 20);
        var env = p.GetEnvelope();
        env.MinX.ShouldBe(10);
        env.MaxX.ShouldBe(10);
        env.MinY.ShouldBe(20);
        env.MaxY.ShouldBe(20);
    }

    [Fact]
    public void Coordinate_ImplicitlyConvertsToPoint()
    {
        Point p = new Coordinate(-104.99, 39.74);
        p.X.ShouldBe(-104.99);
        p.Y.ShouldBe(39.74);
        p.Type.ShouldBe(GeometryType.Point);
    }

    [Fact]
    public void Coordinate_ImplicitlyConvertsToGeometry()
    {
        // a bare coordinate must flow into Geometry-typed parameters directly
        // (C# won't chain Coordinate -> Point -> Geometry)
        Geometry.Geometry g = new Coordinate(1, 2);
        g.ShouldBeOfType<Point>();
        ((Point)g).X.ShouldBe(1);
        ((Point)g).Y.ShouldBe(2);
    }

    [Fact]
    public void LineString_RequiresAtLeastTwoCoordinates()
    {
        var act = () => new LineString(new[] { new Coordinate(0, 0) });
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void LineString_Envelope()
    {
        var ls = new LineString(new[]
        {
            new Coordinate(0, 0),
            new Coordinate(10, 5),
            new Coordinate(20, -3)
        });

        var env = ls.GetEnvelope();
        env.MinX.ShouldBe(0);
        env.MaxX.ShouldBe(20);
        env.MinY.ShouldBe(-3);
        env.MaxY.ShouldBe(5);
    }

    [Fact]
    public void Polygon_RequiresAtLeastFourCoordinates()
    {
        var act = () => new Polygon(new[]
        {
            new Coordinate(0, 0),
            new Coordinate(1, 0),
            new Coordinate(0, 0)
        });
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Polygon_With_Holes()
    {
        var exterior = new[]
        {
            new Coordinate(0, 0), new Coordinate(10, 0),
            new Coordinate(10, 10), new Coordinate(0, 10),
            new Coordinate(0, 0)
        };
        var hole = new[]
        {
            new Coordinate(2, 2), new Coordinate(8, 2),
            new Coordinate(8, 8), new Coordinate(2, 8),
            new Coordinate(2, 2)
        };

        var pg = new Polygon(exterior, new[] { (IReadOnlyList<Coordinate>)hole });
        pg.ExteriorRing.Count.ShouldBe(5);
        pg.InteriorRings.Count.ShouldBe(1);
    }

    [Fact]
    public void Envelope_Contains_And_Intersects()
    {
        var env = new Envelope(0, 10, 0, 10);
        env.Contains(new Coordinate(5, 5)).ShouldBeTrue();
        env.Contains(new Coordinate(15, 5)).ShouldBeFalse();

        var other = new Envelope(5, 15, 5, 15);
        env.Intersects(other).ShouldBeTrue();

        var disjoint = new Envelope(20, 30, 20, 30);
        env.Intersects(disjoint).ShouldBeFalse();
    }

    [Fact]
    public void Envelope_ExpandToInclude()
    {
        var env = new Envelope(0, 10, 0, 10);
        var expanded = env.ExpandToInclude(new Coordinate(15, -5));
        expanded.MinX.ShouldBe(0);
        expanded.MaxX.ShouldBe(15);
        expanded.MinY.ShouldBe(-5);
        expanded.MaxY.ShouldBe(10);
    }

    [Fact]
    public void MultiPoint_Envelope()
    {
        var mp = new MultiPoint(new[]
        {
            new Point(1, 2),
            new Point(3, 4),
            new Point(-1, 0)
        });

        var env = mp.GetEnvelope();
        env.MinX.ShouldBe(-1);
        env.MaxX.ShouldBe(3);
        env.MinY.ShouldBe(0);
        env.MaxY.ShouldBe(4);
    }

    [Fact]
    public void GeometryCollection_Envelope()
    {
        var gc = new GeometryCollection(new Geometry.Geometry[]
        {
            new Point(0, 0),
            new Point(100, 50)
        });

        var env = gc.GetEnvelope();
        env.MinX.ShouldBe(0);
        env.MaxX.ShouldBe(100);
        env.MinY.ShouldBe(0);
        env.MaxY.ShouldBe(50);
    }
}
