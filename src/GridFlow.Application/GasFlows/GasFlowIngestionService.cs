using Microsoft.Extensions.Options;

namespace GridFlow.Application.GasFlows;

/// <summary>
/// Orchestrates one ingestion cycle: compute a dynamic time window from <see cref="TimeProvider"/>,
/// pull records from the source, and upsert them idempotently. Pure orchestration — no I/O details
/// leak in here, which keeps it fully unit-testable with fakes.
/// </summary>
public sealed class GasFlowIngestionService
{
    private readonly IGasFlowSource _source;
    private readonly IGasFlowRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly IngestionOptions _options;

    public GasFlowIngestionService(
        IGasFlowSource source,
        IGasFlowRepository repository,
        TimeProvider timeProvider,
        IOptions<IngestionOptions> options)
    {
        _source = source;
        _repository = repository;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public async Task<IngestionResult> IngestAsync(CancellationToken cancellationToken)
    {
        var end = _timeProvider.GetUtcNow();
        var start = end - _options.Window;

        var records = await _source.GetGasFlowAsync(start, end, cancellationToken).ConfigureAwait(false);
        var upsert = await _repository.UpsertAsync(records, cancellationToken).ConfigureAwait(false);

        return new IngestionResult(start, end, records.Count, upsert.Inserted, upsert.Updated);
    }
}