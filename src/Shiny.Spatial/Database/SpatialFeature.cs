using System.Collections.Generic;
using Shiny.Spatial.Geometry;

namespace Shiny.Spatial.Database;

public sealed class SpatialFeature
{
    public long Id { get; set; }
    public Geometry.Geometry Geometry { get; }
    public Dictionary<string, object?> Properties { get; }

    public SpatialFeature(Geometry.Geometry geometry)
        : this(geometry, new Dictionary<string, object?>()) { }

    public SpatialFeature(Geometry.Geometry geometry, Dictionary<string, object?> properties)
    {
        Geometry = geometry;
        Properties = properties;
    }
}
