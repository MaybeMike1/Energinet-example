namespace GridFlow.Application.GasFlows;

/// <summary>Read-side persistence port for querying stored gas-flow observations.</summary>
public interface IGasFlowReadRepository
{
    Task<PaginatedResult<GasFlowDto>> GetFlowsAsync(
        DateOnly from,
        DateOnly to,
        GasFlowZone? zone,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<GasFlowSummaryPointDto>> GetSummaryAsync(
        DateOnly from,
        DateOnly to,
        GasFlowZone zone,
        CancellationToken cancellationToken);

    Task<DateTimeOffset?> GetLatestRetrievedAtUtcAsync(CancellationToken cancellationToken);
}