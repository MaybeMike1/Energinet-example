namespace GridFlow.Web.Services;

public sealed class DashboardOptions
{
    public const string SectionName = "Dashboard";

    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(1);
}