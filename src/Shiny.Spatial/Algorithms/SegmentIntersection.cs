using Shiny.Spatial.Geometry;

namespace Shiny.Spatial.Algorithms;

public static class SegmentIntersection
{
    public static bool Intersects(Coordinate a1, Coordinate a2, Coordinate b1, Coordinate b2)
    {
        double d1 = CrossProduct(b1, b2, a1);
        double d2 = CrossProduct(b1, b2, a2);
        double d3 = CrossProduct(a1, a2, b1);
        double d4 = CrossProduct(a1, a2, b2);

        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
            ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            return true;

        if (d1 == 0 && OnSegment(b1, b2, a1)) return true;
        if (d2 == 0 && OnSegment(b1, b2, a2)) return true;
        if (d3 == 0 && OnSegment(a1, a2, b1)) return true;
        if (d4 == 0 && OnSegment(a1, a2, b2)) return true;

        return false;
    }

    private static double CrossProduct(Coordinate o, Coordinate a, Coordinate b) =>
        (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);

    private static bool OnSegment(Coordinate p, Coordinate q, Coordinate r)
    {
        return r.X <= Max(p.X, q.X) && r.X >= Min(p.X, q.X) &&
               r.Y <= Max(p.Y, q.Y) && r.Y >= Min(p.Y, q.Y);
    }

    private static double Min(double a, double b) => a < b ? a : b;
    private static double Max(double a, double b) => a > b ? a : b;
}
