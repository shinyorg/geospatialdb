using System;
using System.Collections.Generic;

namespace Shiny.Spatial.Geometry;

public sealed class GeometryCollection : Geometry
{
    public IReadOnlyList<Geometry> Geometries { get; }

    public GeometryCollection(IReadOnlyList<Geometry> geometries)
    {
        Geometries = geometries ?? throw new ArgumentNullException(nameof(geometries));
    }

    public override GeometryType Type => GeometryType.GeometryCollection;
    public override bool IsEmpty => Geometries.Count == 0;

    public override Envelope GetEnvelope()
    {
        var env = Envelope.Empty;
        for (int i = 0; i < Geometries.Count; i++)
            env = env.ExpandToInclude(Geometries[i].GetEnvelope());
        return env;
    }

    public override string ToString() => $"GEOMETRYCOLLECTION ({Geometries.Count} geometries)";
}
