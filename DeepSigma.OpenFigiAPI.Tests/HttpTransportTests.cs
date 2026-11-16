using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace DeepSigma.OpenFigiAPI.Tests;

/// <summary>The package's own transport, on the DeepSigma web handlers, over a fake handler: the key and body sent, 429,
/// 500 and 503 retried, other refusals returned at once.</summary>
public sealed class HttpTransportTests
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private sealed class FakeHandler(params Func<HttpResponseMessage>[] responses) : HttpMessageHandler
    {
        private int _next;
        public List<(HttpRequestMessage Request, string? Body)> Received { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Received.Add((request, request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));
            return responses[Math.Min(_next++, responses.Length - 1)]();   // a new message each time: the transport disposes it
        }
    }

    private static Func<HttpResponseMessage> Status(HttpStatusCode status, string body = "", TimeSpan? retryAfter = null) => () =>
    {
        var response = new HttpResponseMessage(status) { Content = new StringContent(body) };
        if (retryAfter is { } wait) response.Headers.RetryAfter = new RetryConditionHeaderValue(wait);
        return response;
    };

    private static readonly OpenFigiRequest Mapping = new(HttpMethod.Post, "/v3/mapping", """[{"idType":"ID_ISIN","idValue":"US0378331005"}]""",
                                                          OpenFigiCallGroup.Mapping);

    private static OpenFigiOptions Fast(string? key = null) =>
        new() { ApiKey = key, BaseBackoff = TimeSpan.Zero, MaxBackoff = TimeSpan.Zero, LimitRequests = false };

    [Fact]
    public async Task ARequestCarriesTheKeyAndItsJsonBody()
    {
        var handler = new FakeHandler(Status(HttpStatusCode.OK, "[]"));
        var transport = new HttpOpenFigiTransport(Fast("the-key"), handler);

        var response = await transport.SendAsync(Mapping, Cancellation);

        Assert.Equal((200, "[]"), (response.StatusCode, response.Body));
        var (request, body) = Assert.Single(handler.Received);
        Assert.Equal("https://api.openfigi.com/v3/mapping", request.RequestUri!.ToString());
        Assert.Equal("the-key", Assert.Single(request.Headers.GetValues("X-OPENFIGI-APIKEY")));
        Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);
        Assert.Equal(Mapping.JsonBody, body);
    }

    [Fact]
    public async Task WithoutAKeyNoKeyIsSent()
    {
        var handler = new FakeHandler(Status(HttpStatusCode.OK, "[]"));
        await new HttpOpenFigiTransport(Fast(), handler).SendAsync(Mapping, Cancellation);
        Assert.False(handler.Received[0].Request.Headers.Contains("X-OPENFIGI-APIKEY"));
    }

    [Fact]
    public async Task ARateLimitAndAnOutageAreRetried()
    {
        var handler = new FakeHandler(Status(HttpStatusCode.TooManyRequests, "Too many requests", TimeSpan.Zero),
                                      Status(HttpStatusCode.ServiceUnavailable), Status(HttpStatusCode.OK, "[]"));
        var response = await new HttpOpenFigiTransport(Fast(), handler).SendAsync(Mapping, Cancellation);
        Assert.Equal(200, response.StatusCode);
        Assert.Equal(3, handler.Received.Count);
    }

    [Fact]
    public async Task RetriesStopAtTheMaximumAndTheLastAnswerIsReturned()
    {
        var handler = new FakeHandler(Status(HttpStatusCode.ServiceUnavailable, "down"));
        var response = await new HttpOpenFigiTransport(Fast() with { MaxAttempts = 3 }, handler).SendAsync(Mapping, Cancellation);
        Assert.Equal((503, "down"), (response.StatusCode, response.Body));
        Assert.Equal(3, handler.Received.Count);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.RequestEntityTooLarge)]
    public async Task ARefusalIsReturnedAtOnceNotRetried(HttpStatusCode status)
    {
        var handler = new FakeHandler(Status(status, "refused"), Status(HttpStatusCode.OK, "[]"));
        var response = await new HttpOpenFigiTransport(Fast(), handler).SendAsync(Mapping, Cancellation);
        Assert.Equal((int)status, response.StatusCode);
        Assert.Single(handler.Received);
    }

    [Fact]
    public async Task ARetryAfterIsPassedOnWhenTheLastAttemptIsALimit()
    {
        var handler = new FakeHandler(Status(HttpStatusCode.TooManyRequests, "Too many requests", TimeSpan.FromSeconds(42)));
        var response = await new HttpOpenFigiTransport(Fast() with { MaxAttempts = 1 }, handler).SendAsync(Mapping, Cancellation);
        Assert.Equal((429, TimeSpan.FromSeconds(42)), (response.StatusCode, response.RetryAfter));
    }

    [Fact]
    public async Task RequestsAreSpacedToTheLimitOfTheirGroup()
    {
        // 600 a minute: one every 100 ms, so three mapping requests take at least 200 ms; a search is spaced on its own
        var handler = new FakeHandler(Status(HttpStatusCode.OK, "[]"));
        var transport = new HttpOpenFigiTransport(Fast() with { LimitRequests = true, MappingRequestsPerMinute = 600 }, handler);
        var started = System.Diagnostics.Stopwatch.StartNew();
        for (var i = 0; i < 3; i++) await transport.SendAsync(Mapping, Cancellation);
        Assert.True(started.Elapsed >= TimeSpan.FromMilliseconds(190), $"three requests took {started.Elapsed.TotalMilliseconds} ms");
        Assert.Equal(3, handler.Received.Count);
    }
}
