namespace GridFlow.Application.GasFlows;

/// <summary>Summary of a single ingestion cycle, used for logging and health reporting.</summary>
public sealed record IngestionResult(
    DateTimeOffset WindowStart,
    DateTimeOffset WindowEnd,
    int Fetched,
    int Inserted,
    int Updated);