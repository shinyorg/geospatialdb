using System;
using System.Collections.Generic;
using System.IO;
using Shiny.Spatial.Geometry;

namespace Shiny.Spatial.Serialization;

public static class WkbWriter
{
    private const byte LittleEndian = 1;

    public static byte[] Write(Geometry.Geometry geometry)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        WriteGeometry(writer, geometry);
        return ms.ToArray();
    }

    private static void WriteGeometry(BinaryWriter writer, Geometry.Geometry geometry)
    {
        switch (geometry)
        {
            case Point p: WritePoint(writer, p); break;
            case LineString ls: WriteLineString(writer, ls); break;
            case Polygon pg: WritePolygon(writer, pg); break;
            case MultiPoint mp: WriteMultiPoint(writer, mp); break;
            case MultiLineString mls: WriteMultiLineString(writer, mls); break;
            case MultiPolygon mpg: WriteMultiPolygon(writer, mpg); break;
            case GeometryCollection gc: WriteGeometryCollection(writer, gc); break;
            default: throw new ArgumentException($"Unsupported geometry type: {geometry.GetType().Name}");
        }
    }

    private static void WriteHeader(BinaryWriter writer, GeometryType type)
    {
        writer.Write(LittleEndian);
        writer.Write((int)type);
    }

    private static void WriteCoordinate(BinaryWriter writer, Coordinate c)
    {
        writer.Write(c.X);
        writer.Write(c.Y);
    }

    private static void WriteCoordinates(BinaryWriter writer, IReadOnlyList<Coordinate> coords)
    {
        writer.Write(coords.Count);
        for (int i = 0; i < coords.Count; i++)
            WriteCoordinate(writer, coords[i]);
    }

    private static void WritePoint(BinaryWriter writer, Point point)
    {
        WriteHeader(writer, GeometryType.Point);
        WriteCoordinate(writer, point.Coordinate);
    }

    private static void WriteLineString(BinaryWriter writer, LineString lineString)
    {
        WriteHeader(writer, GeometryType.LineString);
        WriteCoordinates(writer, lineString.Coordinates);
    }

    private static void WritePolygon(BinaryWriter writer, Polygon polygon)
    {
        WriteHeader(writer, GeometryType.Polygon);
        int numRings = 1 + polygon.InteriorRings.Count;
        writer.Write(numRings);
        WriteCoordinates(writer, polygon.ExteriorRing);
        for (int i = 0; i < polygon.InteriorRings.Count; i++)
            WriteCoordinates(writer, polygon.InteriorRings[i]);
    }

    private static void WriteMultiPoint(BinaryWriter writer, MultiPoint multiPoint)
    {
        WriteHeader(writer, GeometryType.MultiPoint);
        writer.Write(multiPoint.Points.Count);
        for (int i = 0; i < multiPoint.Points.Count; i++)
            WritePoint(writer, multiPoint.Points[i]);
    }

    private static void WriteMultiLineString(BinaryWriter writer, MultiLineString multiLineString)
    {
        WriteHeader(writer, GeometryType.MultiLineString);
        writer.Write(multiLineString.LineStrings.Count);
        for (int i = 0; i < multiLineString.LineStrings.Count; i++)
            WriteLineString(writer, multiLineString.LineStrings[i]);
    }

    private static void WriteMultiPolygon(BinaryWriter writer, MultiPolygon multiPolygon)
    {
        WriteHeader(writer, GeometryType.MultiPolygon);
        writer.Write(multiPolygon.Polygons.Count);
        for (int i = 0; i < multiPolygon.Polygons.Count; i++)
            WritePolygon(writer, multiPolygon.Polygons[i]);
    }

    private static void WriteGeometryCollection(BinaryWriter writer, GeometryCollection collection)
    {
        WriteHeader(writer, GeometryType.GeometryCollection);
        writer.Write(collection.Geometries.Count);
        for (int i = 0; i < collection.Geometries.Count; i++)
            WriteGeometry(writer, collection.Geometries[i]);
    }
}
