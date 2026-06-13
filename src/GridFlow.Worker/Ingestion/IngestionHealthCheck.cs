using GridFlow.Application.GasFlows;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace GridFlow.Worker.Ingestion;

/// <summary>
/// Reports ingestion freshness: degraded until the first successful run, unhealthy once the last
/// success is older than two configured intervals (one missed cycle tolerated), otherwise healthy.
/// </summary>
public sealed class IngestionHealthCheck(
    IngestionHealthTracker tracker,
    TimeProvider timeProvider,
    IOptions<IngestionOptions> options) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var status = tracker.Snapshot();

        if (status.LastSuccessUtc is null)
        {
            var message = status.LastError is null
                ? "Ingestion has not completed a run yet."
                : $"Ingestion has not completed a successful run yet. Last error: {status.LastError}";
            return Task.FromResult(HealthCheckResult.Degraded(message));
        }

        var staleness = timeProvider.GetUtcNow() - status.LastSuccessUtc.Value;
        var allowedStaleness = options.Value.Interval * 2;
        if (staleness > allowedStaleness)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Last successful ingestion was {staleness.TotalMinutes:F0} min ago, exceeding the {allowedStaleness.TotalMinutes:F0} min budget."));
        }

        var data = new Dictionary<string, object>
        {
            ["lastSuccessUtc"] = status.LastSuccessUtc.Value,
            ["lastError"] = status.LastError ?? "none",
        };

        if (status.LastResult is { } result)
        {
            data["fetched"] = result.Fetched;
            data["inserted"] = result.Inserted;
            data["updated"] = result.Updated;
        }

        return Task.FromResult(HealthCheckResult.Healthy("Ingestion is fresh.", data));
    }
}