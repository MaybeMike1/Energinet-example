using GridFlow.Infrastructure.Observability;

using Microsoft.AspNetCore.Http;

namespace GridFlow.UnitTests.Observability;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task GivenNoIncomingHeader_WhenRequestProcessed_ThenGeneratesCorrelationIdOnResponse()
    {
        var middleware = new CorrelationIdMiddleware(_ =>
        {
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Headers[CorrelationIds.HeaderName].ToString()
            .Should().NotBeNullOrWhiteSpace()
            .And.HaveLength(32);
    }

    [Fact]
    public async Task GivenIncomingHeader_WhenRequestProcessed_ThenEchoesSameCorrelationId()
    {
        const string correlationId = "abc123def456";

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIds.HeaderName] = correlationId;
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Headers[CorrelationIds.HeaderName].ToString()
            .Should().Be(correlationId);
    }
}