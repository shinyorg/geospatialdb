using System;
using System.Collections.Generic;

namespace Shiny.Spatial.Geometry;

public sealed class MultiLineString : Geometry
{
    public IReadOnlyList<LineString> LineStrings { get; }

    public MultiLineString(IReadOnlyList<LineString> lineStrings)
    {
        LineStrings = lineStrings ?? throw new ArgumentNullException(nameof(lineStrings));
    }

    public override GeometryType Type => GeometryType.MultiLineString;
    public override bool IsEmpty => LineStrings.Count == 0;

    public override Envelope GetEnvelope()
    {
        var env = Envelope.Empty;
        for (int i = 0; i < LineStrings.Count; i++)
            env = env.ExpandToInclude(LineStrings[i].GetEnvelope());
        return env;
    }

    public override string ToString() => $"MULTILINESTRING ({LineStrings.Count} lines)";
}
