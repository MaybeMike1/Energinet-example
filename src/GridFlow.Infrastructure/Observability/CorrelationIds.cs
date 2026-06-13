namespace GridFlow.Infrastructure.Observability;

/// <summary>Shared correlation-id header and log property names.</summary>
public static class CorrelationIds
{
    public const string HeaderName = "X-Correlation-Id";
    public const string LogPropertyName = "CorrelationId";
}