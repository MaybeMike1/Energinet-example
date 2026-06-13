namespace GridFlow.Application.GasFlows;

/// <summary>
/// Identifies which gas-flow metric the dashboard filters or charts. Maps to the kWh fields on
/// <see cref="GasFlowRecord"/>; the API accepts kebab-case names (e.g. <c>from-north-sea</c>).
/// </summary>
public enum GasFlowZone
{
    FromBiogas,
    ToDenmark,
    FromNorthSea,
    ToOrFromStorage,
    ToOrFromGermany,
    ToSweden,
    FromTyra,
    ToPoland,
}