using Shouldly;
using Shiny.Spatial.Geofencing;
using Xunit;

namespace Shiny.Spatial.Geofencing.Tests;

public class SpatialMonitorConfigTests
{
    [Fact]
    public void Defaults_Are_Set()
    {
        var config = new SpatialMonitorConfig();

        config.Entries.ShouldBeEmpty();
        config.MinimumDistance.ShouldNotBeNull();
        config.MinimumTime.ShouldNotBeNull();
        config.MinimumTime!.Value.ShouldBe(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void Add_Single_Entry()
    {
        var config = new SpatialMonitorConfig();
        var result = config.Add("/path/to/db", "regions");

        result.ShouldBeSameAs(config);
        config.Entries.Count.ShouldBe(1);
        config.Entries[0].DatabasePath.ShouldBe("/path/to/db");
        config.Entries[0].TableName.ShouldBe("regions");
    }

    [Fact]
    public void Add_Multiple_Entries_Fluent()
    {
        var config = new SpatialMonitorConfig()
            .Add("/db1", "table1")
            .Add("/db2", "table2")
            .Add("/db3", "table3");

        config.Entries.Count.ShouldBe(3);
        config.Entries[0].DatabasePath.ShouldBe("/db1");
        config.Entries[1].TableName.ShouldBe("table2");
        config.Entries[2].DatabasePath.ShouldBe("/db3");
    }

    [Fact]
    public void MinimumDistance_Can_Be_Set()
    {
        var config = new SpatialMonitorConfig
        {
            MinimumDistance = Distance.FromMeters(500)
        };
        config.MinimumDistance.ShouldNotBeNull();
    }

    [Fact]
    public void MinimumDistance_Can_Be_Null()
    {
        var config = new SpatialMonitorConfig
        {
            MinimumDistance = null
        };
        config.MinimumDistance.ShouldBeNull();
    }

    [Fact]
    public void MinimumTime_Can_Be_Set()
    {
        var config = new SpatialMonitorConfig
        {
            MinimumTime = TimeSpan.FromSeconds(30)
        };
        config.MinimumTime.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void MinimumTime_Can_Be_Null()
    {
        var config = new SpatialMonitorConfig
        {
            MinimumTime = null
        };
        config.MinimumTime.ShouldBeNull();
    }

    [Fact]
    public void Add_Duplicate_Entry_Allowed()
    {
        var config = new SpatialMonitorConfig()
            .Add("/db", "table")
            .Add("/db", "table");

        config.Entries.Count.ShouldBe(2);
    }

    [Fact]
    public void Add_Same_Database_Different_Tables()
    {
        var config = new SpatialMonitorConfig()
            .Add("/db", "cities")
            .Add("/db", "states");

        config.Entries.Count.ShouldBe(2);
        config.Entries[0].TableName.ShouldBe("cities");
        config.Entries[1].TableName.ShouldBe("states");
    }
}
