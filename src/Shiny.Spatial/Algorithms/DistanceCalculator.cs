using System;
using Shiny.Spatial.Geometry;

namespace Shiny.Spatial.Algorithms;

public static class DistanceCalculator
{
    private const double EarthRadiusMeters = 6_371_000.0;

    public static double Haversine(Coordinate a, Coordinate b)
    {
        double lat1 = ToRadians(a.Y);
        double lat2 = ToRadians(b.Y);
        double dLat = ToRadians(b.Y - a.Y);
        double dLon = ToRadians(b.X - a.X);

        double sinDLat = Math.Sin(dLat / 2);
        double sinDLon = Math.Sin(dLon / 2);
        double h = sinDLat * sinDLat + Math.Cos(lat1) * Math.Cos(lat2) * sinDLon * sinDLon;

        return 2.0 * EarthRadiusMeters * Math.Asin(Math.Sqrt(h));
    }

    public static double Euclidean(Coordinate a, Coordinate b)
    {
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public static double DistanceToSegment(Coordinate p, Coordinate a, Coordinate b)
    {
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;
        double lengthSq = dx * dx + dy * dy;

        if (lengthSq == 0)
            return Euclidean(p, a);

        double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lengthSq;
        t = Math.Max(0, Math.Min(1, t));

        var proj = new Coordinate(a.X + t * dx, a.Y + t * dy);
        return Euclidean(p, proj);
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
