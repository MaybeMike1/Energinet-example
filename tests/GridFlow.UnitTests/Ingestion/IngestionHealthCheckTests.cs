using GridFlow.Application.GasFlows;
using GridFlow.Worker.Ingestion;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace GridFlow.UnitTests.Ingestion;

public sealed class IngestionHealthCheckTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 13, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private static IngestionHealthCheck CreateCheck(IngestionHealthTracker tracker, DateTimeOffset checkTime) =>
        new(
            tracker,
            new FakeTimeProvider(checkTime),
            Options.Create(new IngestionOptions { Interval = Interval, Window = TimeSpan.FromDays(7) }));

    private static IngestionResult SampleResult() => new(Now.AddDays(-7), Now, 5, 2, 3);

    [Fact]
    public async Task GivenNoRunYet_WhenChecking_ThenDegraded()
    {
        var check = CreateCheck(new IngestionHealthTracker(), Now);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public async Task GivenRecentSuccess_WhenChecking_ThenHealthyWithSummaryData()
    {
        var tracker = new IngestionHealthTracker();
        tracker.RecordSuccess(SampleResult(), Now);
        var check = CreateCheck(tracker, Now.AddMinutes(30));

        var result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("fetched");
    }

    [Fact]
    public async Task GivenSuccessOlderThanTwoIntervals_WhenChecking_ThenUnhealthy()
    {
        var tracker = new IngestionHealthTracker();
        tracker.RecordSuccess(SampleResult(), Now);
        var check = CreateCheck(tracker, Now.AddHours(3));

        var result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }
}