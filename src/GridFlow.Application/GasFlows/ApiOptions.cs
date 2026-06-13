using System.ComponentModel.DataAnnotations;

namespace GridFlow.Application.GasFlows;

/// <summary>API pagination and data-freshness settings, validated at startup.</summary>
public sealed class ApiOptions
{
    public const string SectionName = "Api";

    [Range(1, 500)]
    public int DefaultPageSize { get; set; } = 20;

    [Range(1, 500)]
    public int MaxPageSize { get; set; } = 100;

    /// <summary>Readiness is unhealthy when the newest stored record is older than this.</summary>
    public TimeSpan MaxDataAge { get; set; } = TimeSpan.FromHours(12);

    [Required]
    public string DefaultDataset { get; set; } = "Gasflow";
}