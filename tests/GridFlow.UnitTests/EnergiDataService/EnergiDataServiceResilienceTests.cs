using System.Net;
using System.Text;

using GridFlow.Application.GasFlows;
using GridFlow.Infrastructure.EnergiDataService;
using GridFlow.UnitTests.TestDoubles;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace GridFlow.UnitTests.EnergiDataService;

public sealed class EnergiDataServiceResilienceTests
{
    private const string SingleRecordJson = """
    {
      "total": 1,
      "dataset": "Gasflow",
      "records": [
        {
          "GasDay": "2026-06-12T00:00:00",
          "KWhFromBiogas": 1,
          "KWhToDenmark": 2,
          "KWhFromNorthSea": 3,
          "KWhToOrFromStorage": 4,
          "KWhToOrFromGermany": 5,
          "KWhToSweden": 6,
          "kWhFromTyra": 7,
          "KWhToPoland": 8
        }
      ]
    }
    """;

    [Fact]
    public async Task GivenTooManyRequestsThenSuccess_WhenGettingGasFlow_ThenRetriesAndSucceeds()
    {
        var handler = new StubHttpMessageHandler(
            () => new HttpResponseMessage(HttpStatusCode.TooManyRequests),
            () => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(SingleRecordJson, Encoding.UTF8, "application/json"),
            });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EnergiDataService:BaseUrl"] = "https://api.energidataservice.dk/",
                ["EnergiDataService:Dataset"] = "Gasflow",
                ["EnergiDataService:MaxRetries"] = "3",
                // Keep the test fast: 1 ms base backoff.
                ["EnergiDataService:BaseRetryDelay"] = "00:00:00.001",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FakeTimeProvider());
        services.AddEnergiDataService(configuration)
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        await using var provider = services.BuildServiceProvider();
        var source = provider.GetRequiredService<IGasFlowSource>();

        var records = await source.GetGasFlowAsync(
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddDays(1),
            TestContext.Current.CancellationToken);

        records.Should().ContainSingle();
        handler.CallCount.Should().Be(2, "the 429 response should trigger exactly one retry");
    }
}