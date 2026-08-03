using System.Data.Common;

using Infrastructure.DataAccess;

using Microsoft.EntityFrameworkCore;

namespace Compass.Tests.Fixtures;

public class MysqlDatabaseFixture
{
    private const string ConnectionStringEnvironmentVariable = "SPRINTLABS_MYSQL_TEST_CONNECTION";
    private static readonly object Lock = new();
    private static bool _databaseInitialized;
    private readonly string _connectionString;

    public MysqlDatabaseFixture()
    {
        _connectionString = GetSafeConnectionString();
        lock (Lock)
        {
            if (_databaseInitialized)
            {
                return;
            }

            using var context = CreateContext();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            _databaseInitialized = true;
        }
    }

    public ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySql(_connectionString, ServerVersion.AutoDetect(_connectionString))
            .Options);

    private static string GetSafeConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Set {ConnectionStringEnvironmentVariable} to run destructive MySQL integration tests.");
        }

        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        var database = builder.TryGetValue("Database", out var databaseValue) ? databaseValue?.ToString() : null;
        if (string.IsNullOrWhiteSpace(database) ||
            (!database.StartsWith("sprintlabs-test", StringComparison.OrdinalIgnoreCase) &&
             !database.StartsWith("compass-test", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("MySQL integration tests require a dedicated database whose name begins with sprintlabs-test or compass-test.");
        }

        return connectionString;
    }
}