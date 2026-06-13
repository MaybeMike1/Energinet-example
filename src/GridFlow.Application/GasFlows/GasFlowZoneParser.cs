namespace GridFlow.Application.GasFlows;

/// <summary>Parses and normalises the optional <c>zone</c> query parameter.</summary>
public static class GasFlowZoneParser
{
    private static readonly Dictionary<string, GasFlowZone> BySlug = new(StringComparer.OrdinalIgnoreCase)
    {
        ["from-biogas"] = GasFlowZone.FromBiogas,
        ["to-denmark"] = GasFlowZone.ToDenmark,
        ["from-north-sea"] = GasFlowZone.FromNorthSea,
        ["to-or-from-storage"] = GasFlowZone.ToOrFromStorage,
        ["to-or-from-germany"] = GasFlowZone.ToOrFromGermany,
        ["to-sweden"] = GasFlowZone.ToSweden,
        ["from-tyra"] = GasFlowZone.FromTyra,
        ["to-poland"] = GasFlowZone.ToPoland,
    };

    public static bool TryParse(string? value, out GasFlowZone zone, out string? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            zone = default;
            error = null;
            return false;
        }

        if (BySlug.TryGetValue(value.Trim(), out zone))
        {
            error = null;
            return true;
        }

        error = $"Unknown zone '{value}'. Valid values: {string.Join(", ", BySlug.Keys.Order(StringComparer.Ordinal))}.";
        zone = default;
        return false;
    }

    public static long GetValue(GasFlowZone zone, GasFlowDto record) => zone switch
    {
        GasFlowZone.FromBiogas => record.KWhFromBiogas,
        GasFlowZone.ToDenmark => record.KWhToDenmark,
        GasFlowZone.FromNorthSea => record.KWhFromNorthSea,
        GasFlowZone.ToOrFromStorage => record.KWhToOrFromStorage,
        GasFlowZone.ToOrFromGermany => record.KWhToOrFromGermany,
        GasFlowZone.ToSweden => record.KWhToSweden,
        GasFlowZone.FromTyra => record.KWhFromTyra,
        GasFlowZone.ToPoland => record.KWhToPoland,
        _ => throw new ArgumentOutOfRangeException(nameof(zone), zone, null),
    };

    public static string ToSlug(GasFlowZone zone) =>
        BySlug.First(pair => pair.Value == zone).Key;
}