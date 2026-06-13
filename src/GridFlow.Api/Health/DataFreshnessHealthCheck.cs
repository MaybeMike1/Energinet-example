using GridFlow.Application.GasFlows;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace GridFlow.Api.Health;

/// <summary>Readiness check: stored data must be fresh enough for the dashboard to be useful.</summary>
public sealed class DataFreshnessHealthCheck(
    GasFlowQueryService queryService,
    TimeProvider timeProvider,
    IOptions<ApiOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var latest = await queryService.GetLatestRetrievedAtUtcAsync(cancellationToken).ConfigureAwait(false);
        if (latest is null)
        {
            return HealthCheckResult.Degraded("No gas-flow data has been ingested yet.");
        }

        var age = timeProvider.GetUtcNow() - latest.Value;
        if (age > options.Value.MaxDataAge)
        {
            return HealthCheckResult.Unhealthy(
                $"Newest stored record is {age.TotalHours:F1} hours old, exceeding the {options.Value.MaxDataAge.TotalHours:F0} hour budget.");
        }

        return HealthCheckResult.Healthy(
            "Stored gas-flow data is fresh.",
            new Dictionary<string, object> { ["latestRetrievedAtUtc"] = latest.Value });
    }
}