using System;
using System.Collections.Generic;

namespace Shiny.Spatial.Geometry;

public sealed class LineString : Geometry
{
    public IReadOnlyList<Coordinate> Coordinates { get; }

    public LineString(IReadOnlyList<Coordinate> coordinates)
    {
        if (coordinates == null) throw new ArgumentNullException(nameof(coordinates));
        if (coordinates.Count < 2) throw new ArgumentException("LineString requires at least 2 coordinates.", nameof(coordinates));
        Coordinates = coordinates;
    }

    public override GeometryType Type => GeometryType.LineString;
    public override bool IsEmpty => Coordinates.Count == 0;

    public bool IsClosed =>
        Coordinates.Count >= 4 &&
        Coordinates[0] == Coordinates[Coordinates.Count - 1];

    public override Envelope GetEnvelope()
    {
        var env = Envelope.Empty;
        for (int i = 0; i < Coordinates.Count; i++)
            env = env.ExpandToInclude(Coordinates[i]);
        return env;
    }

    public override string ToString() => $"LINESTRING ({Coordinates.Count} points)";
}
