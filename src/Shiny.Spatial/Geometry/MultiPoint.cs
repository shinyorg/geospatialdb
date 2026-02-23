using System;
using System.Collections.Generic;

namespace Shiny.Spatial.Geometry;

public sealed class MultiPoint : Geometry
{
    public IReadOnlyList<Point> Points { get; }

    public MultiPoint(IReadOnlyList<Point> points)
    {
        Points = points ?? throw new ArgumentNullException(nameof(points));
    }

    public override GeometryType Type => GeometryType.MultiPoint;
    public override bool IsEmpty => Points.Count == 0;

    public override Envelope GetEnvelope()
    {
        var env = Envelope.Empty;
        for (int i = 0; i < Points.Count; i++)
            env = env.ExpandToInclude(Points[i].Coordinate);
        return env;
    }

    public override string ToString() => $"MULTIPOINT ({Points.Count} points)";
}
