using GridFlow.Application.GasFlows;
using GridFlow.Domain.GasFlows;
using GridFlow.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace GridFlow.IntegrationTests.Persistence;

public sealed class GasFlowRepositoryTests : IClassFixture<SqlServerFixture>
{
    // Distinct dataset keeps these rows isolated from other tests sharing the database.
    private const string Dataset = "GasflowUpsertTest";

    private static readonly GasFlowValues OriginalValues = new(
        KWhFromBiogas: 1_000,
        KWhToDenmark: -2_000,
        KWhFromNorthSea: 3_000,
        KWhToOrFromStorage: 4_000,
        KWhToOrFromGermany: 5_000,
        KWhToSweden: -6_000,
        KWhFromTyra: 7_000,
        KWhToPoland: -8_000);

    private static readonly GasFlowValues RevisedValues = OriginalValues with { KWhFromBiogas = 9_999 };

    private readonly SqlServerFixture _fixture;

    public GasFlowRepositoryTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    private static IReadOnlyList<GasFlowRecord> BuildWindow(GasFlowValues values, DateTimeOffset retrievedAt) =>
    [
        GasFlowRecord.Create(Dataset, new DateOnly(2026, 7, 1), values, retrievedAt),
        GasFlowRecord.Create(Dataset, new DateOnly(2026, 7, 2), values, retrievedAt),
        GasFlowRecord.Create(Dataset, new DateOnly(2026, 7, 3), values, retrievedAt),
    ];

    [Fact]
    public async Task GivenSameWindowIngestedTwice_WhenUpserting_ThenNoDuplicateRows()
    {
        var ct = TestContext.Current.CancellationToken;
        var firstRetrieval = new DateTimeOffset(2026, 7, 4, 6, 0, 0, TimeSpan.Zero);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new GasFlowRepository(context);
            var first = await repository.UpsertAsync(BuildWindow(OriginalValues, firstRetrieval), ct);

            first.Inserted.Should().Be(3);
            first.Updated.Should().Be(0);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new GasFlowRepository(context);
            var second = await repository.UpsertAsync(BuildWindow(OriginalValues, firstRetrieval), ct);

            second.Inserted.Should().Be(0);
            second.Updated.Should().Be(3);
        }

        await using var verify = _fixture.CreateContext();
        var rowCount = await verify.GasFlowRecords.CountAsync(r => r.Dataset == Dataset, ct);
        rowCount.Should().Be(3);
    }

    [Fact]
    public async Task GivenChangedValues_WhenReUpserting_ThenExistingRowIsRefreshedInPlace()
    {
        var ct = TestContext.Current.CancellationToken;
        var gasDay = new DateOnly(2026, 8, 15);
        const string isolatedDataset = "GasflowRefreshTest";
        var firstRetrieval = new DateTimeOffset(2026, 8, 16, 6, 0, 0, TimeSpan.Zero);
        var secondRetrieval = new DateTimeOffset(2026, 8, 17, 6, 0, 0, TimeSpan.Zero);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new GasFlowRepository(context);
            await repository.UpsertAsync(
                [GasFlowRecord.Create(isolatedDataset, gasDay, OriginalValues, firstRetrieval)],
                ct);
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository = new GasFlowRepository(context);
            var outcome = await repository.UpsertAsync(
                [GasFlowRecord.Create(isolatedDataset, gasDay, RevisedValues, secondRetrieval)],
                ct);

            outcome.Inserted.Should().Be(0);
            outcome.Updated.Should().Be(1);
        }

        await using var verify = _fixture.CreateContext();
        var reloaded = await verify.GasFlowRecords.SingleAsync(r => r.Dataset == isolatedDataset && r.GasDay == gasDay, ct);
        reloaded.ToValues().Should().Be(RevisedValues);
        reloaded.RetrievedAtUtc.Should().Be(secondRetrieval);
    }
}