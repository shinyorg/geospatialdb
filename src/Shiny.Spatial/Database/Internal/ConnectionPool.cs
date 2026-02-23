using System;
using Microsoft.Data.Sqlite;

namespace Shiny.Spatial.Database.Internal;

internal sealed class ConnectionPool : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;
    private bool _disposed;

    public ConnectionPool(string databasePath)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = databasePath == ":memory:"
                ? SqliteOpenMode.Memory
                : SqliteOpenMode.ReadWriteCreate
        }.ToString();
    }

    public SqliteConnection GetConnection()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ConnectionPool));

        if (_connection == null)
        {
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;";
            cmd.ExecuteNonQuery();
        }

        return _connection;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _connection?.Dispose();
        _connection = null;
    }
}
