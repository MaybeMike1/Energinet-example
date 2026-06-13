using System.Net;
using System.Text;

using GridFlow.Domain.GasFlows;
using GridFlow.Infrastructure.EnergiDataService;
using GridFlow.UnitTests.TestDoubles;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace GridFlow.UnitTests.EnergiDataService;

public sealed class EnergiDataServiceClientTests
{
    // Two real records from the live Gasflow dataset. Note "kWhFromTyra" is lower-cased in the
    // source feed, which exercises case-insensitive deserialization.
    private const string GasflowJson = """
    {
      "total": 2,
      "dataset": "Gasflow",
      "records": [
        {
          "GasDay": "2026-06-12T00:00:00",
          "KWhFromBiogas": 25312656,
          "KWhToDenmark": -34839889,
          "KWhFromNorthSea": 207112725,
          "KWhToOrFromStorage": -5368000,
          "KWhToOrFromGermany": 106488,
          "KWhToSweden": -11824683,
          "kWhFromTyra": 40050073,
          "KWhToPoland": -241865040
        },
        {
          "GasDay": "2026-06-11T00:00:00",
          "KWhFromBiogas": 24830332,
          "KWhToDenmark": -39848832,
          "KWhFromNorthSea": 198659556,
          "KWhToOrFromStorage": 11261000,
          "KWhToOrFromGermany": 106488,
          "KWhToSweden": -13226335,
          "kWhFromTyra": 67393227,
          "KWhToPoland": -235121040
        }
      ]
    }
    """;

    private static readonly DateTimeOffset Start = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = new(2026, 6, 13, 0, 0, 0, TimeSpan.Zero);

    private static EnergiDataServiceClient CreateClient(StubHttpMessageHandler handler, FakeTimeProvider timeProvider)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.energidataservice.dk/"),
        };

        var options = Options.Create(new EnergiDataServiceOptions { Dataset = "Gasflow" });
        return new EnergiDataServiceClient(httpClient, options, timeProvider);
    }

    private static StubHttpMessageHandler JsonResponse(string json) =>
        new(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });

    [Fact]
    public async Task GivenSuccessfulResponse_WhenGettingGasFlow_ThenMapsRecordsFromRealFields()
    {
        var retrievedAt = new DateTimeOffset(2026, 6, 13, 9, 0, 0, TimeSpan.Zero);
        var client = CreateClient(JsonResponse(GasflowJson), new FakeTimeProvider(retrievedAt));

        var records = await client.GetGasFlowAsync(Start, End, TestContext.Current.CancellationToken);

        records.Should().HaveCount(2);

        var latest = records[0];
        latest.Dataset.Should().Be("Gasflow");
        latest.GasDay.Should().Be(new DateOnly(2026, 6, 12));
        latest.RetrievedAtUtc.Should().Be(retrievedAt);
        latest.ToValues().Should().Be(new GasFlowValues(
            KWhFromBiogas: 25_312_656,
            KWhToDenmark: -34_839_889,
            KWhFromNorthSea: 207_112_725,
            KWhToOrFromStorage: -5_368_000,
            KWhToOrFromGermany: 106_488,
            KWhToSweden: -11_824_683,
            KWhFromTyra: 40_050_073,
            KWhToPoland: -241_865_040));
    }

    [Fact]
    public async Task GivenEmptyRecords_WhenGettingGasFlow_ThenReturnsEmptyList()
    {
        const string emptyJson = """{ "total": 0, "dataset": "Gasflow", "records": [] }""";
        var client = CreateClient(JsonResponse(emptyJson), new FakeTimeProvider());

        var records = await client.GetGasFlowAsync(Start, End, TestContext.Current.CancellationToken);

        records.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenServerError_WhenGettingGasFlow_ThenThrowsHttpRequestException()
    {
        var handler = new StubHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = CreateClient(handler, new FakeTimeProvider());

        var act = async () => await client.GetGasFlowAsync(Start, End, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<HttpRequestException>();
    }
}