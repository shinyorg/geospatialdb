using System;
using System.Collections.Generic;
using FluentAssertions;
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

        a.Should().Be(b);
        a.Should().NotBe(c);
        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();
    }

    [Fact]
    public void Coordinate_LonLat_Aliases()
    {
        var c = new Coordinate(-104.99, 39.74);
        c.Longitude.Should().Be(-104.99);
        c.Latitude.Should().Be(39.74);
    }

    [Fact]
    public void Point_Properties()
    {
        var p = new Point(-104.99, 39.74);
        p.X.Should().Be(-104.99);
        p.Y.Should().Be(39.74);
        p.Type.Should().Be(GeometryType.Point);
        p.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void Point_Envelope_Is_Degenerate()
    {
        var p = new Point(10, 20);
        var env = p.GetEnvelope();
        env.MinX.Should().Be(10);
        env.MaxX.Should().Be(10);
        env.MinY.Should().Be(20);
        env.MaxY.Should().Be(20);
    }

    [Fact]
    public void LineString_RequiresAtLeastTwoCoordinates()
    {
        var act = () => new LineString(new[] { new Coordinate(0, 0) });
        act.Should().Throw<ArgumentException>();
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
        env.MinX.Should().Be(0);
        env.MaxX.Should().Be(20);
        env.MinY.Should().Be(-3);
        env.MaxY.Should().Be(5);
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
        act.Should().Throw<ArgumentException>();
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
        pg.ExteriorRing.Count.Should().Be(5);
        pg.InteriorRings.Count.Should().Be(1);
    }

    [Fact]
    public void Envelope_Contains_And_Intersects()
    {
        var env = new Envelope(0, 10, 0, 10);
        env.Contains(new Coordinate(5, 5)).Should().BeTrue();
        env.Contains(new Coordinate(15, 5)).Should().BeFalse();

        var other = new Envelope(5, 15, 5, 15);
        env.Intersects(other).Should().BeTrue();

        var disjoint = new Envelope(20, 30, 20, 30);
        env.Intersects(disjoint).Should().BeFalse();
    }

    [Fact]
    public void Envelope_ExpandToInclude()
    {
        var env = new Envelope(0, 10, 0, 10);
        var expanded = env.ExpandToInclude(new Coordinate(15, -5));
        expanded.MinX.Should().Be(0);
        expanded.MaxX.Should().Be(15);
        expanded.MinY.Should().Be(-5);
        expanded.MaxY.Should().Be(10);
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
        env.MinX.Should().Be(-1);
        env.MaxX.Should().Be(3);
        env.MinY.Should().Be(0);
        env.MaxY.Should().Be(4);
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
        env.MinX.Should().Be(0);
        env.MaxX.Should().Be(100);
        env.MinY.Should().Be(0);
        env.MaxY.Should().Be(50);
    }
}
