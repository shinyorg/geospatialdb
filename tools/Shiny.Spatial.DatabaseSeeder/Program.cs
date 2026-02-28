using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using Shiny.Spatial.Database;
using Shiny.Spatial.DatabaseSeeder;
using Shiny.Spatial.DatabaseSeeder.Data;
using Shiny.Spatial.Geometry;

string outputDir = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "databases");

Directory.CreateDirectory(outputDir);

Console.WriteLine($"Output directory: {Path.GetFullPath(outputDir)}");
Console.WriteLine();

using var httpClient = new HttpClient();
httpClient.Timeout = TimeSpan.FromMinutes(10);
httpClient.DefaultRequestHeaders.Add("User-Agent", "Shiny.Spatial.DatabaseSeeder/1.0");

SeedUsStates(outputDir);
SeedCanadianProvinces(outputDir);
await SeedUsCitiesAsync(outputDir, httpClient);
await SeedCanadianCitiesAsync(outputDir, httpClient);

Console.WriteLine();
Console.WriteLine("All databases seeded successfully.");

// Verification queries
Console.WriteLine();
Console.WriteLine("--- Verification ---");
VerifyDatabase(Path.Combine(outputDir, "us-cities.db"), "cities", "New York City", -74.0060, 40.7128);
VerifyDatabase(Path.Combine(outputDir, "us-cities.db"), "cities", "Los Angeles", -118.2437, 34.0522);
VerifyDatabase(Path.Combine(outputDir, "ca-cities.db"), "cities", "Toronto", -79.3832, 43.6532);
VerifyDatabase(Path.Combine(outputDir, "ca-cities.db"), "cities", "Montreal", -73.5673, 45.5017);

static void VerifyDatabase(string path, string tableName, string expectedCity, double lon, double lat)
{
    using var db = new SpatialDatabase(path);
    var table = db.GetTable(tableName);
    var results = table.FindIntersecting(new Point(lon, lat));
    if (results.Count > 0)
    {
        var names = string.Join(", ", results.Select(r => $"{r.Properties["name"]}"));
        Console.WriteLine($"  Point ({lat:F4}, {lon:F4}) -> {names}");
    }
    else
    {
        Console.WriteLine($"  Point ({lat:F4}, {lon:F4}) -> NO MATCH (expected: {expectedCity})");
    }
}

static void SeedUsStates(string outputDir)
{
    var path = Path.Combine(outputDir, "us-states.db");
    if (File.Exists(path)) File.Delete(path);

    using var db = new SpatialDatabase(path);
    var table = db.CreateTable("states", CoordinateSystem.Wgs84,
        new PropertyDefinition("name", PropertyType.Text),
        new PropertyDefinition("abbreviation", PropertyType.Text),
        new PropertyDefinition("population", PropertyType.Integer));

    var states = UsStates.GetAll();
    var features = new List<SpatialFeature>();

    foreach (var state in states)
    {
        var coords = state.Boundary
            .Select(b => new Coordinate(b.Lon, b.Lat))
            .ToArray();

        var polygon = new Polygon(coords);
        features.Add(new SpatialFeature(polygon)
        {
            Properties =
            {
                ["name"] = state.Name,
                ["abbreviation"] = state.Abbreviation,
                ["population"] = state.Population
            }
        });
    }

    table.BulkInsert(features);
    Console.WriteLine($"US States:           {features.Count} records -> {Path.GetFileName(path)}");
}

static void SeedCanadianProvinces(string outputDir)
{
    var path = Path.Combine(outputDir, "ca-provinces.db");
    if (File.Exists(path)) File.Delete(path);

    using var db = new SpatialDatabase(path);
    var table = db.CreateTable("provinces", CoordinateSystem.Wgs84,
        new PropertyDefinition("name", PropertyType.Text),
        new PropertyDefinition("abbreviation", PropertyType.Text),
        new PropertyDefinition("population", PropertyType.Integer));

    var provinces = CanadianProvinces.GetAll();
    var features = new List<SpatialFeature>();

    foreach (var prov in provinces)
    {
        var coords = prov.Boundary
            .Select(b => new Coordinate(b.Lon, b.Lat))
            .ToArray();

        var polygon = new Polygon(coords);
        features.Add(new SpatialFeature(polygon)
        {
            Properties =
            {
                ["name"] = prov.Name,
                ["abbreviation"] = prov.Abbreviation,
                ["population"] = prov.Population
            }
        });
    }

    table.BulkInsert(features);
    Console.WriteLine($"Canadian Provinces:  {features.Count} records -> {Path.GetFileName(path)}");
}

static async Task SeedUsCitiesAsync(string outputDir, HttpClient httpClient)
{
    var path = Path.Combine(outputDir, "us-cities.db");
    if (File.Exists(path)) File.Delete(path);

    Console.WriteLine("Downloading US city boundaries from Census TIGERweb...");

    var features = await DownloadUsCitiesAsync(httpClient);

    using var db = new SpatialDatabase(path);
    var table = db.CreateTable("cities", CoordinateSystem.Wgs84,
        new PropertyDefinition("name", PropertyType.Text),
        new PropertyDefinition("state", PropertyType.Text),
        new PropertyDefinition("population", PropertyType.Integer));

    table.BulkInsert(features);
    Console.WriteLine($"US Cities:           {features.Count} records -> {Path.GetFileName(path)}");
}

static async Task<List<SpatialFeature>> DownloadUsCitiesAsync(HttpClient httpClient)
{
    // Census 2020 TIGERweb - Incorporated Places with population (POP100)
    const string baseUrl = "https://tigerweb.geo.census.gov/arcgis/rest/services/Census2020/Places_CouSub_ConCity_SubMCD/MapServer/4/query";
    const int pageSize = 5000;

    var allFeatures = new List<SpatialFeature>();
    int offset = 0;
    bool hasMore = true;

    while (hasMore)
    {
        var url = $"{baseUrl}?where=POP100>=10000"
            + $"&outFields=BASENAME,STATE,POP100"
            + $"&outSR=4326"
            + $"&geometryPrecision=4"
            + $"&maxAllowableOffset=0.001"
            + $"&resultOffset={offset}"
            + $"&resultRecordCount={pageSize}"
            + $"&f=geojson";

        var json = await httpClient.GetStringAsync(url);
        var parsed = GeoJsonParser.ParseFeatureCollection(json);

        if (parsed.Count == 0)
        {
            hasMore = false;
            continue;
        }

        foreach (var feature in parsed)
        {
            var name = feature.Properties["BASENAME"].GetString() ?? "";
            var stateFips = feature.Properties["STATE"].GetString() ?? "";
            var pop = (long)feature.Properties["POP100"].GetDouble();
            var stateAbbrev = FipsToStateAbbreviation(stateFips);

            allFeatures.Add(new SpatialFeature(feature.Geometry)
            {
                Properties =
                {
                    ["name"] = name,
                    ["state"] = stateAbbrev,
                    ["population"] = pop
                }
            });
        }

        Console.WriteLine($"  Downloaded {allFeatures.Count} US cities so far...");

        if (parsed.Count < pageSize)
            hasMore = false;
        else
            offset += pageSize;
    }

    return allFeatures;
}

static async Task SeedCanadianCitiesAsync(string outputDir, HttpClient httpClient)
{
    var path = Path.Combine(outputDir, "ca-cities.db");
    if (File.Exists(path)) File.Delete(path);

    Console.WriteLine("Downloading Canadian population data...");
    var populationByCsd = await DownloadCanadianPopulationAsync(httpClient);

    Console.WriteLine("Downloading Canadian city boundaries from StatsCan...");
    var features = await DownloadCanadianBoundariesAsync(httpClient, populationByCsd);

    using var db = new SpatialDatabase(path);
    var table = db.CreateTable("cities", CoordinateSystem.Wgs84,
        new PropertyDefinition("name", PropertyType.Text),
        new PropertyDefinition("province", PropertyType.Text),
        new PropertyDefinition("population", PropertyType.Integer));

    table.BulkInsert(features);
    Console.WriteLine($"Canadian Cities:     {features.Count} records -> {Path.GetFileName(path)}");
}

static async Task<Dictionary<string, (string Name, long Population)>> DownloadCanadianPopulationAsync(HttpClient httpClient)
{
    // StatsCan Table 98-10-0002-01: Population by census subdivision
    // CSV is wide format: each row is a geography, with population columns directly
    const string csvUrl = "https://www150.statcan.gc.ca/n1/tbl/csv/98100002-eng.zip";

    var zipBytes = await httpClient.GetByteArrayAsync(csvUrl);
    using var zipStream = new MemoryStream(zipBytes);
    using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

    // Find the main data CSV (not the metadata CSV)
    var csvEntry = archive.Entries
        .Where(e => e.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        .Where(e => !e.Name.Contains("MetaData", StringComparison.OrdinalIgnoreCase))
        .FirstOrDefault()
        ?? throw new InvalidOperationException("No CSV file found in StatsCan ZIP archive");

    using var reader = new StreamReader(csvEntry.Open());
    var headerLine = await reader.ReadLineAsync()
        ?? throw new InvalidOperationException("Empty CSV file");

    // Remove BOM if present
    if (headerLine.Length > 0 && headerLine[0] == '\uFEFF')
        headerLine = headerLine[1..];

    var headers = ParseCsvLine(headerLine);

    // Find column indices
    int geoIdx = FindColumnIndex(headers, "GEO");
    int dguidIdx = FindColumnIndex(headers, "DGUID");
    // Population 2021 is typically column 4 - find it by header text
    int pop2021Idx = FindColumnIndex(headers, "Population, 2021");

    Console.WriteLine($"  CSV columns: GEO={geoIdx}, DGUID={dguidIdx}, Pop2021={pop2021Idx}");

    var populationByCsd = new Dictionary<string, (string Name, long Population)>();

    while (await reader.ReadLineAsync() is { } line)
    {
        if (string.IsNullOrWhiteSpace(line))
            continue;

        var fields = ParseCsvLine(line);
        if (fields.Length <= Math.Max(dguidIdx, pop2021Idx))
            continue;

        var dguid = fields[dguidIdx];

        // Only process Census Subdivision rows (DGUID contains "A0005")
        if (!dguid.Contains("A0005"))
            continue;

        // Extract CSDUID from DGUID (format: {year}A0005{CSDUID})
        int csdStart = dguid.IndexOf("A0005") + 5;
        if (csdStart >= dguid.Length)
            continue;
        var csduid = dguid[csdStart..];

        if (!long.TryParse(fields[pop2021Idx], NumberStyles.Any, CultureInfo.InvariantCulture, out var pop))
            continue;

        var geoName = fields[geoIdx];
        populationByCsd[csduid] = (geoName, pop);
    }

    Console.WriteLine($"  Parsed {populationByCsd.Count} census subdivisions from CSV");
    Console.WriteLine($"  CSDs with pop >= 10,000: {populationByCsd.Count(x => x.Value.Population >= 10000)}");

    return populationByCsd;
}

static async Task<List<SpatialFeature>> DownloadCanadianBoundariesAsync(
    HttpClient httpClient,
    Dictionary<string, (string Name, long Population)> populationByCsd)
{
    // StatsCan 2021 Cartographic Boundary Files - Census Subdivisions (layer 9)
    const string baseUrl = "https://geo.statcan.gc.ca/geo_wa/rest/services/2021/Cartographic_boundary_files/MapServer/9/query";
    const int pageSize = 2000;

    var allFeatures = new List<SpatialFeature>();
    int offset = 0;
    bool hasMore = true;

    while (hasMore)
    {
        var url = $"{baseUrl}?where=1=1"
            + $"&outFields=CSDUID,CSDNAME,PRUID"
            + $"&outSR=4326"
            + $"&geometryPrecision=4"
            + $"&maxAllowableOffset=0.001"
            + $"&resultOffset={offset}"
            + $"&resultRecordCount={pageSize}"
            + $"&f=geojson";

        var json = await httpClient.GetStringAsync(url);
        var parsed = GeoJsonParser.ParseFeatureCollection(json);

        if (parsed.Count == 0)
        {
            hasMore = false;
            continue;
        }

        foreach (var feature in parsed)
        {
            var csduid = feature.Properties["CSDUID"].GetString() ?? "";
            var pruid = feature.Properties["PRUID"].GetString() ?? "";

            if (!populationByCsd.TryGetValue(csduid, out var popData))
                continue;

            if (popData.Population < 10000)
                continue;

            var provinceAbbrev = PruidToProvinceAbbreviation(pruid);
            var name = popData.Name;

            allFeatures.Add(new SpatialFeature(feature.Geometry)
            {
                Properties =
                {
                    ["name"] = name,
                    ["province"] = provinceAbbrev,
                    ["population"] = popData.Population
                }
            });
        }

        Console.WriteLine($"  Processed {offset + parsed.Count} Canadian CSDs, matched {allFeatures.Count} cities...");

        if (parsed.Count < pageSize)
            hasMore = false;
        else
            offset += pageSize;
    }

    return allFeatures;
}

static string[] ParseCsvLine(string line)
{
    var fields = new List<string>();
    bool inQuotes = false;
    var current = new System.Text.StringBuilder();

    for (int i = 0; i < line.Length; i++)
    {
        char c = line[i];
        if (c == '"')
        {
            if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
            {
                current.Append('"');
                i++;
            }
            else
            {
                inQuotes = !inQuotes;
            }
        }
        else if (c == ',' && !inQuotes)
        {
            fields.Add(current.ToString());
            current.Clear();
        }
        else
        {
            current.Append(c);
        }
    }
    fields.Add(current.ToString());
    return fields.ToArray();
}

static int FindColumnIndex(string[] headers, string name)
{
    for (int i = 0; i < headers.Length; i++)
    {
        var header = headers[i].Trim('"', ' ');
        if (header.Equals(name, StringComparison.OrdinalIgnoreCase))
            return i;
    }
    // Try partial match
    for (int i = 0; i < headers.Length; i++)
    {
        var header = headers[i].Trim('"', ' ');
        if (header.Contains(name, StringComparison.OrdinalIgnoreCase))
            return i;
    }
    return -1;
}

static string FipsToStateAbbreviation(string fips) => fips switch
{
    "01" => "AL", "02" => "AK", "04" => "AZ", "05" => "AR", "06" => "CA",
    "08" => "CO", "09" => "CT", "10" => "DE", "11" => "DC", "12" => "FL",
    "13" => "GA", "15" => "HI", "16" => "ID", "17" => "IL", "18" => "IN",
    "19" => "IA", "20" => "KS", "21" => "KY", "22" => "LA", "23" => "ME",
    "24" => "MD", "25" => "MA", "26" => "MI", "27" => "MN", "28" => "MS",
    "29" => "MO", "30" => "MT", "31" => "NE", "32" => "NV", "33" => "NH",
    "34" => "NJ", "35" => "NM", "36" => "NY", "37" => "NC", "38" => "ND",
    "39" => "OH", "40" => "OK", "41" => "OR", "42" => "PA", "44" => "RI",
    "45" => "SC", "46" => "SD", "47" => "TN", "48" => "TX", "49" => "UT",
    "50" => "VT", "51" => "VA", "53" => "WA", "54" => "WV", "55" => "WI",
    "56" => "WY", "60" => "AS", "66" => "GU", "69" => "MP", "72" => "PR",
    "78" => "VI",
    _ => fips
};

static string PruidToProvinceAbbreviation(string pruid) => pruid switch
{
    "10" => "NL", "11" => "PE", "12" => "NS", "13" => "NB",
    "24" => "QC", "35" => "ON", "46" => "MB", "47" => "SK",
    "48" => "AB", "59" => "BC", "60" => "YT", "61" => "NT",
    "62" => "NU",
    _ => pruid
};
