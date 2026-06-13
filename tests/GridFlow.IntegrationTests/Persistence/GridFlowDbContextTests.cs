using GridFlow.Domain.GasFlows;

using Microsoft.EntityFrameworkCore;

namespace GridFlow.IntegrationTests.Persistence;

public sealed class GridFlowDbContextTests : IClassFixture<SqlServerFixture>
{
    private static readonly GasFlowValues SampleValues = new(
        KWhFromBiogas: 25_312_656,
        KWhToDenmark: -34_839_889,
        KWhFromNorthSea: 207_112_725,
        KWhToOrFromStorage: -5_368_000,
        KWhToOrFromGermany: 106_488,
        KWhToSweden: -11_824_683,
        KWhFromTyra: 40_050_073,
        KWhToPoland: -241_865_040);

    private readonly SqlServerFixture _fixture;

    public GridFlowDbContextTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GivenMigratedDatabase_WhenQueryingAppliedMigrations_ThenIncludesInitialCreate()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _fixture.CreateContext();

        var applied = await db.Database.GetAppliedMigrationsAsync(ct);

        applied.Should().Contain(name => name.EndsWith("InitialCreate", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GivenSavedRecord_WhenReloading_ThenValuesRoundTrip()
    {
        var ct = TestContext.Current.CancellationToken;
        var gasDay = new DateOnly(2026, 6, 12);
        var retrievedAt = new DateTimeOffset(2026, 6, 13, 8, 30, 0, TimeSpan.Zero);

        await using (var write = _fixture.CreateContext())
        {
            write.GasFlowRecords.Add(GasFlowRecord.Create("Gasflow", gasDay, SampleValues, retrievedAt));
            await write.SaveChangesAsync(ct);
        }

        await using var read = _fixture.CreateContext();
        var loaded = await read.GasFlowRecords.SingleAsync(x => x.Dataset == "Gasflow" && x.GasDay == gasDay, ct);

        loaded.ToValues().Should().Be(SampleValues);
        loaded.RetrievedAtUtc.Should().Be(retrievedAt);
    }

    [Fact]
    public async Task GivenExistingNaturalKey_WhenSavingDuplicate_ThenThrowsDbUpdateException()
    {
        var ct = TestContext.Current.CancellationToken;
        var gasDay = new DateOnly(2026, 6, 20);

        await using (var first = _fixture.CreateContext())
        {
            first.GasFlowRecords.Add(GasFlowRecord.Create("Gasflow", gasDay, SampleValues, DateTimeOffset.UtcNow));
            await first.SaveChangesAsync(ct);
        }

        await using var second = _fixture.CreateContext();
        second.GasFlowRecords.Add(GasFlowRecord.Create("Gasflow", gasDay, SampleValues, DateTimeOffset.UtcNow));

        var act = async () => await second.SaveChangesAsync(ct);

        await act.Should().ThrowAsync<DbUpdateException>();
    }
}