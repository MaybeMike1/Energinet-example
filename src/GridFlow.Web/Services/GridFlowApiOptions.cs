namespace GridFlow.Web.Services;

public sealed class GridFlowApiOptions
{
    public const string SectionName = "GridFlowApi";

    public string BaseUrl { get; set; } = "http://localhost:5087";
}