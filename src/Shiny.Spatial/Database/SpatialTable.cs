using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Shiny.Spatial.Algorithms;
using Shiny.Spatial.Database.Internal;
using Shiny.Spatial.Geometry;
using Shiny.Spatial.Serialization;

namespace Shiny.Spatial.Database;

public sealed class SpatialTable
{
    private readonly ConnectionPool _pool;
    private readonly string _tableName;
    private readonly IReadOnlyList<PropertyDefinition> _properties;
    private readonly CoordinateSystem _coordinateSystem;

    internal SpatialTable(
        ConnectionPool pool,
        string tableName,
        CoordinateSystem coordinateSystem,
        IReadOnlyList<PropertyDefinition> properties)
    {
        _pool = pool;
        _tableName = tableName;
        _coordinateSystem = coordinateSystem;
        _properties = properties;
    }

    public string Name => _tableName;
    public CoordinateSystem CoordinateSystem => _coordinateSystem;
    public IReadOnlyList<PropertyDefinition> Properties => _properties;

    public long Insert(SpatialFeature feature)
    {
        var conn = _pool.GetConnection();
        var env = feature.Geometry.GetEnvelope();
        var wkb = WkbWriter.Write(feature.Geometry);

        var sql = SqlBuilder.BuildInsert(_tableName, _properties);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("$min_x", env.MinX);
        cmd.Parameters.AddWithValue("$max_x", env.MaxX);
        cmd.Parameters.AddWithValue("$min_y", env.MinY);
        cmd.Parameters.AddWithValue("$max_y", env.MaxY);
        cmd.Parameters.AddWithValue("$geometry", wkb);

        AddPropertyParameters(cmd, feature);
        cmd.ExecuteNonQuery();

        using var idCmd = conn.CreateCommand();
        idCmd.CommandText = "SELECT last_insert_rowid()";
        feature.Id = (long)idCmd.ExecuteScalar()!;
        return feature.Id;
    }

    public void BulkInsert(IEnumerable<SpatialFeature> features)
    {
        var conn = _pool.GetConnection();
        using var transaction = conn.BeginTransaction();

        try
        {
            var sql = SqlBuilder.BuildInsert(_tableName, _properties);
            foreach (var feature in features)
            {
                var env = feature.Geometry.GetEnvelope();
                var wkb = WkbWriter.Write(feature.Geometry);

                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("$min_x", env.MinX);
                cmd.Parameters.AddWithValue("$max_x", env.MaxX);
                cmd.Parameters.AddWithValue("$min_y", env.MinY);
                cmd.Parameters.AddWithValue("$max_y", env.MaxY);
                cmd.Parameters.AddWithValue("$geometry", wkb);
                AddPropertyParameters(cmd, feature);
                cmd.ExecuteNonQuery();

                using var idCmd = conn.CreateCommand();
                idCmd.CommandText = "SELECT last_insert_rowid()";
                feature.Id = (long)idCmd.ExecuteScalar()!;
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void Update(SpatialFeature feature)
    {
        var conn = _pool.GetConnection();
        var env = feature.Geometry.GetEnvelope();
        var wkb = WkbWriter.Write(feature.Geometry);

        var sql = SqlBuilder.BuildUpdate(_tableName, _properties);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("$id", feature.Id);
        cmd.Parameters.AddWithValue("$min_x", env.MinX);
        cmd.Parameters.AddWithValue("$max_x", env.MaxX);
        cmd.Parameters.AddWithValue("$min_y", env.MinY);
        cmd.Parameters.AddWithValue("$max_y", env.MaxY);
        cmd.Parameters.AddWithValue("$geometry", wkb);
        AddPropertyParameters(cmd, feature);
        cmd.ExecuteNonQuery();
    }

    public bool Delete(long id)
    {
        var conn = _pool.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = SqlBuilder.BuildDelete(_tableName);
        cmd.Parameters.AddWithValue("$id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public SpatialFeature? GetById(long id)
    {
        var conn = _pool.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = SqlBuilder.BuildSelectById(_tableName);
        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return ReadFeature(reader);
    }

    public long Count()
    {
        var conn = _pool.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = SqlBuilder.BuildCount(_tableName);
        return (long)cmd.ExecuteScalar()!;
    }

    public List<SpatialFeature> FindInEnvelope(Envelope envelope)
    {
        var conn = _pool.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = SqlBuilder.BuildSelect(_tableName, envelope);
        cmd.Parameters.AddWithValue("$min_x", envelope.MinX);
        cmd.Parameters.AddWithValue("$max_x", envelope.MaxX);
        cmd.Parameters.AddWithValue("$min_y", envelope.MinY);
        cmd.Parameters.AddWithValue("$max_y", envelope.MaxY);

        return ReadFeatures(cmd);
    }

    public List<SpatialFeature> FindIntersecting(Geometry.Geometry geometry)
    {
        // Pass 1: R*Tree bounding box filter
        var candidates = FindInEnvelope(geometry.GetEnvelope());

        // Pass 2: exact geometry refinement
        var results = new List<SpatialFeature>();
        for (int i = 0; i < candidates.Count; i++)
        {
            if (SpatialPredicates.Intersects(candidates[i].Geometry, geometry))
                results.Add(candidates[i]);
        }
        return results;
    }

    public List<SpatialFeature> FindContainedBy(Geometry.Geometry container)
    {
        var candidates = FindInEnvelope(container.GetEnvelope());

        var results = new List<SpatialFeature>();
        for (int i = 0; i < candidates.Count; i++)
        {
            if (SpatialPredicates.Contains(container, candidates[i].Geometry))
                results.Add(candidates[i]);
        }
        return results;
    }

    public List<SpatialFeature> FindWithinDistance(Coordinate center, double distanceMeters)
    {
        var pointEnv = new Envelope(center.X, center.X, center.Y, center.Y);
        var searchEnv = EnvelopeExpander.ExpandByDistance(pointEnv, distanceMeters, _coordinateSystem);

        // Pass 1: R*Tree bounding box filter
        var candidates = FindInEnvelope(searchEnv);

        // Pass 2: exact distance check
        var results = new List<SpatialFeature>();
        for (int i = 0; i < candidates.Count; i++)
        {
            var featureCenter = candidates[i].Geometry.GetEnvelope().Center;
            double dist = _coordinateSystem == CoordinateSystem.Wgs84
                ? DistanceCalculator.Haversine(center, featureCenter)
                : DistanceCalculator.Euclidean(center, featureCenter);

            if (dist <= distanceMeters)
                results.Add(candidates[i]);
        }
        return results;
    }

    public SpatialQuery Query() => new(this);

    internal List<SpatialFeature> ExecuteQuery(
        Envelope? bbox,
        List<PropertyFilter>? propertyFilters,
        Func<SpatialFeature, bool>? geometryFilter,
        Coordinate? orderByDistanceFrom,
        int? limit,
        int offset)
    {
        var conn = _pool.GetConnection();
        using var cmd = conn.CreateCommand();

        var sql = BuildQuerySql(bbox, propertyFilters, out var paramIndex);
        cmd.CommandText = sql;

        if (bbox != null)
        {
            cmd.Parameters.AddWithValue("$min_x", bbox.Value.MinX);
            cmd.Parameters.AddWithValue("$max_x", bbox.Value.MaxX);
            cmd.Parameters.AddWithValue("$min_y", bbox.Value.MinY);
            cmd.Parameters.AddWithValue("$max_y", bbox.Value.MaxY);
        }

        if (propertyFilters != null)
        {
            for (int i = 0; i < propertyFilters.Count; i++)
            {
                cmd.Parameters.AddWithValue($"$pf{i}", propertyFilters[i].Value);
            }
        }

        var features = ReadFeatures(cmd);

        // Pass 2: geometry refinement
        if (geometryFilter != null)
        {
            var filtered = new List<SpatialFeature>();
            for (int i = 0; i < features.Count; i++)
            {
                if (geometryFilter(features[i]))
                    filtered.Add(features[i]);
            }
            features = filtered;
        }

        // Order by distance
        if (orderByDistanceFrom != null)
        {
            var center = orderByDistanceFrom.Value;
            features.Sort((a, b) =>
            {
                double da = ComputeDistance(center, a.Geometry.GetEnvelope().Center);
                double db = ComputeDistance(center, b.Geometry.GetEnvelope().Center);
                return da.CompareTo(db);
            });
        }

        // Paging
        if (offset > 0)
        {
            if (offset >= features.Count)
                return new List<SpatialFeature>();
            features = features.GetRange(offset, features.Count - offset);
        }

        if (limit != null && limit.Value < features.Count)
            features = features.GetRange(0, limit.Value);

        return features;
    }

    private string BuildQuerySql(Envelope? bbox, List<PropertyFilter>? propertyFilters, out int paramIndex)
    {
        paramIndex = 0;
        var sql = $"SELECT id, min_x, max_x, min_y, max_y, geometry, * FROM [{_tableName}_rtree]";
        var conditions = new List<string>();

        if (bbox != null)
        {
            conditions.Add("min_x <= $max_x AND max_x >= $min_x AND min_y <= $max_y AND max_y >= $min_y");
        }

        if (propertyFilters != null)
        {
            for (int i = 0; i < propertyFilters.Count; i++)
            {
                conditions.Add($"[prop_{propertyFilters[i].PropertyName}] {propertyFilters[i].Operator} $pf{i}");
                paramIndex++;
            }
        }

        if (conditions.Count > 0)
            sql += " WHERE " + string.Join(" AND ", conditions);

        return sql;
    }

    private double ComputeDistance(Coordinate from, Coordinate to) =>
        _coordinateSystem == CoordinateSystem.Wgs84
            ? DistanceCalculator.Haversine(from, to)
            : DistanceCalculator.Euclidean(from, to);

    private List<SpatialFeature> ReadFeatures(SqliteCommand cmd)
    {
        var results = new List<SpatialFeature>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            results.Add(ReadFeature(reader));
        return results;
    }

    private SpatialFeature ReadFeature(SqliteDataReader reader)
    {
        long id = reader.GetInt64(0);
        var wkb = (byte[])reader["geometry"];
        var geom = WkbReader.Read(wkb);

        var props = new Dictionary<string, object?>();
        for (int i = 0; i < _properties.Count; i++)
        {
            var colName = $"prop_{_properties[i].Name}";
            var ordinal = reader.GetOrdinal(colName);
            props[_properties[i].Name] = reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal);
        }

        return new SpatialFeature(geom, props) { Id = id };
    }

    private void AddPropertyParameters(SqliteCommand cmd, SpatialFeature feature)
    {
        for (int i = 0; i < _properties.Count; i++)
        {
            var propName = _properties[i].Name;
            object? value = feature.Properties.TryGetValue(propName, out var v) ? v : null;
            cmd.Parameters.AddWithValue($"$prop_{propName}", value ?? DBNull.Value);
        }
    }
}
