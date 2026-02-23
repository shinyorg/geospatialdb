using System.Collections.Generic;
using System.Text;
using Microsoft.Data.Sqlite;

namespace Shiny.Spatial.Database.Internal;

internal static class SchemaManager
{
    public static void EnsureMetaTables(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS __spatial_meta (
                table_name TEXT PRIMARY KEY,
                coordinate_system TEXT NOT NULL,
                created_at TEXT NOT NULL DEFAULT (datetime('now'))
            );

            CREATE TABLE IF NOT EXISTS __spatial_columns (
                table_name TEXT NOT NULL,
                column_name TEXT NOT NULL,
                column_type TEXT NOT NULL,
                PRIMARY KEY (table_name, column_name)
            );
        ";
        cmd.ExecuteNonQuery();
    }

    public static void CreateSpatialTable(
        SqliteConnection conn,
        string tableName,
        CoordinateSystem coordSystem,
        IReadOnlyList<PropertyDefinition> properties)
    {
        var sb = new StringBuilder();
        sb.Append($"CREATE VIRTUAL TABLE [{tableName}_rtree] USING rtree(\n");
        sb.Append("    id, min_x, max_x, min_y, max_y,\n");
        sb.Append("    +geometry BLOB");

        for (int i = 0; i < properties.Count; i++)
        {
            sb.Append($",\n    +[prop_{properties[i].Name}] {properties[i].SqlType}");
        }

        sb.Append("\n);");

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sb.ToString();
        cmd.ExecuteNonQuery();

        // Register in meta tables
        using var metaCmd = conn.CreateCommand();
        metaCmd.CommandText = "INSERT INTO __spatial_meta (table_name, coordinate_system) VALUES ($name, $cs)";
        metaCmd.Parameters.AddWithValue("$name", tableName);
        metaCmd.Parameters.AddWithValue("$cs", coordSystem.ToString());
        metaCmd.ExecuteNonQuery();

        for (int i = 0; i < properties.Count; i++)
        {
            using var colCmd = conn.CreateCommand();
            colCmd.CommandText = "INSERT INTO __spatial_columns (table_name, column_name, column_type) VALUES ($table, $col, $type)";
            colCmd.Parameters.AddWithValue("$table", tableName);
            colCmd.Parameters.AddWithValue("$col", properties[i].Name);
            colCmd.Parameters.AddWithValue("$type", properties[i].Type.ToString());
            colCmd.ExecuteNonQuery();
        }
    }

    public static void DropSpatialTable(SqliteConnection conn, string tableName)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"DROP TABLE IF EXISTS [{tableName}_rtree]";
        cmd.ExecuteNonQuery();

        using var metaCmd = conn.CreateCommand();
        metaCmd.CommandText = "DELETE FROM __spatial_meta WHERE table_name = $name";
        metaCmd.Parameters.AddWithValue("$name", tableName);
        metaCmd.ExecuteNonQuery();

        using var colCmd = conn.CreateCommand();
        colCmd.CommandText = "DELETE FROM __spatial_columns WHERE table_name = $name";
        colCmd.Parameters.AddWithValue("$name", tableName);
        colCmd.ExecuteNonQuery();
    }

    public static bool TableExists(SqliteConnection conn, string tableName)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM __spatial_meta WHERE table_name = $name";
        cmd.Parameters.AddWithValue("$name", tableName);
        return (long)cmd.ExecuteScalar()! > 0;
    }

    public static CoordinateSystem GetCoordinateSystem(SqliteConnection conn, string tableName)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT coordinate_system FROM __spatial_meta WHERE table_name = $name";
        cmd.Parameters.AddWithValue("$name", tableName);
        var result = (string)cmd.ExecuteScalar()!;
        return result == "Wgs84" ? CoordinateSystem.Wgs84 : CoordinateSystem.Cartesian;
    }

    public static List<PropertyDefinition> GetProperties(SqliteConnection conn, string tableName)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT column_name, column_type FROM __spatial_columns WHERE table_name = $name ORDER BY rowid";
        cmd.Parameters.AddWithValue("$name", tableName);

        var props = new List<PropertyDefinition>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var name = reader.GetString(0);
            var type = ParsePropertyType(reader.GetString(1));
            props.Add(new PropertyDefinition(name, type));
        }
        return props;
    }

    public static bool CheckRTreeSupport(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA compile_options";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (reader.GetString(0) == "ENABLE_RTREE")
                return true;
        }
        return false;
    }

    private static PropertyType ParsePropertyType(string value) => value switch
    {
        nameof(PropertyType.Text) => PropertyType.Text,
        nameof(PropertyType.Integer) => PropertyType.Integer,
        nameof(PropertyType.Real) => PropertyType.Real,
        nameof(PropertyType.Blob) => PropertyType.Blob,
        _ => throw new System.ArgumentException($"Unknown property type: {value}")
    };
}
