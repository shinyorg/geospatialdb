using Shiny.Spatial.Database;

namespace Shiny.Spatial.Geofencing;

public record SpatialRegionChange(
    string TableName,
    SpatialFeature Region,
    bool Entered
);
