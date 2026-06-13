using System.Net;
using System.Text.Json;

using GridFlow.Infrastructure.Observability;

namespace GridFlow.ApiTests;

[Collection(ApiTestCollection.Name)]
public sealed class HealthEndpointTests(ApiTestFixture fixture)
{
    [Fact]
    public async Task GivenRunningHost_WhenCheckingLiveness_ThenReturnsOk()
    {
        var response = await fixture.Client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .Should().Contain("Healthy");
    }

    [Fact]
    public async Task GivenSeededDatabase_WhenCheckingReadiness_ThenReturnsStructuredJson()
    {
        var response = await fixture.Client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        document.RootElement.GetProperty("status").GetString().Should().NotBeNullOrWhiteSpace();
        document.RootElement.GetProperty("checks").EnumerateArray().Should().NotBeEmpty();
    }

    [Fact]
    public async Task GivenIncomingCorrelationId_WhenCallingApi_ThenEchoesSameHeader()
    {
        const string correlationId = "test-correlation-id-001";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add(CorrelationIds.HeaderName, correlationId);

        var response = await fixture.Client.SendAsync(request, TestContext.Current.CancellationToken);

        response.Headers.TryGetValues(CorrelationIds.HeaderName, out var values).Should().BeTrue();
        values!.Single().Should().Be(correlationId);
    }
}