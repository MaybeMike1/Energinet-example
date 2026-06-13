using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using GridFlow.Application.GasFlows;

namespace GridFlow.ApiTests;

[Collection(ApiTestCollection.Name)]
public sealed class FlowsEndpointTests(ApiTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GivenSeededData_WhenGettingFlows_ThenReturnsPaginatedResults()
    {
        var response = await fixture.Client.GetAsync(
            "/api/v1/flows?from=2026-06-01&to=2026-06-30&page=1&pageSize=2",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PaginatedResult<GasFlowDto>>(JsonOptions, TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Items.Should().HaveCount(2);
        body.TotalCount.Should().Be(3);
        body.Page.Should().Be(1);
        body.PageSize.Should().Be(2);
        body.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GivenMissingFromDate_WhenGettingFlows_ThenReturns400ProblemDetails()
    {
        var response = await fixture.Client.GetAsync(
            "/api/v1/flows?to=2026-06-30",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .Should().Contain("from");
    }

    [Fact]
    public async Task GivenInvalidZone_WhenGettingFlows_ThenReturns400ProblemDetails()
    {
        var response = await fixture.Client.GetAsync(
            "/api/v1/flows?from=2026-06-01&to=2026-06-30&zone=atlantis",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .Should().Contain("Unknown zone");
    }

    [Fact]
    public async Task GivenPageSizeOverCap_WhenGettingFlows_ThenReturns400ProblemDetails()
    {
        var response = await fixture.Client.GetAsync(
            "/api/v1/flows?from=2026-06-01&to=2026-06-30&pageSize=500",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .Should().Contain("pageSize");
    }

    [Fact]
    public async Task GivenSeededData_WhenGettingSummary_ThenReturnsSeriesForZone()
    {
        var response = await fixture.Client.GetAsync(
            "/api/v1/flows/summary?from=2026-06-01&to=2026-06-30&zone=from-north-sea",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        json.Should().Contain("from-north-sea");
        json.Should().Contain("points");
    }

    [Fact]
    public async Task GivenMissingZone_WhenGettingSummary_ThenReturns400ProblemDetails()
    {
        var response = await fixture.Client.GetAsync(
            "/api/v1/flows/summary?from=2026-06-01&to=2026-06-30",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .Should().Contain("zone");
    }
}