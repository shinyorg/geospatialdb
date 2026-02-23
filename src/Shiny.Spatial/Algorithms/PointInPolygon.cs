using System.Collections.Generic;
using Shiny.Spatial.Geometry;

namespace Shiny.Spatial.Algorithms;

public static class PointInPolygon
{
    public static bool Contains(Polygon polygon, Coordinate point)
    {
        if (!polygon.GetEnvelope().Contains(point))
            return false;

        if (!IsInsideRing(polygon.ExteriorRing, point))
            return false;

        for (int i = 0; i < polygon.InteriorRings.Count; i++)
        {
            if (IsInsideRing(polygon.InteriorRings[i], point))
                return false;
        }

        return true;
    }

    internal static bool IsInsideRing(IReadOnlyList<Coordinate> ring, Coordinate point)
    {
        bool inside = false;
        int count = ring.Count;

        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            double xi = ring[i].X, yi = ring[i].Y;
            double xj = ring[j].X, yj = ring[j].Y;

            if (((yi > point.Y) != (yj > point.Y)) &&
                (point.X < (xj - xi) * (point.Y - yi) / (yj - yi) + xi))
            {
                inside = !inside;
            }
        }

        return inside;
    }
}
