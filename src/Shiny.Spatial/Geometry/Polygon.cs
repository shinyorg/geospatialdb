using System;
using System.Collections.Generic;

namespace Shiny.Spatial.Geometry;

public sealed class Polygon : Geometry
{
    public IReadOnlyList<Coordinate> ExteriorRing { get; }
    public IReadOnlyList<IReadOnlyList<Coordinate>> InteriorRings { get; }

    public Polygon(IReadOnlyList<Coordinate> exteriorRing)
        : this(exteriorRing, Array.Empty<IReadOnlyList<Coordinate>>()) { }

    public Polygon(IReadOnlyList<Coordinate> exteriorRing, IReadOnlyList<IReadOnlyList<Coordinate>> interiorRings)
    {
        if (exteriorRing == null) throw new ArgumentNullException(nameof(exteriorRing));
        if (exteriorRing.Count < 4) throw new ArgumentException("Polygon exterior ring requires at least 4 coordinates (closed ring).", nameof(exteriorRing));
        ExteriorRing = exteriorRing;
        InteriorRings = interiorRings ?? Array.Empty<IReadOnlyList<Coordinate>>();
    }

    public override GeometryType Type => GeometryType.Polygon;
    public override bool IsEmpty => ExteriorRing.Count == 0;

    public override Envelope GetEnvelope()
    {
        var env = Envelope.Empty;
        for (int i = 0; i < ExteriorRing.Count; i++)
            env = env.ExpandToInclude(ExteriorRing[i]);
        return env;
    }

    public override string ToString() =>
        $"POLYGON ({ExteriorRing.Count} ext points, {InteriorRings.Count} holes)";
}
