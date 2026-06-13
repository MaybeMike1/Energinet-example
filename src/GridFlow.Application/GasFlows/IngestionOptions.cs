using System.ComponentModel.DataAnnotations;

namespace GridFlow.Application.GasFlows;

/// <summary>
/// Controls the ingestion worker. <see cref="Interval"/> drives the polling cadence (keep it at or
/// below the dataset's update frequency to respect rate limits); <see cref="Window"/> is the lookback
/// span fetched on each tick so late corrections to past gas days are picked up.
/// </summary>
public sealed class IngestionOptions
{
    public const string SectionName = "Ingestion";

    [Range(typeof(TimeSpan), "00:00:30", "1.00:00:00")]
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(6);

    [Range(typeof(TimeSpan), "00:01:00", "90.00:00:00")]
    public TimeSpan Window { get; set; } = TimeSpan.FromDays(7);
}