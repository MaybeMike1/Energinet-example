using GridFlow.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Testcontainers.MsSql;

namespace GridFlow.IntegrationTests.Persistence;

/// <summary>
/// Starts a real SQL Server in a container once per test class and applies the EF Core migrations
/// to it. Tests share the database, so each one uses a distinct natural key to stay independent.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public GridFlowDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GridFlowDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .Options;

        return new GridFlowDbContext(options);
    }
}