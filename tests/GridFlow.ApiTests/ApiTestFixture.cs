using GridFlow.Domain.GasFlows;
using GridFlow.Infrastructure;
using GridFlow.Infrastructure.Persistence;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.MsSql;

namespace GridFlow.ApiTests;

/// <summary>
/// Starts SQL Server in a container, boots the API via <see cref="WebApplicationFactory{TEntryPoint}"/>,
/// applies migrations, and seeds gas-flow rows for endpoint tests.
/// </summary>
public sealed class ApiTestFixture : IAsyncLifetime
{
    private static readonly GasFlowValues SampleValues = new(
        KWhFromBiogas: 1_000,
        KWhToDenmark: -2_000,
        KWhFromNorthSea: 3_000,
        KWhToOrFromStorage: 4_000,
        KWhToOrFromGermany: 5_000,
        KWhToSweden: -6_000,
        KWhFromTyra: 7_000,
        KWhToPoland: -8_000);

    private readonly MsSqlContainer _container =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public HttpClient Client { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:GridFlow", _container.GetConnectionString());
                builder.UseEnvironment("Testing");
            });

        await Factory.Services.ApplyMigrationsAsync();
        await SeedAsync();

        Client = Factory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
        await _container.DisposeAsync();
    }

    private async Task SeedAsync()
    {
        var retrievedAt = DateTimeOffset.UtcNow;
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GridFlowDbContext>();

        if (await db.GasFlowRecords.AnyAsync())
        {
            return;
        }

        db.GasFlowRecords.AddRange(
            GasFlowRecord.Create("Gasflow", new DateOnly(2026, 6, 10), SampleValues, retrievedAt),
            GasFlowRecord.Create("Gasflow", new DateOnly(2026, 6, 11), SampleValues, retrievedAt),
            GasFlowRecord.Create("Gasflow", new DateOnly(2026, 6, 12), SampleValues, retrievedAt));

        await db.SaveChangesAsync();
    }
}