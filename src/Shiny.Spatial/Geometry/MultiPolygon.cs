using System;
using System.Collections.Generic;

namespace Shiny.Spatial.Geometry;

public sealed class MultiPolygon : Geometry
{
    public IReadOnlyList<Polygon> Polygons { get; }

    public MultiPolygon(IReadOnlyList<Polygon> polygons)
    {
        Polygons = polygons ?? throw new ArgumentNullException(nameof(polygons));
    }

    public override GeometryType Type => GeometryType.MultiPolygon;
    public override bool IsEmpty => Polygons.Count == 0;

    public override Envelope GetEnvelope()
    {
        var env = Envelope.Empty;
        for (int i = 0; i < Polygons.Count; i++)
            env = env.ExpandToInclude(Polygons[i].GetEnvelope());
        return env;
    }

    public override string ToString() => $"MULTIPOLYGON ({Polygons.Count} polygons)";
}
