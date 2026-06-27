using System;

namespace Shiny.Spatial.Geometry;

public readonly struct Coordinate : IEquatable<Coordinate>
{
    public double X { get; }
    public double Y { get; }

    public Coordinate(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double Longitude => X;
    public double Latitude => Y;

    public bool Equals(Coordinate other) =>
        X.Equals(other.X) && Y.Equals(other.Y);

    public override bool Equals(object? obj) =>
        obj is Coordinate other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + X.GetHashCode();
            hash = hash * 31 + Y.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(Coordinate left, Coordinate right) => left.Equals(right);
    public static bool operator !=(Coordinate left, Coordinate right) => !left.Equals(right);

    /// <summary>
    /// Implicitly wraps a coordinate in a <see cref="Point"/> so a bare coordinate can be
    /// passed wherever a <see cref="Point"/> is expected.
    /// </summary>
    public static implicit operator Point(Coordinate coordinate) => new(coordinate);

    /// <summary>
    /// Implicitly wraps a coordinate in a <see cref="Point"/> (as a <see cref="Geometry"/>) so a
    /// bare coordinate can be passed to the geometry-based query methods (e.g. Intersecting,
    /// ContainedBy) without first constructing a <see cref="Point"/>. C# applies only one
    /// user-defined conversion in a chain, so this <c>Geometry</c>-targeted operator is required
    /// in addition to the <see cref="Point"/> one.
    /// </summary>
    public static implicit operator Geometry(Coordinate coordinate) => new Point(coordinate);

    public override string ToString() => $"({X}, {Y})";
}
