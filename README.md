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

## Project Structure

```
geospatialdb/
├── Shiny.Spatial.sln
├── src/Shiny.Spatial/
│   ├── Shiny.Spatial.csproj
│   ├── Geometry/           Coordinate, Envelope, Point, LineString, Polygon,
│   │                       MultiPoint, MultiLineString, MultiPolygon, GeometryCollection
│   ├── Serialization/      WkbReader, WkbWriter
│   ├── Algorithms/         DistanceCalculator, PointInPolygon, SegmentIntersection,
│   │                       SpatialPredicates, EnvelopeExpander
│   └── Database/           SpatialDatabase, SpatialTable, SpatialFeature, SpatialQuery
│       └── Internal/       ConnectionPool, SchemaManager, SqlBuilder
└── tests/Shiny.Spatial.Tests/
    ├── GeometryTests.cs
    ├── WkbTests.cs
    ├── AlgorithmTests.cs
    ├── DatabaseTests.cs
    ├── QueryTests.cs
    └── PerformanceTests.cs
```

## Running Tests

```bash
dotnet test
```

53 tests covering geometry types, WKB roundtrips, spatial algorithms, database CRUD, query pipeline, and a 100K point performance benchmark.

## License

MIT
