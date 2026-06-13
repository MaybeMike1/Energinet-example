namespace GridFlow.Application.GasFlows;

/// <summary>A single gas-flow observation shaped for the dashboard table.</summary>
public sealed record GasFlowDto(
    DateOnly GasDay,
    long KWhFromBiogas,
    long KWhToDenmark,
    long KWhFromNorthSea,
    long KWhToOrFromStorage,
    long KWhToOrFromGermany,
    long KWhToSweden,
    long KWhFromTyra,
    long KWhToPoland,
    DateTimeOffset RetrievedAtUtc);