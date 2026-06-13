namespace GridFlow.Domain.GasFlows;

/// <summary>
/// A single normalized gas-flow observation for one gas day, sourced from a named Energi Data
/// Service dataset (default "Gasflow"). The natural key is (<see cref="Dataset"/>,
/// <see cref="GasDay"/>); ingestion upserts on that key so re-fetching a window is idempotent.
/// </summary>
public sealed class GasFlowRecord
{
    // Required by EF Core's materialization.
    private GasFlowRecord()
    {
    }

    private GasFlowRecord(string dataset, DateOnly gasDay, GasFlowValues values, DateTimeOffset retrievedAt)
    {
        Dataset = dataset;
        GasDay = gasDay;
        RetrievedAtUtc = retrievedAt.ToUniversalTime();
        ApplyValues(values);
    }

    public int Id { get; private set; }

    /// <summary>The Energi Data Service dataset this observation came from (part of the natural key).</summary>
    public string Dataset { get; private set; } = null!;

    /// <summary>The Danish gas day this observation describes (part of the natural key).</summary>
    public DateOnly GasDay { get; private set; }

    public long KWhFromBiogas { get; private set; }
    public long KWhToDenmark { get; private set; }
    public long KWhFromNorthSea { get; private set; }
    public long KWhToOrFromStorage { get; private set; }
    public long KWhToOrFromGermany { get; private set; }
    public long KWhToSweden { get; private set; }
    public long KWhFromTyra { get; private set; }
    public long KWhToPoland { get; private set; }

    /// <summary>When this observation was last fetched from the source, stored in UTC.</summary>
    public DateTimeOffset RetrievedAtUtc { get; private set; }

    public static GasFlowRecord Create(string dataset, DateOnly gasDay, GasFlowValues values, DateTimeOffset retrievedAt)
    {
        if (string.IsNullOrWhiteSpace(dataset))
        {
            throw new ArgumentException("Dataset must be provided.", nameof(dataset));
        }

        if (gasDay == default)
        {
            throw new ArgumentException("Gas day must be a real date.", nameof(gasDay));
        }

        return new GasFlowRecord(dataset.Trim(), gasDay, values, retrievedAt);
    }

    /// <summary>Refreshes the measured values when an existing record is re-ingested (idempotent upsert).</summary>
    public void Update(GasFlowValues values, DateTimeOffset retrievedAt)
    {
        ApplyValues(values);
        RetrievedAtUtc = retrievedAt.ToUniversalTime();
    }

    public GasFlowValues ToValues() => new(
        KWhFromBiogas,
        KWhToDenmark,
        KWhFromNorthSea,
        KWhToOrFromStorage,
        KWhToOrFromGermany,
        KWhToSweden,
        KWhFromTyra,
        KWhToPoland);

    private void ApplyValues(GasFlowValues values)
    {
        KWhFromBiogas = values.KWhFromBiogas;
        KWhToDenmark = values.KWhToDenmark;
        KWhFromNorthSea = values.KWhFromNorthSea;
        KWhToOrFromStorage = values.KWhToOrFromStorage;
        KWhToOrFromGermany = values.KWhToOrFromGermany;
        KWhToSweden = values.KWhToSweden;
        KWhFromTyra = values.KWhFromTyra;
        KWhToPoland = values.KWhToPoland;
    }
}