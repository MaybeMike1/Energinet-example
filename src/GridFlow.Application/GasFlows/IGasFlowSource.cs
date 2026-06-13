using GridFlow.Domain.GasFlows;

namespace GridFlow.Application.GasFlows;

/// <summary>
/// Port for an external source of gas-flow observations (default: Energinet's Energi Data Service
/// "Gasflow" dataset). Implementations live in Infrastructure and are responsible for the transport,
/// resilience, and mapping the raw response into <see cref="GasFlowRecord"/> domain entities.
/// </summary>
public interface IGasFlowSource
{
    /// <summary>
    /// Fetches the gas-flow observations whose gas day falls within the inclusive
    /// [<paramref name="start"/>, <paramref name="end"/>] window.
    /// </summary>
    Task<IReadOnlyList<GasFlowRecord>> GetGasFlowAsync(
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken);
}