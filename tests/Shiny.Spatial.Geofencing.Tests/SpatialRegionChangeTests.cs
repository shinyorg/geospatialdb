using Shouldly;
using Shiny.Spatial.Database;
using Shiny.Spatial.Geofencing;
using Shiny.Spatial.Geometry;
using Xunit;

namespace Shiny.Spatial.Geofencing.Tests;

public class SpatialRegionChangeTests
{
    [Fact]
    public void Record_Properties_Set_Correctly()
    {
        var prev = new SpatialFeature(new Point(1, 2)) { Properties = { ["name"] = "Region A" } };
        var curr = new SpatialFeature(new Point(3, 4)) { Properties = { ["name"] = "Region B" } };

        var change = new SpatialRegionChange("cities", prev, curr);

        change.TableName.ShouldBe("cities");
        change.PreviousRegion.ShouldBeSameAs(prev);
        change.CurrentRegion.ShouldBeSameAs(curr);
    }

    [Fact]
    public void Enter_Region_Has_Null_Previous()
    {
        var curr = new SpatialFeature(new Point(1, 1));
        var change = new SpatialRegionChange("regions", null, curr);

        change.PreviousRegion.ShouldBeNull();
        change.CurrentRegion.ShouldNotBeNull();
    }

    [Fact]
    public void Exit_Region_Has_Null_Current()
    {
        var prev = new SpatialFeature(new Point(1, 1));
        var change = new SpatialRegionChange("regions", prev, null);

        change.PreviousRegion.ShouldNotBeNull();
        change.CurrentRegion.ShouldBeNull();
    }

    [Fact]
    public void Both_Null_Represents_No_Region()
    {
        var change = new SpatialRegionChange("regions", null, null);

        change.PreviousRegion.ShouldBeNull();
        change.CurrentRegion.ShouldBeNull();
    }

    [Fact]
    public void Record_Equality()
    {
        var feature = new SpatialFeature(new Point(1, 1));
        var a = new SpatialRegionChange("t", feature, null);
        var b = new SpatialRegionChange("t", feature, null);

        a.ShouldBe(b);
    }

    [Fact]
    public void Record_Inequality_Different_Table()
    {
        var feature = new SpatialFeature(new Point(1, 1));
        var a = new SpatialRegionChange("t1", feature, null);
        var b = new SpatialRegionChange("t2", feature, null);

        a.ShouldNotBe(b);
    }

    [Fact]
    public void Deconstruction()
    {
        var prev = new SpatialFeature(new Point(0, 0));
        var curr = new SpatialFeature(new Point(1, 1));
        var change = new SpatialRegionChange("test", prev, curr);

        var (tableName, previousRegion, currentRegion) = change;

        tableName.ShouldBe("test");
        previousRegion.ShouldBeSameAs(prev);
        currentRegion.ShouldBeSameAs(curr);
    }
}
