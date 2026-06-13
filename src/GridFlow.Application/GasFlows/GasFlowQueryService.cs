using Microsoft.Extensions.Options;

namespace GridFlow.Application.GasFlows;

/// <summary>BFF use case for reading stored gas-flow data with validated query parameters.</summary>
public sealed class GasFlowQueryService
{
    private readonly IGasFlowReadRepository _repository;
    private readonly ApiOptions _options;

    public GasFlowQueryService(IGasFlowReadRepository repository, IOptions<ApiOptions> options)
    {
        _repository = repository;
        _options = options.Value;
    }

    public async Task<PaginatedResult<GasFlowDto>> GetFlowsAsync(
        DateOnly? from,
        DateOnly? to,
        GasFlowZone? zone,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var (fromDay, toDay) = ResolveDateRange(from, to);
        var resolvedPage = page is null or < 1 ? 1 : page.Value;
        var resolvedPageSize = ResolvePageSize(pageSize);

        return await _repository.GetFlowsAsync(fromDay, toDay, zone, resolvedPage, resolvedPageSize, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<GasFlowSummaryPointDto>> GetSummaryAsync(
        DateOnly? from,
        DateOnly? to,
        GasFlowZone zone,
        CancellationToken cancellationToken)
    {
        var (fromDay, toDay) = ResolveDateRange(from, to);
        return await _repository.GetSummaryAsync(fromDay, toDay, zone, cancellationToken).ConfigureAwait(false);
    }

    public Task<DateTimeOffset?> GetLatestRetrievedAtUtcAsync(CancellationToken cancellationToken) =>
        _repository.GetLatestRetrievedAtUtcAsync(cancellationToken);

    public int MaxPageSize => _options.MaxPageSize;

    private int ResolvePageSize(int? pageSize)
    {
        if (pageSize is null or < 1)
        {
            return _options.DefaultPageSize;
        }

        return Math.Min(pageSize.Value, _options.MaxPageSize);
    }

    private static (DateOnly From, DateOnly To) ResolveDateRange(DateOnly? from, DateOnly? to)
    {
        if (from is null || to is null)
        {
            throw new ArgumentException("Both 'from' and 'to' date parameters are required.");
        }

        if (from > to)
        {
            throw new ArgumentException("'from' must be on or before 'to'.");
        }

        return (from.Value, to.Value);
    }
}