using GridFlow.Application.GasFlows;

namespace GridFlow.Web.Services;

public sealed record GasFlowSummaryResponse(string Zone, IReadOnlyList<GasFlowSummaryPointDto> Points);