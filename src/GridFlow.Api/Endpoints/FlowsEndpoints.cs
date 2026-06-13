using GridFlow.Application.GasFlows;

namespace GridFlow.Api.Endpoints;

public static class FlowsEndpoints
{
    public static RouteGroupBuilder MapFlowsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/flows")
            .WithTags("Flows");

        group.MapGet("/", GetFlowsAsync)
            .WithName("GetFlows")
            .WithSummary("Paginated gas-flow observations for the dashboard table.");

        group.MapGet("/summary", GetSummaryAsync)
            .WithName("GetFlowSummary")
            .WithSummary("Time-series points for the dashboard chart.");

        return group;
    }

    private static async Task<IResult> GetFlowsAsync(
        GasFlowQueryService queryService,
        string? from,
        string? to,
        string? zone,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        if (!TryParseDate(from, out var fromDay, out var fromError))
        {
            return Results.Problem(fromError, statusCode: StatusCodes.Status400BadRequest);
        }

        if (!TryParseDate(to, out var toDay, out var toError))
        {
            return Results.Problem(toError, statusCode: StatusCodes.Status400BadRequest);
        }

        GasFlowZone? selectedZone = null;
        if (GasFlowZoneParser.TryParse(zone, out var parsedZone, out var zoneError))
        {
            selectedZone = parsedZone;
        }
        else if (zoneError is not null)
        {
            return Results.Problem(zoneError, statusCode: StatusCodes.Status400BadRequest);
        }

        if (pageSize > queryService.MaxPageSize)
        {
            return Results.Problem(
                $"pageSize cannot exceed {queryService.MaxPageSize}.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var result = await queryService.GetFlowsAsync(fromDay, toDay, selectedZone, page, pageSize, cancellationToken)
                .ConfigureAwait(false);
            return Results.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetSummaryAsync(
        GasFlowQueryService queryService,
        string? from,
        string? to,
        string? zone,
        CancellationToken cancellationToken)
    {
        if (!TryParseDate(from, out var fromDay, out var fromError))
        {
            return Results.Problem(fromError, statusCode: StatusCodes.Status400BadRequest);
        }

        if (!TryParseDate(to, out var toDay, out var toError))
        {
            return Results.Problem(toError, statusCode: StatusCodes.Status400BadRequest);
        }

        if (!GasFlowZoneParser.TryParse(zone, out var selectedZone, out var zoneError) || zoneError is not null)
        {
            var detail = zoneError ?? "The 'zone' query parameter is required for summary (e.g. from-north-sea).";
            return Results.Problem(detail, statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var points = await queryService.GetSummaryAsync(fromDay, toDay, selectedZone, cancellationToken)
                .ConfigureAwait(false);
            return Results.Ok(new { zone = GasFlowZoneParser.ToSlug(selectedZone), points });
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static bool TryParseDate(string? value, out DateOnly? date, out string? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            date = null;
            error = null;
            return true;
        }

        if (DateOnly.TryParse(value, out var parsed))
        {
            date = parsed;
            error = null;
            return true;
        }

        date = null;
        error = $"Invalid date '{value}'. Use ISO format yyyy-MM-dd.";
        return false;
    }
}