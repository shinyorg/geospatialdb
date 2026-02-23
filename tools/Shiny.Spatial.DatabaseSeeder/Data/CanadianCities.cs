using System.Collections.Generic;

namespace Shiny.Spatial.DatabaseSeeder.Data;

public static class CanadianCities
{
    public static IReadOnlyList<CanadianCityData> GetAll() => _cities;

    private static readonly CanadianCityData[] _cities = new[]
    {
        new CanadianCityData("Toronto", "ON", 2794356, -79.3832, 43.6532),
        new CanadianCityData("Montreal", "QC", 1762949, -73.5673, 45.5017),
        new CanadianCityData("Vancouver", "BC", 662248, -123.1216, 49.2827),
        new CanadianCityData("Calgary", "AB", 1306784, -114.0719, 51.0447),
        new CanadianCityData("Edmonton", "AB", 1010899, -113.4909, 53.5461),
        new CanadianCityData("Ottawa", "ON", 1017449, -75.6972, 45.4215),
        new CanadianCityData("Winnipeg", "MB", 749607, -97.1384, 49.8951),
        new CanadianCityData("Quebec City", "QC", 549459, -71.2080, 46.8139),
        new CanadianCityData("Hamilton", "ON", 569353, -79.8711, 43.2557),
        new CanadianCityData("Kitchener", "ON", 256885, -80.4834, 43.4516),
        new CanadianCityData("London", "ON", 422324, -81.2453, 42.9849),
        new CanadianCityData("Victoria", "BC", 91867, -123.3656, 48.4284),
        new CanadianCityData("Halifax", "NS", 439819, -63.5752, 44.6488),
        new CanadianCityData("Oshawa", "ON", 175383, -78.8658, 43.8971),
        new CanadianCityData("Windsor", "ON", 229660, -83.0364, 42.3149),
        new CanadianCityData("Saskatoon", "SK", 317480, -106.6700, 52.1332),
        new CanadianCityData("Regina", "SK", 226404, -104.6189, 50.4452),
        new CanadianCityData("St. John's", "NL", 110525, -52.7126, 47.5615),
        new CanadianCityData("Barrie", "ON", 147829, -79.6903, 44.3894),
        new CanadianCityData("Kelowna", "BC", 144576, -119.4960, 49.8880),
        new CanadianCityData("Abbotsford", "BC", 153524, -122.3045, 49.0504),
        new CanadianCityData("Kingston", "ON", 132485, -76.4860, 44.2312),
        new CanadianCityData("Sudbury", "ON", 166004, -81.0100, 46.4917),
        new CanadianCityData("Thunder Bay", "ON", 108843, -89.2477, 48.3809),
        new CanadianCityData("Sherbrooke", "QC", 172950, -71.8929, 45.4042),
        new CanadianCityData("Trois-Rivi\u00e8res", "QC", 140420, -72.5477, 46.3432),
        new CanadianCityData("Guelph", "ON", 143740, -80.2482, 43.5448),
        new CanadianCityData("Moncton", "NB", 79470, -64.7782, 46.0878),
        new CanadianCityData("Fredericton", "NB", 63116, -66.6431, 45.9636),
        new CanadianCityData("Charlottetown", "PE", 38809, -63.1311, 46.2382),
        new CanadianCityData("Red Deer", "AB", 100844, -113.8110, 52.2681),
        new CanadianCityData("Lethbridge", "AB", 101482, -112.8328, 49.6935),
        new CanadianCityData("Kamloops", "BC", 97902, -120.3273, 50.6745),
        new CanadianCityData("Prince George", "BC", 86622, -122.7497, 53.9171),
        new CanadianCityData("Medicine Hat", "AB", 63260, -110.6764, 50.0405),
        new CanadianCityData("Drummondville", "QC", 79664, -72.4843, 45.8838),
        new CanadianCityData("Saint John", "NB", 69895, -66.0633, 45.2733),
        new CanadianCityData("Brantford", "ON", 104688, -80.2636, 43.1394),
        new CanadianCityData("Peterborough", "ON", 83651, -78.3197, 44.3091),
        new CanadianCityData("Nanaimo", "BC", 99863, -123.9401, 49.1659),
        new CanadianCityData("Chilliwack", "BC", 93203, -121.9514, 49.1579),
        new CanadianCityData("Belleville", "ON", 55071, -77.3832, 44.1628),
        new CanadianCityData("Sarnia", "ON", 72047, -82.4066, 42.9745),
        new CanadianCityData("Sault Ste. Marie", "ON", 72051, -84.3358, 46.5136),
        new CanadianCityData("North Bay", "ON", 52662, -79.4608, 46.3091),
        new CanadianCityData("Cornwall", "ON", 47845, -74.7280, 45.0181),
        new CanadianCityData("Timmins", "ON", 41145, -81.3305, 48.4758),
        new CanadianCityData("Chatham-Kent", "ON", 104316, -82.1849, 42.4048),
        new CanadianCityData("Saint-Hyacinthe", "QC", 57000, -72.9554, 45.6307),
        new CanadianCityData("Cape Breton", "NS", 94285, -60.1947, 46.1351),
    };
}

public record CanadianCityData(string Name, string ProvinceAbbreviation, long Population, double Longitude, double Latitude);
