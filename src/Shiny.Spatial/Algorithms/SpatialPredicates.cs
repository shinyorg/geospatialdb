using System.Collections.Generic;
using Shiny.Spatial.Geometry;

namespace Shiny.Spatial.Algorithms;

public static class SpatialPredicates
{
    public static bool Intersects(Geometry.Geometry a, Geometry.Geometry b)
    {
        if (!a.GetEnvelope().Intersects(b.GetEnvelope()))
            return false;

        return (a, b) switch
        {
            (Point pa, Point pb) => pa.Coordinate == pb.Coordinate,
            (Point p, Polygon pg) => PointInPolygon.Contains(pg, p.Coordinate),
            (Polygon pg, Point p) => PointInPolygon.Contains(pg, p.Coordinate),
            (Point p, LineString ls) => PointIntersectsLineString(p, ls),
            (LineString ls, Point p) => PointIntersectsLineString(p, ls),
            (LineString a1, LineString b1) => LineStringIntersectsLineString(a1, b1),
            (LineString ls, Polygon pg) => LineStringIntersectsPolygon(ls, pg),
            (Polygon pg, LineString ls) => LineStringIntersectsPolygon(ls, pg),
            (Polygon a1, Polygon b1) => PolygonIntersectsPolygon(a1, b1),

            (MultiPoint mp, _) => AnyIntersects(mp.Points, b),
            (_, MultiPoint mp) => AnyIntersects(mp.Points, a),
            (MultiLineString mls, _) => AnyIntersects(mls.LineStrings, b),
            (_, MultiLineString mls) => AnyIntersects(mls.LineStrings, a),
            (MultiPolygon mpg, _) => AnyIntersects(mpg.Polygons, b),
            (_, MultiPolygon mpg) => AnyIntersects(mpg.Polygons, a),
            (GeometryCollection gc, _) => AnyIntersects(gc.Geometries, b),
            (_, GeometryCollection gc) => AnyIntersects(gc.Geometries, a),

            _ => false
        };
    }

    public static bool Contains(Geometry.Geometry container, Geometry.Geometry contained)
    {
        if (!container.GetEnvelope().Intersects(contained.GetEnvelope()))
            return false;

        return (container, contained) switch
        {
            (Polygon pg, Point p) => PointInPolygon.Contains(pg, p.Coordinate),
            (Polygon pg, LineString ls) => PolygonContainsLineString(pg, ls),
            (Polygon outer, Polygon inner) => PolygonContainsPolygon(outer, inner),
            (Polygon pg, MultiPoint mp) => AllContained(pg, mp.Points),
            (Polygon pg, MultiLineString mls) => AllContained(pg, mls.LineStrings),
            (Polygon pg, MultiPolygon mpg) => AllContained(pg, mpg.Polygons),
            (Polygon pg, GeometryCollection gc) => AllContained(pg, gc.Geometries),

            (MultiPolygon mpg, _) => AnyContains(mpg.Polygons, contained),
            (GeometryCollection gc, _) => AnyContains(gc.Geometries, contained),

            _ => false
        };
    }

    private static bool PointIntersectsLineString(Point point, LineString lineString)
    {
        const double epsilon = 1e-10;
        for (int i = 0; i < lineString.Coordinates.Count - 1; i++)
        {
            double dist = DistanceCalculator.DistanceToSegment(
                point.Coordinate,
                lineString.Coordinates[i],
                lineString.Coordinates[i + 1]);
            if (dist < epsilon)
                return true;
        }
        return false;
    }

    private static bool LineStringIntersectsLineString(LineString a, LineString b)
    {
        for (int i = 0; i < a.Coordinates.Count - 1; i++)
        {
            for (int j = 0; j < b.Coordinates.Count - 1; j++)
            {
                if (SegmentIntersection.Intersects(
                    a.Coordinates[i], a.Coordinates[i + 1],
                    b.Coordinates[j], b.Coordinates[j + 1]))
                    return true;
            }
        }
        return false;
    }

    private static bool LineStringIntersectsPolygon(LineString ls, Polygon pg)
    {
        // Check if any linestring point is inside the polygon
        for (int i = 0; i < ls.Coordinates.Count; i++)
        {
            if (PointInPolygon.Contains(pg, ls.Coordinates[i]))
                return true;
        }

        // Check if any linestring segment crosses any polygon ring segment
        for (int i = 0; i < ls.Coordinates.Count - 1; i++)
        {
            if (SegmentIntersectsRing(ls.Coordinates[i], ls.Coordinates[i + 1], pg.ExteriorRing))
                return true;
        }

        return false;
    }

    private static bool PolygonIntersectsPolygon(Polygon a, Polygon b)
    {
        // Check if any vertex of a is inside b
        for (int i = 0; i < a.ExteriorRing.Count; i++)
        {
            if (PointInPolygon.Contains(b, a.ExteriorRing[i]))
                return true;
        }

        // Check if any vertex of b is inside a
        for (int i = 0; i < b.ExteriorRing.Count; i++)
        {
            if (PointInPolygon.Contains(a, b.ExteriorRing[i]))
                return true;
        }

        // Check edge intersections
        for (int i = 0; i < a.ExteriorRing.Count - 1; i++)
        {
            if (SegmentIntersectsRing(a.ExteriorRing[i], a.ExteriorRing[i + 1], b.ExteriorRing))
                return true;
        }

        return false;
    }

    private static bool PolygonContainsLineString(Polygon pg, LineString ls)
    {
        for (int i = 0; i < ls.Coordinates.Count; i++)
        {
            if (!PointInPolygon.Contains(pg, ls.Coordinates[i]))
                return false;
        }
        return true;
    }

    private static bool PolygonContainsPolygon(Polygon outer, Polygon inner)
    {
        for (int i = 0; i < inner.ExteriorRing.Count; i++)
        {
            if (!PointInPolygon.Contains(outer, inner.ExteriorRing[i]))
                return false;
        }
        return true;
    }

    private static bool SegmentIntersectsRing(Coordinate a, Coordinate b, IReadOnlyList<Coordinate> ring)
    {
        for (int i = 0; i < ring.Count - 1; i++)
        {
            if (SegmentIntersection.Intersects(a, b, ring[i], ring[i + 1]))
                return true;
        }
        return false;
    }

    private static bool AnyIntersects<T>(IReadOnlyList<T> geometries, Geometry.Geometry other) where T : Geometry.Geometry
    {
        for (int i = 0; i < geometries.Count; i++)
        {
            if (Intersects(geometries[i], other))
                return true;
        }
        return false;
    }

    private static bool AnyContains<T>(IReadOnlyList<T> containers, Geometry.Geometry contained) where T : Geometry.Geometry
    {
        for (int i = 0; i < containers.Count; i++)
        {
            if (Contains(containers[i], contained))
                return true;
        }
        return false;
    }

    private static bool AllContained<T>(Polygon container, IReadOnlyList<T> geometries) where T : Geometry.Geometry
    {
        for (int i = 0; i < geometries.Count; i++)
        {
            if (!Contains(container, geometries[i]))
                return false;
        }
        return true;
    }
}
