using System.Text.Json.Serialization;

namespace GridFlow.Infrastructure.EnergiDataService;

/// <summary>
/// The envelope returned by <c>GET /dataset/{name}</c>. Only the fields we consume are modelled.
/// </summary>
internal sealed class EnergiDataServiceResponse
{
    public int Total { get; set; }

    public string? Dataset { get; set; }

    public List<GasFlowDto> Records { get; set; } = [];
}

/// <summary>
/// One "Gasflow" record, modelled directly from the live response. Property names match the dataset
/// fields; deserialization is case-insensitive (web defaults), which also handles the lower-case
/// "kWhFromTyra" field.
/// </summary>
internal sealed class GasFlowDto
{
    [JsonPropertyName("GasDay")]
    public DateTime GasDay { get; set; }

    public long KWhFromBiogas { get; set; }
    public long KWhToDenmark { get; set; }
    public long KWhFromNorthSea { get; set; }
    public long KWhToOrFromStorage { get; set; }
    public long KWhToOrFromGermany { get; set; }
    public long KWhToSweden { get; set; }
    public long KWhFromTyra { get; set; }
    public long KWhToPoland { get; set; }
}