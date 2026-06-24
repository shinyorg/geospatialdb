namespace Shiny.Spatial.Geofencing;

public record SpatialMonitorEntry(string DatabasePath, string TableName);

public class SpatialMonitorConfig
{
    public List<SpatialMonitorEntry> Entries { get; } = new();
    public Distance? MinimumDistance { get; set; } = Distance.FromMeters(300);
    public TimeSpan? MinimumTime { get; set; } = TimeSpan.FromMinutes(1);

    public Distance? MaximumDistance { get; set; }
    public TimeSpan? MaximumTime { get; set; }
    
    
    public SpatialMonitorConfig Add(string databasePath, string tableName)
    {
        Entries.Add(new SpatialMonitorEntry(databasePath, tableName));
        return this;
    }
}
