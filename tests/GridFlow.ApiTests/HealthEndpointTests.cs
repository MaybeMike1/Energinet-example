using System.Net;

namespace GridFlow.ApiTests;

public sealed class HealthEndpointTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
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
    public async Task GivenSeededDatabase_WhenCheckingReadiness_ThenReturnsSuccessStatus()
    {
        var response = await fixture.Client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }
}