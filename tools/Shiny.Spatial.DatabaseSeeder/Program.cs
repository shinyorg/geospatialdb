using Shiny.Spatial.Database;
using Shiny.Spatial.DatabaseSeeder.Data;
using Shiny.Spatial.Geometry;

string outputDir = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "databases");

Directory.CreateDirectory(outputDir);

Console.WriteLine($"Output directory: {Path.GetFullPath(outputDir)}");
Console.WriteLine();

SeedUsStates(outputDir);
SeedUsCities(outputDir);
SeedCanadianProvinces(outputDir);
SeedCanadianCities(outputDir);

Console.WriteLine();
Console.WriteLine("All databases seeded successfully.");

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

static void SeedUsCities(string outputDir)
{
    var path = Path.Combine(outputDir, "us-cities.db");
    if (File.Exists(path)) File.Delete(path);

    using var db = new SpatialDatabase(path);
    var table = db.CreateTable("cities", CoordinateSystem.Wgs84,
        new PropertyDefinition("name", PropertyType.Text),
        new PropertyDefinition("state", PropertyType.Text),
        new PropertyDefinition("population", PropertyType.Integer));

    var cities = UsCities.GetAll();
    var features = new List<SpatialFeature>();

    foreach (var city in cities)
    {
        features.Add(new SpatialFeature(new Point(city.Longitude, city.Latitude))
        {
            Properties =
            {
                ["name"] = city.Name,
                ["state"] = city.StateAbbreviation,
                ["population"] = city.Population
            }
        });
    }

    table.BulkInsert(features);
    Console.WriteLine($"US Cities:           {features.Count} records -> {Path.GetFileName(path)}");
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

static void SeedCanadianCities(string outputDir)
{
    var path = Path.Combine(outputDir, "ca-cities.db");
    if (File.Exists(path)) File.Delete(path);

    using var db = new SpatialDatabase(path);
    var table = db.CreateTable("cities", CoordinateSystem.Wgs84,
        new PropertyDefinition("name", PropertyType.Text),
        new PropertyDefinition("province", PropertyType.Text),
        new PropertyDefinition("population", PropertyType.Integer));

    var cities = CanadianCities.GetAll();
    var features = new List<SpatialFeature>();

    foreach (var city in cities)
    {
        features.Add(new SpatialFeature(new Point(city.Longitude, city.Latitude))
        {
            Properties =
            {
                ["name"] = city.Name,
                ["province"] = city.ProvinceAbbreviation,
                ["population"] = city.Population
            }
        });
    }

    table.BulkInsert(features);
    Console.WriteLine($"Canadian Cities:     {features.Count} records -> {Path.GetFileName(path)}");
}
