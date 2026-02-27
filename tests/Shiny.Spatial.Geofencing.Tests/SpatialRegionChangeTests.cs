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
        var feature = new SpatialFeature(new Point(1, 2)) { Properties = { ["name"] = "Region A" } };

        var change = new SpatialRegionChange("cities", feature, true);

        change.TableName.ShouldBe("cities");
        change.Region.ShouldBeSameAs(feature);
        change.Entered.ShouldBeTrue();
    }

    [Fact]
    public void Enter_Region_Has_Entered_True()
    {
        var feature = new SpatialFeature(new Point(1, 1));
        var change = new SpatialRegionChange("regions", feature, true);

        change.Entered.ShouldBeTrue();
        change.Region.ShouldNotBeNull();
    }

    [Fact]
    public void Exit_Region_Has_Entered_False()
    {
        var feature = new SpatialFeature(new Point(1, 1));
        var change = new SpatialRegionChange("regions", feature, false);

        change.Entered.ShouldBeFalse();
        change.Region.ShouldNotBeNull();
    }

    [Fact]
    public void Record_Equality()
    {
        var feature = new SpatialFeature(new Point(1, 1));
        var a = new SpatialRegionChange("t", feature, true);
        var b = new SpatialRegionChange("t", feature, true);

        a.ShouldBe(b);
    }

    [Fact]
    public void Record_Inequality_Different_Table()
    {
        var feature = new SpatialFeature(new Point(1, 1));
        var a = new SpatialRegionChange("t1", feature, true);
        var b = new SpatialRegionChange("t2", feature, true);

        a.ShouldNotBe(b);
    }

    [Fact]
    public void Record_Inequality_Different_Entered()
    {
        var feature = new SpatialFeature(new Point(1, 1));
        var a = new SpatialRegionChange("t", feature, true);
        var b = new SpatialRegionChange("t", feature, false);

        a.ShouldNotBe(b);
    }

    [Fact]
    public void Deconstruction()
    {
        var feature = new SpatialFeature(new Point(0, 0));
        var change = new SpatialRegionChange("test", feature, true);

        var (tableName, region, entered) = change;

        tableName.ShouldBe("test");
        region.ShouldBeSameAs(feature);
        entered.ShouldBeTrue();
    }
}
