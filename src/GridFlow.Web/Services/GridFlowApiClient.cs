using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using GridFlow.Application.GasFlows;

namespace GridFlow.Web.Services;

public sealed class GridFlowApiClient(HttpClient httpClient) : IGridFlowApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PaginatedResult<GasFlowDto>> GetFlowsAsync(
        DateOnly from,
        DateOnly to,
        string? zone,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var uri = BuildFlowsUri(from, to, zone, page, pageSize);
        using var response = await httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        return await response.Content
            .ReadFromJsonAsync<PaginatedResult<GasFlowDto>>(JsonOptions, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new GridFlowApiException("The flows endpoint returned an empty body.");
    }

    public async Task<GasFlowSummaryResponse> GetSummaryAsync(
        DateOnly from,
        DateOnly to,
        string zone,
        CancellationToken cancellationToken)
    {
        var uri = $"api/v1/flows/summary?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&zone={Uri.EscapeDataString(zone)}";
        using var response = await httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        return await response.Content
            .ReadFromJsonAsync<GasFlowSummaryResponse>(JsonOptions, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new GridFlowApiException("The summary endpoint returned an empty body.");
    }

    private static string BuildFlowsUri(DateOnly from, DateOnly to, string? zone, int page, int pageSize)
    {
        var builder = new StringBuilder("api/v1/flows?from=");
        builder.Append(from.ToString("yyyy-MM-dd"));
        builder.Append("&to=").Append(to.ToString("yyyy-MM-dd"));
        builder.Append("&page=").Append(page);
        builder.Append("&pageSize=").Append(pageSize);
        if (!string.IsNullOrWhiteSpace(zone))
        {
            builder.Append("&zone=").Append(Uri.EscapeDataString(zone));
        }

        return builder.ToString();
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var detail = await TryReadProblemDetailAsync(response, cancellationToken).ConfigureAwait(false);
        throw new GridFlowApiException(detail ?? $"The API returned HTTP {(int)response.StatusCode}.");
    }

    private static async Task<string?> TryReadProblemDetailAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content
                .ReadFromJsonAsync<ProblemDetailsPayload>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            return problem?.Detail ?? problem?.Title;
        }
        catch (JsonException)
        {
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class ProblemDetailsPayload
    {
        public string? Title { get; set; }
        public string? Detail { get; set; }
    }
}