using GridFlow.Application.GasFlows;

namespace GridFlow.Web.Services;

public interface IGridFlowApiClient
{
    Task<PaginatedResult<GasFlowDto>> GetFlowsAsync(
        DateOnly from,
        DateOnly to,
        string? zone,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<GasFlowSummaryResponse> GetSummaryAsync(
        DateOnly from,
        DateOnly to,
        string zone,
        CancellationToken cancellationToken);
}