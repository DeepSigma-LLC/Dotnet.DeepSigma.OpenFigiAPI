using System.Net;
using Xunit;

namespace DeepSigma.OpenFigiAPI.Tests;

/// <summary>The package's own transport, on the DeepSigma web handlers, over a fake handler: the key and body sent, 429,
/// 500 and 503 retried, other refusals returned at once.</summary>
public sealed class HttpTransportTests
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static readonly OpenFigiRequest Mapping = new(HttpMethod.Post, "/v3/mapping", """[{"idType":"ID_ISIN","idValue":"US0378331005"}]""",
                                                          OpenFigiCallGroup.Mapping);

    private static async Task<OpenFigiResponse> Send(HttpOpenFigiTransport transport)
    {
        using (transport) return await transport.SendAsync(Mapping, Cancellation);
    }

    private static OpenFigiOptions Fast(string? key = null) =>
        new() { ApiKey = key, BaseBackoff = TimeSpan.Zero, MaxBackoff = TimeSpan.Zero, LimitRequests = false };

    [Fact]
    public async Task ARequestCarriesTheKeyAndItsJsonBody()
    {
        var handler = new FakeHandler(FakeHandler.Status(HttpStatusCode.OK, "[]"));
        using var transport = new HttpOpenFigiTransport(Fast("the-key"), handler);

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
        var handler = new FakeHandler(FakeHandler.Status(HttpStatusCode.OK, "[]"));
        await Send(new HttpOpenFigiTransport(Fast(), handler));
        Assert.False(handler.Received[0].Request.Headers.Contains("X-OPENFIGI-APIKEY"));
    }

    [Fact]
    public async Task ARateLimitAndAnOutageAreRetried()
    {
        var handler = new FakeHandler(FakeHandler.Status(HttpStatusCode.TooManyRequests, "Too many requests", TimeSpan.Zero),
                                      FakeHandler.Status(HttpStatusCode.ServiceUnavailable), FakeHandler.Status(HttpStatusCode.OK, "[]"));
        var response = await Send(new HttpOpenFigiTransport(Fast(), handler));
        Assert.Equal(200, response.StatusCode);
        Assert.Equal(3, handler.Received.Count);
    }

    [Fact]
    public async Task RetriesStopAtTheMaximumAndTheLastAnswerIsReturned()
    {
        var handler = new FakeHandler(FakeHandler.Status(HttpStatusCode.ServiceUnavailable, "down"));
        var response = await Send(new HttpOpenFigiTransport(Fast() with { MaxAttempts = 3 }, handler));
        Assert.Equal((503, "down"), (response.StatusCode, response.Body));
        Assert.Equal(3, handler.Received.Count);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.RequestEntityTooLarge)]
    public async Task ARefusalIsReturnedAtOnceNotRetried(HttpStatusCode status)
    {
        var handler = new FakeHandler(FakeHandler.Status(status, "refused"), FakeHandler.Status(HttpStatusCode.OK, "[]"));
        var response = await Send(new HttpOpenFigiTransport(Fast(), handler));
        Assert.Equal((int)status, response.StatusCode);
        Assert.Single(handler.Received);
    }

    [Fact]
    public async Task ARetryAfterIsPassedOnWhenTheLastAttemptIsALimit()
    {
        var handler = new FakeHandler(FakeHandler.Status(HttpStatusCode.TooManyRequests, "Too many requests", TimeSpan.FromSeconds(42)));
        var response = await Send(new HttpOpenFigiTransport(Fast() with { MaxAttempts = 1 }, handler));
        Assert.Equal((429, TimeSpan.FromSeconds(42)), (response.StatusCode, response.RetryAfter));
    }

    [Fact]
    public async Task RequestsAreSpacedToTheLimitOfTheirGroup()
    {
        // 600 a minute: one every 100 ms, so three mapping requests take at least 200 ms; a search is spaced on its own
        var handler = new FakeHandler(FakeHandler.Status(HttpStatusCode.OK, "[]"));
        using var transport = new HttpOpenFigiTransport(Fast() with { LimitRequests = true, MappingRequestsPerMinute = 600 }, handler);
        var started = System.Diagnostics.Stopwatch.StartNew();
        for (var i = 0; i < 3; i++) await transport.SendAsync(Mapping, Cancellation);
        Assert.True(started.Elapsed >= TimeSpan.FromMilliseconds(190), $"three requests took {started.Elapsed.TotalMilliseconds} ms");
        Assert.Equal(3, handler.Received.Count);
    }
}
