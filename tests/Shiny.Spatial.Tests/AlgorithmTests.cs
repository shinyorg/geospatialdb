using System.Collections.Generic;
using Shouldly;
using Shiny.Spatial.Algorithms;
using Shiny.Spatial.Geometry;
using Xunit;

namespace Shiny.Spatial.Tests;

public class AlgorithmTests
{
    [Fact]
    public void Haversine_London_To_Paris()
    {
        // London: -0.1276, 51.5074  Paris: 2.3522, 48.8566
        var london = new Coordinate(-0.1276, 51.5074);
        var paris = new Coordinate(2.3522, 48.8566);

        double distance = DistanceCalculator.Haversine(london, paris);

        // ~343 km known distance
        distance.ShouldBe(343_560, 1000);
    }

    [Fact]
    public void Haversine_Same_Point_Is_Zero()
    {
        var point = new Coordinate(-104.99, 39.74);
        DistanceCalculator.Haversine(point, point).ShouldBe(0);
    }

    [Fact]
    public void Euclidean_Distance()
    {
        var a = new Coordinate(0, 0);
        var b = new Coordinate(3, 4);
        DistanceCalculator.Euclidean(a, b).ShouldBe(5.0, 1e-10);
    }

    [Fact]
    public void PointInPolygon_Denver_In_Colorado()
    {
        // Simplified Colorado bounding polygon
        var colorado = new Polygon(new[]
        {
            new Coordinate(-109.05, 37.0), new Coordinate(-102.05, 37.0),
            new Coordinate(-102.05, 41.0), new Coordinate(-109.05, 41.0),
            new Coordinate(-109.05, 37.0)
        });

        var denver = new Coordinate(-104.99, 39.74);
        var newYork = new Coordinate(-74.006, 40.7128);

        PointInPolygon.Contains(colorado, denver).ShouldBeTrue();
        PointInPolygon.Contains(colorado, newYork).ShouldBeFalse();
    }

    [Fact]
    public void PointInPolygon_With_Hole()
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
        var polygon = new Polygon(exterior, new[] { (IReadOnlyList<Coordinate>)hole });

        // Inside exterior, outside hole
        PointInPolygon.Contains(polygon, new Coordinate(2, 2)).ShouldBeTrue();

        // Inside hole (should not be contained)
        PointInPolygon.Contains(polygon, new Coordinate(10, 10)).ShouldBeFalse();

        // Outside exterior
        PointInPolygon.Contains(polygon, new Coordinate(25, 25)).ShouldBeFalse();
    }

    [Fact]
    public void SegmentIntersection_Crossing()
    {
        var a1 = new Coordinate(0, 0);
        var a2 = new Coordinate(10, 10);
        var b1 = new Coordinate(0, 10);
        var b2 = new Coordinate(10, 0);

        SegmentIntersection.Intersects(a1, a2, b1, b2).ShouldBeTrue();
    }

    [Fact]
    public void SegmentIntersection_Parallel_No_Intersect()
    {
        var a1 = new Coordinate(0, 0);
        var a2 = new Coordinate(10, 0);
        var b1 = new Coordinate(0, 5);
        var b2 = new Coordinate(10, 5);

        SegmentIntersection.Intersects(a1, a2, b1, b2).ShouldBeFalse();
    }

    [Fact]
    public void SpatialPredicates_Point_In_Polygon()
    {
        var polygon = new Polygon(new[]
        {
            new Coordinate(0, 0), new Coordinate(10, 0),
            new Coordinate(10, 10), new Coordinate(0, 10),
            new Coordinate(0, 0)
        });

        var inside = new Point(5, 5);
        var outside = new Point(15, 15);

        SpatialPredicates.Intersects(inside, polygon).ShouldBeTrue();
        SpatialPredicates.Intersects(outside, polygon).ShouldBeFalse();
        SpatialPredicates.Contains(polygon, inside).ShouldBeTrue();
        SpatialPredicates.Contains(polygon, outside).ShouldBeFalse();
    }

    [Fact]
    public void SpatialPredicates_Polygon_Intersects_Polygon()
    {
        var a = new Polygon(new[]
        {
            new Coordinate(0, 0), new Coordinate(10, 0),
            new Coordinate(10, 10), new Coordinate(0, 10),
            new Coordinate(0, 0)
        });
        var b = new Polygon(new[]
        {
            new Coordinate(5, 5), new Coordinate(15, 5),
            new Coordinate(15, 15), new Coordinate(5, 15),
            new Coordinate(5, 5)
        });
        var c = new Polygon(new[]
        {
            new Coordinate(20, 20), new Coordinate(30, 20),
            new Coordinate(30, 30), new Coordinate(20, 30),
            new Coordinate(20, 20)
        });

        SpatialPredicates.Intersects(a, b).ShouldBeTrue();
        SpatialPredicates.Intersects(a, c).ShouldBeFalse();
    }
}
