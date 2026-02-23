namespace Shiny.Spatial.Geometry;

public abstract class Geometry
{
    public abstract GeometryType Type { get; }
    public abstract Envelope GetEnvelope();
    public abstract bool IsEmpty { get; }
}
