using System;
using System.Collections.Generic;
using System.IO;
using Shiny.Spatial.Geometry;

namespace Shiny.Spatial.Serialization;

public static class WkbReader
{
    public static Geometry.Geometry Read(byte[] wkb)
    {
        using var ms = new MemoryStream(wkb);
        using var reader = new BinaryReader(ms);
        return ReadGeometry(reader);
    }

    private static Geometry.Geometry ReadGeometry(BinaryReader reader)
    {
        byte byteOrder = reader.ReadByte();
        bool isLittleEndian = byteOrder == 1;

        int typeInt = ReadInt32(reader, isLittleEndian);
        var type = (GeometryType)(typeInt & 0xFF);

        return type switch
        {
            GeometryType.Point => ReadPoint(reader, isLittleEndian),
            GeometryType.LineString => ReadLineString(reader, isLittleEndian),
            GeometryType.Polygon => ReadPolygon(reader, isLittleEndian),
            GeometryType.MultiPoint => ReadMultiPoint(reader, isLittleEndian),
            GeometryType.MultiLineString => ReadMultiLineString(reader, isLittleEndian),
            GeometryType.MultiPolygon => ReadMultiPolygon(reader, isLittleEndian),
            GeometryType.GeometryCollection => ReadGeometryCollection(reader, isLittleEndian),
            _ => throw new InvalidDataException($"Unknown WKB geometry type: {typeInt}")
        };
    }

    private static Coordinate ReadCoordinate(BinaryReader reader, bool isLittleEndian) =>
        new(ReadDouble(reader, isLittleEndian), ReadDouble(reader, isLittleEndian));

    private static Coordinate[] ReadCoordinates(BinaryReader reader, bool isLittleEndian)
    {
        int count = ReadInt32(reader, isLittleEndian);
        var coords = new Coordinate[count];
        for (int i = 0; i < count; i++)
            coords[i] = ReadCoordinate(reader, isLittleEndian);
        return coords;
    }

    private static Point ReadPoint(BinaryReader reader, bool isLittleEndian) =>
        new(ReadCoordinate(reader, isLittleEndian));

    private static LineString ReadLineString(BinaryReader reader, bool isLittleEndian) =>
        new(ReadCoordinates(reader, isLittleEndian));

    private static Polygon ReadPolygon(BinaryReader reader, bool isLittleEndian)
    {
        int numRings = ReadInt32(reader, isLittleEndian);
        var exterior = ReadCoordinates(reader, isLittleEndian);

        if (numRings <= 1)
            return new Polygon(exterior);

        var holes = new IReadOnlyList<Coordinate>[numRings - 1];
        for (int i = 0; i < numRings - 1; i++)
            holes[i] = ReadCoordinates(reader, isLittleEndian);

        return new Polygon(exterior, holes);
    }

    private static MultiPoint ReadMultiPoint(BinaryReader reader, bool isLittleEndian)
    {
        int count = ReadInt32(reader, isLittleEndian);
        var points = new Point[count];
        for (int i = 0; i < count; i++)
        {
            reader.ReadByte(); // byte order
            ReadInt32(reader, isLittleEndian); // type
            points[i] = ReadPoint(reader, isLittleEndian);
        }
        return new MultiPoint(points);
    }

    private static MultiLineString ReadMultiLineString(BinaryReader reader, bool isLittleEndian)
    {
        int count = ReadInt32(reader, isLittleEndian);
        var lines = new LineString[count];
        for (int i = 0; i < count; i++)
        {
            reader.ReadByte();
            ReadInt32(reader, isLittleEndian);
            lines[i] = ReadLineString(reader, isLittleEndian);
        }
        return new MultiLineString(lines);
    }

    private static MultiPolygon ReadMultiPolygon(BinaryReader reader, bool isLittleEndian)
    {
        int count = ReadInt32(reader, isLittleEndian);
        var polygons = new Polygon[count];
        for (int i = 0; i < count; i++)
        {
            reader.ReadByte();
            ReadInt32(reader, isLittleEndian);
            polygons[i] = ReadPolygon(reader, isLittleEndian);
        }
        return new MultiPolygon(polygons);
    }

    private static GeometryCollection ReadGeometryCollection(BinaryReader reader, bool isLittleEndian)
    {
        int count = ReadInt32(reader, isLittleEndian);
        var geometries = new Geometry.Geometry[count];
        for (int i = 0; i < count; i++)
            geometries[i] = ReadGeometry(reader);
        return new GeometryCollection(geometries);
    }

    private static int ReadInt32(BinaryReader reader, bool isLittleEndian)
    {
        var bytes = reader.ReadBytes(4);
        if (BitConverter.IsLittleEndian != isLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }

    private static double ReadDouble(BinaryReader reader, bool isLittleEndian)
    {
        var bytes = reader.ReadBytes(8);
        if (BitConverter.IsLittleEndian != isLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToDouble(bytes, 0);
    }
}
