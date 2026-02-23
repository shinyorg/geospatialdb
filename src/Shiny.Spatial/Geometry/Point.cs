namespace Shiny.Spatial.Geometry;

public sealed class Point : Geometry
{
    public Coordinate Coordinate { get; }

    public Point(double x, double y) : this(new Coordinate(x, y)) { }

    public Point(Coordinate coordinate)
    {
        Coordinate = coordinate;
    }

    public double X => Coordinate.X;
    public double Y => Coordinate.Y;

    public override GeometryType Type => GeometryType.Point;
    public override bool IsEmpty => false;

    public override Envelope GetEnvelope() =>
        new(Coordinate.X, Coordinate.X, Coordinate.Y, Coordinate.Y);

    public override string ToString() => $"POINT ({X} {Y})";
}
