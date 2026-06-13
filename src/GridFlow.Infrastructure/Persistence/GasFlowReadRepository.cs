using GridFlow.Application.GasFlows;
using GridFlow.Domain.GasFlows;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GridFlow.Infrastructure.Persistence;

public sealed class GasFlowReadRepository(GridFlowDbContext dbContext, IOptions<ApiOptions> options) : IGasFlowReadRepository
{
    private readonly string _dataset = options.Value.DefaultDataset;

    public async Task<PaginatedResult<GasFlowDto>> GetFlowsAsync(
        DateOnly from,
        DateOnly to,
        GasFlowZone? zone,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.GasFlowRecords
            .AsNoTracking()
            .Where(r => r.Dataset == _dataset && r.GasDay >= from && r.GasDay <= to);

        if (zone is { } selectedZone)
        {
            query = selectedZone switch
            {
                GasFlowZone.FromBiogas => query.Where(r => r.KWhFromBiogas != 0),
                GasFlowZone.ToDenmark => query.Where(r => r.KWhToDenmark != 0),
                GasFlowZone.FromNorthSea => query.Where(r => r.KWhFromNorthSea != 0),
                GasFlowZone.ToOrFromStorage => query.Where(r => r.KWhToOrFromStorage != 0),
                GasFlowZone.ToOrFromGermany => query.Where(r => r.KWhToOrFromGermany != 0),
                GasFlowZone.ToSweden => query.Where(r => r.KWhToSweden != 0),
                GasFlowZone.FromTyra => query.Where(r => r.KWhFromTyra != 0),
                GasFlowZone.ToPoland => query.Where(r => r.KWhToPoland != 0),
                _ => query,
            };
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var records = await query
            .OrderByDescending(r => r.GasDay)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = records.Select(Map).ToList();

        return new PaginatedResult<GasFlowDto>(items, page, pageSize, totalCount);
    }

    public async Task<IReadOnlyList<GasFlowSummaryPointDto>> GetSummaryAsync(
        DateOnly from,
        DateOnly to,
        GasFlowZone zone,
        CancellationToken cancellationToken)
    {
        var records = await dbContext.GasFlowRecords
            .AsNoTracking()
            .Where(r => r.Dataset == _dataset && r.GasDay >= from && r.GasDay <= to)
            .OrderBy(r => r.GasDay)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records
            .Select(r => new GasFlowSummaryPointDto(r.GasDay, GetZoneValue(zone, r)))
            .ToList();
    }

    public async Task<DateTimeOffset?> GetLatestRetrievedAtUtcAsync(CancellationToken cancellationToken)
    {
        return await dbContext.GasFlowRecords
            .AsNoTracking()
            .Where(r => r.Dataset == _dataset)
            .MaxAsync(r => (DateTimeOffset?)r.RetrievedAtUtc, cancellationToken)
            .ConfigureAwait(false);
    }

    private static GasFlowDto Map(GasFlowRecord record) => new(
        record.GasDay,
        record.KWhFromBiogas,
        record.KWhToDenmark,
        record.KWhFromNorthSea,
        record.KWhToOrFromStorage,
        record.KWhToOrFromGermany,
        record.KWhToSweden,
        record.KWhFromTyra,
        record.KWhToPoland,
        record.RetrievedAtUtc);

    private static long GetZoneValue(GasFlowZone zone, GasFlowRecord record) => zone switch
    {
        GasFlowZone.FromBiogas => record.KWhFromBiogas,
        GasFlowZone.ToDenmark => record.KWhToDenmark,
        GasFlowZone.FromNorthSea => record.KWhFromNorthSea,
        GasFlowZone.ToOrFromStorage => record.KWhToOrFromStorage,
        GasFlowZone.ToOrFromGermany => record.KWhToOrFromGermany,
        GasFlowZone.ToSweden => record.KWhToSweden,
        GasFlowZone.FromTyra => record.KWhFromTyra,
        GasFlowZone.ToPoland => record.KWhToPoland,
        _ => throw new ArgumentOutOfRangeException(nameof(zone), zone, null),
    };
}