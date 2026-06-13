using System.Threading;

using GridFlow.Application.GasFlows;

namespace GridFlow.Worker.Ingestion;

/// <summary>
/// Thread-safe, in-memory record of the worker's last ingestion outcome. Registered as a singleton
/// and read by the ingestion health check.
/// </summary>
public sealed class IngestionHealthTracker
{
    private readonly Lock _gate = new();
    private DateTimeOffset? _lastSuccessUtc;
    private string? _lastError;
    private IngestionResult? _lastResult;

    public void RecordSuccess(IngestionResult result, DateTimeOffset timestampUtc)
    {
        lock (_gate)
        {
            _lastSuccessUtc = timestampUtc;
            _lastResult = result;
            _lastError = null;
        }
    }

    public void RecordFailure(string error)
    {
        lock (_gate)
        {
            _lastError = error;
        }
    }

    public IngestionStatus Snapshot()
    {
        lock (_gate)
        {
            return new IngestionStatus(_lastSuccessUtc, _lastError, _lastResult);
        }
    }
}

/// <summary>An immutable snapshot of ingestion health.</summary>
public sealed record IngestionStatus(DateTimeOffset? LastSuccessUtc, string? LastError, IngestionResult? LastResult);