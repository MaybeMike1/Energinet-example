using GridFlow.Domain.GasFlows;

using Microsoft.EntityFrameworkCore;

namespace GridFlow.Infrastructure.Persistence;

public sealed class GridFlowDbContext(DbContextOptions<GridFlowDbContext> options) : DbContext(options)
{
    public DbSet<GasFlowRecord> GasFlowRecords => Set<GasFlowRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GridFlowDbContext).Assembly);
    }
}