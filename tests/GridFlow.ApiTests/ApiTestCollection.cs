namespace GridFlow.ApiTests;

[CollectionDefinition(Name)]
public sealed class ApiTestCollection : ICollectionFixture<ApiTestFixture>
{
    public const string Name = "ApiTests";
}