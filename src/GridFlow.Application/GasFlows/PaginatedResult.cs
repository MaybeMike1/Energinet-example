namespace GridFlow.Application.GasFlows;

/// <summary>Paginated list envelope returned by the BFF flows endpoint.</summary>
public sealed record PaginatedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}