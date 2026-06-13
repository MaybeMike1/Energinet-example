namespace GridFlow.Application.GasFlows;

/// <summary>The outcome of an idempotent upsert: how many rows were newly inserted versus refreshed.</summary>
public readonly record struct UpsertResult(int Inserted, int Updated);