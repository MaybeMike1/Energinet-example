using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using GridFlow.Application.GasFlows;
using GridFlow.Domain.GasFlows;

using Microsoft.Extensions.Options;

namespace GridFlow.Infrastructure.EnergiDataService;

/// <summary>
/// Typed client for Energinet's Energi Data Service. Resilience (timeout, jittered retry, 429
/// handling) is attached to the underlying <see cref="HttpClient"/> via the DI registration; this
/// type only builds the request and maps the response to domain entities.
/// </summary>
public sealed class EnergiDataServiceClient : IGasFlowSource
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly TimeProvider _timeProvider;
    private readonly EnergiDataServiceOptions _options;

    public EnergiDataServiceClient(
        HttpClient httpClient,
        IOptions<EnergiDataServiceOptions> options,
        TimeProvider timeProvider)
    {
        _httpClient = httpClient;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<GasFlowRecord>> GetGasFlowAsync(
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken)
    {
        var requestUri = BuildRequestUri(start, end);

        using var response = await _httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content
            .ReadFromJsonAsync<EnergiDataServiceResponse>(JsonOptions, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Energi Data Service returned an empty response body.");

        var retrievedAt = _timeProvider.GetUtcNow();
        var records = new List<GasFlowRecord>(payload.Records.Count);
        foreach (var dto in payload.Records)
        {
            records.Add(Map(dto, retrievedAt));
        }

        return records;
    }

    private GasFlowRecord Map(GasFlowDto dto, DateTimeOffset retrievedAt) =>
        GasFlowRecord.Create(
            _options.Dataset,
            DateOnly.FromDateTime(dto.GasDay),
            new GasFlowValues(
                dto.KWhFromBiogas,
                dto.KWhToDenmark,
                dto.KWhFromNorthSea,
                dto.KWhToOrFromStorage,
                dto.KWhToOrFromGermany,
                dto.KWhToSweden,
                dto.KWhFromTyra,
                dto.KWhToPoland),
            retrievedAt);

    private string BuildRequestUri(DateTimeOffset start, DateTimeOffset end)
    {
        var builder = new StringBuilder("dataset/");
        builder.Append(Uri.EscapeDataString(_options.Dataset));
        builder.Append("?start=").Append(FormatTimestamp(start));
        builder.Append("&end=").Append(FormatTimestamp(end));
        builder.Append("&limit=").Append(_options.DefaultLimit.ToString(CultureInfo.InvariantCulture));
        builder.Append("&sort=").Append(Uri.EscapeDataString("GasDay DESC"));
        return builder.ToString();
    }

    private static string FormatTimestamp(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
}