using System.ComponentModel.DataAnnotations;

namespace GridFlow.Infrastructure.EnergiDataService;

/// <summary>
/// Configuration for the Energi Data Service client. Bound from the "EnergiDataService" section and
/// validated at startup. Defaults target the public, key-less Gasflow dataset.
/// </summary>
public sealed class EnergiDataServiceOptions
{
    public const string SectionName = "EnergiDataService";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = "https://api.energidataservice.dk/";

    [Required]
    public string Dataset { get; set; } = "Gasflow";

    /// <summary>Maximum number of records requested per call.</summary>
    [Range(1, 10_000)]
    public int DefaultLimit { get; set; } = 100;

    /// <summary>Per-attempt request timeout enforced by the resilience pipeline.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Bounded retry attempts for transient failures and HTTP 429.</summary>
    [Range(0, 10)]
    public int MaxRetries { get; set; } = 3;

    /// <summary>Base delay for the jittered exponential backoff (overridden by a 429 Retry-After hint).</summary>
    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
}