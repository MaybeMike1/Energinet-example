namespace GridFlow.Web.Services;

public sealed class GridFlowApiException(string message) : Exception(message);