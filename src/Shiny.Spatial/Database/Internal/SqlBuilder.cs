using System.Collections.Generic;
using System.Text;
using Shiny.Spatial.Geometry;

namespace Shiny.Spatial.Database.Internal;

internal static class SqlBuilder
{
    public static string BuildInsert(string tableName, IReadOnlyList<PropertyDefinition> properties)
    {
        var sb = new StringBuilder();
        sb.Append($"INSERT INTO [{tableName}_rtree] (min_x, max_x, min_y, max_y, geometry");

        for (int i = 0; i < properties.Count; i++)
            sb.Append($", [prop_{properties[i].Name}]");

        sb.Append(") VALUES ($min_x, $max_x, $min_y, $max_y, $geometry");

        for (int i = 0; i < properties.Count; i++)
            sb.Append($", $prop_{properties[i].Name}");

        sb.Append(")");
        return sb.ToString();
    }

    public static string BuildUpdate(string tableName, IReadOnlyList<PropertyDefinition> properties)
    {
        var sb = new StringBuilder();
        sb.Append($"UPDATE [{tableName}_rtree] SET min_x = $min_x, max_x = $max_x, min_y = $min_y, max_y = $max_y, geometry = $geometry");

        for (int i = 0; i < properties.Count; i++)
            sb.Append($", [prop_{properties[i].Name}] = $prop_{properties[i].Name}");

        sb.Append(" WHERE id = $id");
        return sb.ToString();
    }

    public static string BuildSelect(string tableName, Envelope? bbox = null)
    {
        var sb = new StringBuilder();
        sb.Append($"SELECT id, min_x, max_x, min_y, max_y, geometry, * FROM [{tableName}_rtree]");

        if (bbox != null)
        {
            sb.Append(" WHERE min_x <= $max_x AND max_x >= $min_x AND min_y <= $max_y AND max_y >= $min_y");
        }

        return sb.ToString();
    }

    public static string BuildDelete(string tableName) =>
        $"DELETE FROM [{tableName}_rtree] WHERE id = $id";

    public static string BuildCount(string tableName) =>
        $"SELECT COUNT(*) FROM [{tableName}_rtree]";

    public static string BuildSelectById(string tableName) =>
        $"SELECT id, min_x, max_x, min_y, max_y, geometry, * FROM [{tableName}_rtree] WHERE id = $id";
}
