namespace GridFlow.UnitTests.TestDoubles;

/// <summary>
/// A fake <see cref="HttpMessageHandler"/> that returns canned responses. When more than one
/// response factory is supplied, each call dequeues the next one (the last is reused once reached),
/// which makes it easy to simulate "429 then 200" retry scenarios. Factories are used so each call
/// yields a fresh, undisposed <see cref="HttpResponseMessage"/>.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpResponseMessage>> _responses;

    public StubHttpMessageHandler(params Func<HttpResponseMessage>[] responses)
    {
        if (responses.Length == 0)
        {
            throw new ArgumentException("At least one response is required.", nameof(responses));
        }

        _responses = new Queue<Func<HttpResponseMessage>>(responses);
    }

    public int CallCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        var factory = _responses.Count > 1 ? _responses.Dequeue() : _responses.Peek();
        return Task.FromResult(factory());
    }
}