using System;
using System.Collections.Generic;
using Shiny.Spatial.Database.Internal;

namespace Shiny.Spatial.Database;

public sealed class SpatialDatabase : IDisposable
{
    private readonly ConnectionPool _pool;
    private bool _disposed;

    public SpatialDatabase(string databasePath)
    {
        _pool = new ConnectionPool(databasePath);
        var conn = _pool.GetConnection();

        if (!SchemaManager.CheckRTreeSupport(conn))
            throw new InvalidOperationException("SQLite was not compiled with R*Tree support (ENABLE_RTREE). Cannot use spatial features.");

        SchemaManager.EnsureMetaTables(conn);
    }

    public SpatialTable CreateTable(string tableName, CoordinateSystem coordinateSystem, params PropertyDefinition[] properties)
    {
        ThrowIfDisposed();
        var conn = _pool.GetConnection();

        if (SchemaManager.TableExists(conn, tableName))
            throw new InvalidOperationException($"Spatial table '{tableName}' already exists.");

        SchemaManager.CreateSpatialTable(conn, tableName, coordinateSystem, properties);
        return new SpatialTable(_pool, tableName, coordinateSystem, properties);
    }

    public SpatialTable GetTable(string tableName)
    {
        ThrowIfDisposed();
        var conn = _pool.GetConnection();

        if (!SchemaManager.TableExists(conn, tableName))
            throw new KeyNotFoundException($"Spatial table '{tableName}' does not exist.");

        var coordSystem = SchemaManager.GetCoordinateSystem(conn, tableName);
        var properties = SchemaManager.GetProperties(conn, tableName);
        return new SpatialTable(_pool, tableName, coordSystem, properties);
    }

    public bool TableExists(string tableName)
    {
        ThrowIfDisposed();
        return SchemaManager.TableExists(_pool.GetConnection(), tableName);
    }

    public void DropTable(string tableName)
    {
        ThrowIfDisposed();
        SchemaManager.DropSpatialTable(_pool.GetConnection(), tableName);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pool.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SpatialDatabase));
    }
}
