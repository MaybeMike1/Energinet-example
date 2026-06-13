using GridFlow.Application.GasFlows;
using GridFlow.Domain.GasFlows;

using Microsoft.EntityFrameworkCore;

namespace GridFlow.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IGasFlowRepository"/>. Performs a "load existing keys in the
/// window, then insert-or-refresh" upsert keyed on (Dataset, GasDay), which keeps re-ingesting the
/// same window idempotent.
/// </summary>
public sealed class GasFlowRepository(GridFlowDbContext dbContext) : IGasFlowRepository
{
    public async Task<UpsertResult> UpsertAsync(IReadOnlyCollection<GasFlowRecord> records, CancellationToken cancellationToken)
    {
        if (records.Count == 0)
        {
            return new UpsertResult(0, 0);
        }

        var datasets = records.Select(r => r.Dataset).Distinct().ToArray();
        var gasDays = records.Select(r => r.GasDay).Distinct().ToArray();

        var existing = await dbContext.GasFlowRecords
            .Where(r => datasets.Contains(r.Dataset) && gasDays.Contains(r.GasDay))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var byKey = existing.ToDictionary(r => (r.Dataset, r.GasDay));

        var inserted = 0;
        var updated = 0;
        foreach (var record in records)
        {
            var key = (record.Dataset, record.GasDay);
            if (byKey.TryGetValue(key, out var current))
            {
                current.Update(record.ToValues(), record.RetrievedAtUtc);
                updated++;
            }
            else
            {
                dbContext.GasFlowRecords.Add(record);
                // Guard against duplicate keys appearing twice within the same batch.
                byKey[key] = record;
                inserted++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new UpsertResult(inserted, updated);
    }
}