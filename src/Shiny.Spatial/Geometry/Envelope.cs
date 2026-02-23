using System;

namespace Shiny.Spatial.Geometry;

public readonly struct Envelope : IEquatable<Envelope>
{
    public double MinX { get; }
    public double MaxX { get; }
    public double MinY { get; }
    public double MaxY { get; }

    public Envelope(double minX, double maxX, double minY, double maxY)
    {
        MinX = minX;
        MaxX = maxX;
        MinY = minY;
        MaxY = maxY;
    }

    public double Width => MaxX - MinX;
    public double Height => MaxY - MinY;
    public Coordinate Center => new((MinX + MaxX) / 2.0, (MinY + MaxY) / 2.0);

    public bool Contains(Coordinate c) =>
        c.X >= MinX && c.X <= MaxX && c.Y >= MinY && c.Y <= MaxY;

    public bool Intersects(Envelope other) =>
        MinX <= other.MaxX && MaxX >= other.MinX &&
        MinY <= other.MaxY && MaxY >= other.MinY;

    public Envelope ExpandToInclude(Coordinate c) =>
        new(
            Math.Min(MinX, c.X), Math.Max(MaxX, c.X),
            Math.Min(MinY, c.Y), Math.Max(MaxY, c.Y)
        );

    public Envelope ExpandToInclude(Envelope other) =>
        new(
            Math.Min(MinX, other.MinX), Math.Max(MaxX, other.MaxX),
            Math.Min(MinY, other.MinY), Math.Max(MaxY, other.MaxY)
        );

    public static Envelope Empty => new(
        double.MaxValue, double.MinValue,
        double.MaxValue, double.MinValue
    );

    public bool IsEmpty => MinX > MaxX || MinY > MaxY;

    public bool Equals(Envelope other) =>
        MinX.Equals(other.MinX) && MaxX.Equals(other.MaxX) &&
        MinY.Equals(other.MinY) && MaxY.Equals(other.MaxY);

    public override bool Equals(object? obj) =>
        obj is Envelope other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + MinX.GetHashCode();
            hash = hash * 31 + MaxX.GetHashCode();
            hash = hash * 31 + MinY.GetHashCode();
            hash = hash * 31 + MaxY.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(Envelope left, Envelope right) => left.Equals(right);
    public static bool operator !=(Envelope left, Envelope right) => !left.Equals(right);

    public override string ToString() => $"Envelope({MinX}, {MaxX}, {MinY}, {MaxY})";
}
