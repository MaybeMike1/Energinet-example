using GridFlow.Domain.GasFlows;

namespace GridFlow.Application.GasFlows;

/// <summary>
/// Persistence port for gas-flow records. Implemented in Infrastructure over EF Core. The upsert is
/// idempotent on the natural key (<see cref="GasFlowRecord.Dataset"/>, <see cref="GasFlowRecord.GasDay"/>),
/// so re-ingesting the same window never duplicates rows.
/// </summary>
public interface IGasFlowRepository
{
    Task<UpsertResult> UpsertAsync(IReadOnlyCollection<GasFlowRecord> records, CancellationToken cancellationToken);
}