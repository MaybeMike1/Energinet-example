using GridFlow.Application.GasFlows;

using Microsoft.Extensions.Options;

namespace GridFlow.Worker.Ingestion;

/// <summary>
/// Runs <see cref="GasFlowIngestionService"/> on a fixed cadence. Fires once at startup, then on a
/// <see cref="PeriodicTimer"/> whose interval comes from config. Each cycle is isolated in try/catch
/// so a single bad tick never takes the service down, and its outcome feeds the health tracker.
/// </summary>
public sealed class IngestionWorker(
    IServiceScopeFactory scopeFactory,
    IngestionHealthTracker healthTracker,
    TimeProvider timeProvider,
    IOptions<IngestionOptions> options,
    ILogger<IngestionWorker> logger) : BackgroundService
{
    private readonly IngestionOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Ingestion worker started. Interval={Interval}, Window={Window}.",
            _options.Interval,
            _options.Window);

        using var timer = new PeriodicTimer(_options.Interval, timeProvider);
        do
        {
            await RunCycleAsync(stoppingToken).ConfigureAwait(false);
        }
        while (await WaitForNextTickAsync(timer, stoppingToken).ConfigureAwait(false));
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetTimestamp();
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var ingestion = scope.ServiceProvider.GetRequiredService<GasFlowIngestionService>();

            var result = await ingestion.IngestAsync(cancellationToken).ConfigureAwait(false);
            var elapsed = timeProvider.GetElapsedTime(startedAt);
            healthTracker.RecordSuccess(result, timeProvider.GetUtcNow());

            logger.LogInformation(
                "Ingestion completed for window {WindowStart:o}..{WindowEnd:o}: fetched={Fetched}, inserted={Inserted}, updated={Updated} in {ElapsedMs:F0} ms.",
                result.WindowStart,
                result.WindowEnd,
                result.Fetched,
                result.Inserted,
                result.Updated,
                elapsed.TotalMilliseconds);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Graceful shutdown — not a failure.
        }
        catch (Exception ex)
        {
            healthTracker.RecordFailure(ex.Message);
            logger.LogError(ex, "Ingestion cycle failed; the worker will continue on the next tick.");
        }
    }

    private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}