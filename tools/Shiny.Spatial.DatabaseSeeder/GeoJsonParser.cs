using System.Text.Json;
using Shiny.Spatial.Geometry;
using GeoBase = Shiny.Spatial.Geometry.Geometry;

namespace Shiny.Spatial.DatabaseSeeder;

public static class GeoJsonParser
{
    public static List<ParsedFeature> ParseFeatureCollection(string geoJson)
    {
        using var doc = JsonDocument.Parse(geoJson);
        var root = doc.RootElement;
        var features = new List<ParsedFeature>();

        foreach (var feature in root.GetProperty("features").EnumerateArray())
        {
            var geomElement = feature.GetProperty("geometry");
            var propsElement = feature.GetProperty("properties");

            var geometry = ParseGeometry(geomElement);
            if (geometry == null)
                continue;

            var properties = new Dictionary<string, JsonElement>();
            foreach (var prop in propsElement.EnumerateObject())
                properties[prop.Name] = prop.Value.Clone();

            features.Add(new ParsedFeature(geometry, properties));
        }

        return features;
    }

    static GeoBase? ParseGeometry(JsonElement elem)
    {
        if (elem.ValueKind == JsonValueKind.Null)
            return null;

        var type = elem.GetProperty("type").GetString();
        var coordinates = elem.GetProperty("coordinates");

        return type switch
        {
            "Polygon" => ParsePolygon(coordinates),
            "MultiPolygon" => ParseMultiPolygon(coordinates),
            "Point" => ParsePoint(coordinates),
            _ => null
        };
    }

    static Polygon ParsePolygon(JsonElement coordinates)
    {
        var rings = new List<Coordinate[]>();
        foreach (var ring in coordinates.EnumerateArray())
        {
            var coords = new List<Coordinate>();
            foreach (var point in ring.EnumerateArray())
            {
                var lon = point[0].GetDouble();
                var lat = point[1].GetDouble();
                coords.Add(new Coordinate(lon, lat));
            }
            rings.Add(coords.ToArray());
        }

        if (rings.Count <= 1)
            return new Polygon(rings[0]);

        return new Polygon(rings[0], rings.Skip(1).ToArray());
    }

    static MultiPolygon ParseMultiPolygon(JsonElement coordinates)
    {
        var polygons = new List<Polygon>();
        foreach (var polyCoords in coordinates.EnumerateArray())
        {
            polygons.Add(ParsePolygon(polyCoords));
        }
        return new MultiPolygon(polygons.ToArray());
    }

    static Point ParsePoint(JsonElement coordinates)
    {
        var lon = coordinates[0].GetDouble();
        var lat = coordinates[1].GetDouble();
        return new Point(lon, lat);
    }
}

public record ParsedFeature(GeoBase Geometry, Dictionary<string, JsonElement> Properties);
