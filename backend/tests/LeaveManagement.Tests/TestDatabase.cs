using LeaveManagement.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace LeaveManagement.Tests;

// The tests run against a real, throwaway PostgreSQL started by Testcontainers,
// which needs Docker on the machine.
//
// No Docker? Set the environment variable Database__Provider=Sqlite and the very
// same tests run against an in-memory SQLite instead:
//
//     Database__Provider=Sqlite dotnet test          (bash)
//     $env:Database__Provider="Sqlite"; dotnet test  (PowerShell)
public sealed class TestDatabase : IAsyncLifetime
{
    private readonly List<SqliteConnection> _sqliteConnections = new();
    private PostgreSqlContainer? _postgres;

    public static bool UseSqlite => string.Equals(
        Environment.GetEnvironmentVariable("Database__Provider"),
        "Sqlite",
        StringComparison.OrdinalIgnoreCase);

    public async Task InitializeAsync()
    {
        if (UseSqlite) return;

        _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
        await _postgres.StartAsync();
    }

    // Gives each test its own empty database.
    public LeaveDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<LeaveDbContext>();

        if (UseSqlite)
        {
            // The in-memory database lives as long as its connection, so keep it open.
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            _sqliteConnections.Add(connection);
            options.UseSqlite(connection);
        }
        else
        {
            // A uniquely named database per test: the container's default database
            // cannot be dropped while connected to it, and unique names keep tests isolated.
            var connectionString = new NpgsqlConnectionStringBuilder(_postgres!.GetConnectionString())
            {
                Database = $"test_{Guid.NewGuid():N}"
            }.ConnectionString;
            options.UseNpgsql(connectionString);
        }

        var db = new LeaveDbContext(options.Options);
        db.Database.EnsureCreated();
        return db;
    }

    public async Task DisposeAsync()
    {
        foreach (var connection in _sqliteConnections) connection.Dispose();
        if (_postgres is not null) await _postgres.DisposeAsync();
    }
}
