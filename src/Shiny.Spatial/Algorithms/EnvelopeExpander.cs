using System;
using Shiny.Spatial.Database;
using Shiny.Spatial.Geometry;

namespace Shiny.Spatial.Algorithms;

public static class EnvelopeExpander
{
    public static Envelope ExpandByDistance(Envelope envelope, double distanceMeters, CoordinateSystem coordSystem)
    {
        if (coordSystem == CoordinateSystem.Cartesian)
        {
            return new Envelope(
                envelope.MinX - distanceMeters,
                envelope.MaxX + distanceMeters,
                envelope.MinY - distanceMeters,
                envelope.MaxY + distanceMeters
            );
        }

        // WGS84: convert meters to approximate degrees
        double latExpand = distanceMeters / 111_320.0;
        double midLat = (envelope.MinY + envelope.MaxY) / 2.0;
        double lonExpand = distanceMeters / (111_320.0 * Math.Cos(midLat * Math.PI / 180.0));

        return new Envelope(
            envelope.MinX - lonExpand,
            envelope.MaxX + lonExpand,
            envelope.MinY - latExpand,
            envelope.MaxY + latExpand
        );
    }
}
