using GridFlow.Application.GasFlows;
using GridFlow.Domain.GasFlows;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

namespace GridFlow.UnitTests.GasFlows;

public sealed class GasFlowIngestionServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 13, 12, 0, 0, TimeSpan.Zero);

    private static GasFlowRecord Record(DateOnly gasDay) =>
        GasFlowRecord.Create("Gasflow", gasDay, default, Now);

    [Fact]
    public async Task GivenConfiguredWindow_WhenIngesting_ThenFetchesWindowEndingNowMinusWindow()
    {
        var window = TimeSpan.FromDays(3);
        var source = Substitute.For<IGasFlowSource>();
        source.GetGasFlowAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([Record(new DateOnly(2026, 6, 12))]);

        var repository = Substitute.For<IGasFlowRepository>();
        repository.UpsertAsync(Arg.Any<IReadOnlyCollection<GasFlowRecord>>(), Arg.Any<CancellationToken>())
            .Returns(new UpsertResult(1, 0));

        var service = CreateService(source, repository, window);

        await service.IngestAsync(TestContext.Current.CancellationToken);

        await source.Received(1).GetGasFlowAsync(Now - window, Now, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenSourceReturnsRecords_WhenIngesting_ThenUpsertsThemAndReportsCounts()
    {
        var records = new[]
        {
            Record(new DateOnly(2026, 6, 12)),
            Record(new DateOnly(2026, 6, 11)),
            Record(new DateOnly(2026, 6, 10)),
        };

        var source = Substitute.For<IGasFlowSource>();
        source.GetGasFlowAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(records);

        var repository = Substitute.For<IGasFlowRepository>();
        repository.UpsertAsync(Arg.Any<IReadOnlyCollection<GasFlowRecord>>(), Arg.Any<CancellationToken>())
            .Returns(new UpsertResult(2, 1));

        var service = CreateService(source, repository, TimeSpan.FromDays(7));

        var result = await service.IngestAsync(TestContext.Current.CancellationToken);

        result.Fetched.Should().Be(3);
        result.Inserted.Should().Be(2);
        result.Updated.Should().Be(1);
        result.WindowEnd.Should().Be(Now);
        result.WindowStart.Should().Be(Now - TimeSpan.FromDays(7));
        await repository.Received(1).UpsertAsync(
            Arg.Is<IReadOnlyCollection<GasFlowRecord>>(r => r.Count == 3),
            Arg.Any<CancellationToken>());
    }

    private static GasFlowIngestionService CreateService(IGasFlowSource source, IGasFlowRepository repository, TimeSpan window)
    {
        var options = Options.Create(new IngestionOptions { Window = window, Interval = TimeSpan.FromHours(6) });
        return new GasFlowIngestionService(source, repository, new FakeTimeProvider(Now), options);
    }
}