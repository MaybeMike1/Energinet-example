using GridFlow.Domain.GasFlows;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridFlow.Infrastructure.Persistence.Configurations;

public sealed class GasFlowRecordConfiguration : IEntityTypeConfiguration<GasFlowRecord>
{
    public void Configure(EntityTypeBuilder<GasFlowRecord> builder)
    {
        builder.ToTable("GasFlowRecords");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Dataset)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.GasDay)
            .IsRequired();

        builder.Property(x => x.RetrievedAtUtc)
            .IsRequired();

        // Idempotent ingestion relies on this: one row per (dataset, gas day).
        builder.HasIndex(x => new { x.Dataset, x.GasDay })
            .IsUnique();
    }
}