namespace GridFlow.Domain.GasFlows;

/// <summary>
/// The measured energy quantities (kWh) for a single Danish gas day, as published by the Energi
/// Data Service "Gasflow" dataset. Values can be negative (e.g. net export / withdrawal).
/// </summary>
public readonly record struct GasFlowValues(
    long KWhFromBiogas,
    long KWhToDenmark,
    long KWhFromNorthSea,
    long KWhToOrFromStorage,
    long KWhToOrFromGermany,
    long KWhToSweden,
    long KWhFromTyra,
    long KWhToPoland);