namespace GridFlow.Application.GasFlows;

/// <summary>One point in a time-series chart for a selected flow zone.</summary>
public sealed record GasFlowSummaryPointDto(DateOnly GasDay, long ValueKWh);