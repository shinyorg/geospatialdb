# Shiny.Spatial

A dependency-free, cross-platform .NET geospatial database library. Uses SQLite R\*Tree for spatial indexing with custom C# geometry algorithms for query refinement. No SpatiaLite, no NetTopologySuite — only SQLite via `Microsoft.Data.Sqlite`.

**AOT compatible and trimmable.**

## Features

- Two-pass spatial query pipeline (R\*Tree bounding box filter + C# geometry refinement)
- WGS84 (Haversine) and Cartesian (Euclidean) coordinate systems
- Full geometry type support: Point, LineString, Polygon (with holes), Multi\*, GeometryCollection
- WKB (Well-Known Binary) serialization
- Fluent query builder with property filtering, distance ordering, and paging
- Bulk insert with transaction support
- Works on iOS, Android, and all .NET platforms

## Installation

```xml
<PackageReference Include="Shiny.Spatial" Version="1.0.0" />
```

### Target Frameworks

| Framework | Notes |
|---|---|
| `netstandard2.0` | Broad compatibility (Xamarin, .NET Framework, etc.) |
| `net10.0` | Modern .NET with AOT support |

### Dependencies

- `Microsoft.Data.Sqlite` — brings `SQLitePCLRaw.bundle_e_sqlite3` with R\*Tree enabled
- `System.Memory` — netstandard2.0 only (Span polyfill)

## Quick Start

### Create a Database and Table

```csharp
using Shiny.Spatial.Database;
using Shiny.Spatial.Geometry;

using var db = new SpatialDatabase("mydata.db");  // or ":memory:"

var table = db.CreateTable("cities", CoordinateSystem.Wgs84,
    new PropertyDefinition("name", PropertyType.Text),
    new PropertyDefinition("population", PropertyType.Integer));
```

### Insert Features

```csharp
table.Insert(new SpatialFeature(new Point(-104.99, 39.74))
{
    Properties = { ["name"] = "Denver", ["population"] = 715000L }
});

table.Insert(new SpatialFeature(new Point(-104.82, 38.83))
{
    Properties = { ["name"] = "Colorado Springs", ["population"] = 478000L }
});
```

### Bulk Insert

```csharp
var features = new List<SpatialFeature>();
for (int i = 0; i < 100_000; i++)
    features.Add(new SpatialFeature(new Point(lon, lat)));

table.BulkInsert(features); // wrapped in a transaction
```

### Distance Query

```csharp
// Find all cities within 150 km of Denver
var nearby = table.FindWithinDistance(
    new Coordinate(-104.99, 39.74),
    distanceMeters: 150_000
);
```

### Shape Containment Query

```csharp
var colorado = new Polygon(new[]
{
    new Coordinate(-109.05, 37.0), new Coordinate(-102.05, 37.0),
    new Coordinate(-102.05, 41.0), new Coordinate(-109.05, 41.0),
    new Coordinate(-109.05, 37.0)
});

var inState = table.FindIntersecting(colorado);
```

### Fluent Query Builder

```csharp
var center = new Coordinate(-104.99, 39.74);

var results = table.Query()
    .WithinDistance(center, 150_000)
    .WhereProperty("population", ">", 200000L)
    .OrderByDistance(center)
    .Limit(10)
    .ToList();

// Other terminal operations
int count = table.Query().InEnvelope(envelope).Count();
var first = table.Query().WithinDistance(center, 1000).FirstOrDefault();
```

## Architecture

### Two-Pass Query Pipeline

1. **R\*Tree bounding box filter** (SQL, O(log n)) — eliminates most candidates using the SQLite R\*Tree index
2. **C# geometry refinement** — exact Contains/Intersects/WithinDistance checks on survivors

### SQLite Schema

Each spatial table creates a single R\*Tree virtual table with auxiliary columns:

```sql
CREATE VIRTUAL TABLE {name}_rtree USING rtree(
    id, min_x, max_x, min_y, max_y,
    +geometry BLOB,              -- WKB-encoded geometry
    +prop_{name} {type}, ...     -- user-defined property columns
);
```

Metadata is tracked in `__spatial_meta` and `__spatial_columns` tables.

## API Reference

### Geometry Types

All geometry classes are immutable and sealed, extending the abstract `Geometry` base class.

| Type | Description |
|---|---|
| `Coordinate` | Readonly struct with `X`/`Y` (aliased as `Longitude`/`Latitude`) |
| `Envelope` | Readonly struct — bounding box with `MinX`, `MaxX`, `MinY`, `MaxY` |
| `Point` | Single coordinate |
| `LineString` | Ordered sequence of coordinates (minimum 2) |
| `Polygon` | Exterior ring + optional interior rings (holes) |
| `MultiPoint` | Collection of Points |
| `MultiLineString` | Collection of LineStrings |
| `MultiPolygon` | Collection of Polygons |
| `GeometryCollection` | Collection of mixed Geometry types |

### Serialization

```csharp
using Shiny.Spatial.Serialization;

byte[] wkb = WkbWriter.Write(geometry);
Geometry restored = WkbReader.Read(wkb);
```

Full roundtrip support for all geometry types using the WKB (Well-Known Binary) format.

### Algorithms

```csharp
using Shiny.Spatial.Algorithms;
```

| Class | Method | Description |
|---|---|---|
| `DistanceCalculator` | `Haversine(a, b)` | Great-circle distance in meters (WGS84) |
| `DistanceCalculator` | `Euclidean(a, b)` | Cartesian distance |
| `DistanceCalculator` | `DistanceToSegment(p, a, b)` | Perpendicular distance from point to segment |
| `PointInPolygon` | `Contains(polygon, point)` | Ray-casting with hole support |
| `SegmentIntersection` | `Intersects(a1, a2, b1, b2)` | Cross-product segment intersection test |
| `SpatialPredicates` | `Intersects(a, b)` | Dispatch for all geometry type combinations |
| `SpatialPredicates` | `Contains(container, contained)` | Dispatch for all geometry type combinations |
| `EnvelopeExpander` | `ExpandByDistance(env, meters, cs)` | Expand envelope by distance (WGS84 or Cartesian) |

### Database

#### `SpatialDatabase` (IDisposable)

```csharp
var db = new SpatialDatabase("path.db");    // file-backed
var db = new SpatialDatabase(":memory:");   // in-memory

SpatialTable table = db.CreateTable(name, coordinateSystem, properties...);
SpatialTable table = db.GetTable(name);
bool exists       = db.TableExists(name);
db.DropTable(name);
db.Dispose();
```

Validates R\*Tree support on startup via `PRAGMA compile_options`.

#### `SpatialTable`

| Method | Description |
|---|---|
| `Insert(feature)` | Insert a feature, returns its ID |
| `BulkInsert(features)` | Insert many features in a single transaction |
| `Update(feature)` | Update a feature by ID |
| `Delete(id)` | Delete a feature by ID |
| `GetById(id)` | Retrieve a single feature |
| `Count()` | Total feature count |
| `FindInEnvelope(envelope)` | R\*Tree bounding box query |
| `FindIntersecting(geometry)` | Two-pass intersection query |
| `FindContainedBy(geometry)` | Two-pass containment query |
| `FindWithinDistance(center, meters)` | Two-pass distance query |
| `Query()` | Returns a fluent `SpatialQuery` builder |

#### `SpatialQuery` (Fluent Builder)

| Method | Type | Description |
|---|---|---|
| `InEnvelope(envelope)` | Filter | Bounding box filter |
| `Intersecting(geometry)` | Filter | Geometry intersection |
| `ContainedBy(geometry)` | Filter | Geometry containment |
| `WithinDistance(center, meters)` | Filter | Distance radius |
| `WhereProperty(name, op, value)` | Filter | Property comparison (`=`, `!=`, `<`, `<=`, `>`, `>=`, `LIKE`) |
| `OrderByDistance(center)` | Sort | Order by distance from coordinate |
| `Limit(count)` | Paging | Limit result count |
| `Offset(count)` | Paging | Skip first N results |
| `ToList()` | Terminal | Execute and return results |
| `Count()` | Terminal | Execute and return count |
| `FirstOrDefault()` | Terminal | Execute and return first or null |

#### `SpatialFeature`

```csharp
var feature = new SpatialFeature(new Point(-104.99, 39.74))
{
    Properties = { ["name"] = "Denver", ["population"] = 715000L }
};

long id = feature.Id;              // set after Insert
Geometry geom = feature.Geometry;
Dictionary<string, object?> props = feature.Properties;
```

#### `PropertyDefinition`

```csharp
new PropertyDefinition("name", PropertyType.Text)
new PropertyDefinition("population", PropertyType.Integer)
new PropertyDefinition("area", PropertyType.Real)
new PropertyDefinition("data", PropertyType.Blob)
```

## Pre-Built Databases

The `databases/` folder contains ready-to-use spatial databases seeded with real geographic data.

### Database Catalog

| Database | Table | Geometry | Records | Properties |
|---|---|---|---|---|
| `us-states.db` | `states` | Polygon | 51 (50 states + DC) | `name`, `abbreviation`, `population` |
| `us-cities.db` | `cities` | Point | 100 (top 100 by pop.) | `name`, `state`, `population` |
| `ca-provinces.db` | `provinces` | Polygon | 13 (all provinces/territories) | `name`, `abbreviation`, `population` |
| `ca-cities.db` | `cities` | Point | 50 (top 50 by pop.) | `name`, `province`, `population` |

All databases use the `CoordinateSystem.Wgs84` coordinate system with WGS84 (longitude/latitude) coordinates.

### Using a Pre-Built Database

```csharp
using var db = new SpatialDatabase("databases/us-states.db");
var states = db.GetTable("states");

// Find which state Denver is in
var denver = new Point(-104.99, 39.74);
var results = states.FindIntersecting(denver);
// results[0].Properties["name"] == "Colorado"

// Find all states within 500km of Chicago
var nearby = states.FindWithinDistance(new Coordinate(-87.6298, 41.8781), 500_000);
```

```csharp
using var db = new SpatialDatabase("databases/us-cities.db");
var cities = db.GetTable("cities");

// Find cities near San Francisco with population over 500K
var sf = new Coordinate(-122.4194, 37.7749);
var results = cities.Query()
    .WithinDistance(sf, 100_000)
    .WhereProperty("population", ">", 500000L)
    .OrderByDistance(sf)
    .ToList();
```

```csharp
using var db = new SpatialDatabase("databases/ca-provinces.db");
var provinces = db.GetTable("provinces");

// Find which province Toronto is in
var toronto = new Point(-79.3832, 43.6532);
var results = provinces.FindIntersecting(toronto);
// results[0].Properties["name"] == "Ontario"
```

### Regenerating Databases

The databases are generated by the `Shiny.Spatial.DatabaseSeeder` tool. To regenerate:

```bash
dotnet run --project tools/Shiny.Spatial.DatabaseSeeder -- ./databases
```

The seeder contains hardcoded geographic data with simplified polygon boundaries for states/provinces and point locations for cities, using census population figures.

## Project Structure

```
geospatialdb/
├── Shiny.Spatial.slnx
├── databases/                  Pre-built spatial databases
│   ├── us-states.db
│   ├── us-cities.db
│   ├── ca-provinces.db
│   └── ca-cities.db
├── src/Shiny.Spatial/
│   ├── Shiny.Spatial.csproj
│   ├── Geometry/               Coordinate, Envelope, Point, LineString, Polygon,
│   │                           MultiPoint, MultiLineString, MultiPolygon, GeometryCollection
│   ├── Serialization/          WkbReader, WkbWriter
│   ├── Algorithms/             DistanceCalculator, PointInPolygon, SegmentIntersection,
│   │                           SpatialPredicates, EnvelopeExpander
│   └── Database/               SpatialDatabase, SpatialTable, SpatialFeature, SpatialQuery
│       └── Internal/           ConnectionPool, SchemaManager, SqlBuilder
├── tests/Shiny.Spatial.Tests/
│   ├── GeometryTests.cs
│   ├── WkbTests.cs
│   ├── AlgorithmTests.cs
│   ├── DatabaseTests.cs
│   ├── QueryTests.cs
│   └── PerformanceTests.cs
├── tests/Shiny.Spatial.Benchmarks/
│   ├── InsertBenchmarks.cs
│   ├── SpatialQueryBenchmarks.cs
│   ├── QueryBuilderBenchmarks.cs
│   ├── AlgorithmBenchmarks.cs
│   └── SerializationBenchmarks.cs
└── tools/Shiny.Spatial.DatabaseSeeder/
    ├── Shiny.Spatial.DatabaseSeeder.csproj
    ├── Program.cs
    └── Data/                   Hardcoded geographic seed data
        ├── UsStates.cs             51 US state/territory polygons
        ├── UsCities.cs             100 US city points
        ├── CanadianProvinces.cs    13 Canadian province/territory polygons
        └── CanadianCities.cs       50 Canadian city points
```

## Benchmarks

Measured with [BenchmarkDotNet](https://benchmarkdotnet.org/) on Apple M2, .NET 10.0.3, Arm64 RyuJIT AdvSIMD. All database benchmarks use in-memory SQLite (`:memory:`) to isolate CPU/algorithm cost from disk I/O.

```bash
# Run all benchmarks
dotnet run --project tests/Shiny.Spatial.Benchmarks -c Release

# Run a specific suite
dotnet run --project tests/Shiny.Spatial.Benchmarks -c Release -- --filter "*Algorithm*"
```

### Insert Performance

| Method | Count | Mean | Allocated |
|---|---|---|---|
| SingleInsert | 1,000 | 14.27 ms | 4.32 MB |
| BulkInsert | 1,000 | 9.81 ms | 3.15 MB |
| SingleInsert | 10,000 | 120.51 ms | 43.12 MB |
| BulkInsert | 10,000 | 93.11 ms | 31.37 MB |
| SingleInsert | 100,000 | 1,204.32 ms | 431.07 MB |
| BulkInsert | 100,000 | 963.87 ms | 313.59 MB |

### Spatial Queries (100K points)

| Method | Mean | Allocated |
|---|---|---|
| FindInEnvelope_Small | 1,815.44 us | 693.47 KB |
| FindInEnvelope_Large | 60,631.83 us | 17,767.42 KB |
| FindIntersecting_Polygon | 1,153.05 us | 447.58 KB |
| FindWithinDistance | 183.22 us | 85.04 KB |
| FindContainedBy | 986.73 us | 447.58 KB |
| GetById | 9.40 us | 3.48 KB |

### Fluent Query Builder (100K points)

| Method | Mean | Allocated |
|---|---|---|
| SpatialOnly | 2,019.6 us | 851.75 KB |
| SpatialPlusPropertyFilter | 1,442.1 us | 426.27 KB |
| DistanceWithOrderAndLimit | 253.7 us | 84.95 KB |
| PropertyFilterOnly | 87,064.9 us | 7,230.53 KB |

### Algorithms (pure computation, no DB)

| Method | Mean | Allocated |
|---|---|---|
| Haversine | 28.33 ns | - |
| Euclidean | 0.15 ns | - |
| PointInPolygon_Simple (5 vertices) | 23.63 ns | - |
| PointInPolygon_Complex (100 vertices) | 252.58 ns | - |
| SegmentIntersection | 2.72 ns | - |
| SpatialPredicates_PointInPolygon | 25.70 ns | 32 B |
| SpatialPredicates_PolygonIntersectsPolygon | 41.86 ns | - |

### WKB Serialization

| Method | Mean | Allocated |
|---|---|---|
| WritePoint | 57.87 ns | 432 B |
| ReadPoint | 49.51 ns | 248 B |
| WritePolygon (5 vertices) | 115.96 ns | 504 B |
| ReadPolygon (5 vertices) | 135.63 ns | 672 B |
| WriteComplexPolygon (100 vertices) | 1,373.27 ns | 5,696 B |
| ReadComplexPolygon (100 vertices) | 2,207.57 ns | 8,352 B |
| WriteLineString (50 coords) | 705.30 ns | 2,808 B |
| ReadLineString (50 coords) | 827.24 ns | 4,232 B |

## Running Tests

```bash
dotnet test
```

53 tests covering geometry types, WKB roundtrips, spatial algorithms, database CRUD, query pipeline, and a 100K point performance benchmark.

## License

MIT
