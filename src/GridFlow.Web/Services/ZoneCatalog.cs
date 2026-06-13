namespace GridFlow.Web.Services;

public sealed record ZoneOption(string Slug, string Label);

public static class ZoneCatalog
{
    public static readonly IReadOnlyList<ZoneOption> All =
    [
        new("from-biogas", "From biogas"),
        new("to-denmark", "To Denmark"),
        new("from-north-sea", "From North Sea"),
        new("to-or-from-storage", "To/from storage"),
        new("to-or-from-germany", "To/from Germany"),
        new("to-sweden", "To Sweden"),
        new("from-tyra", "From Tyra"),
        new("to-poland", "To Poland"),
    ];

    public static string GetLabel(string slug) =>
        All.FirstOrDefault(z => z.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase))?.Label ?? slug;
}