using System.Net;
using System.Net.Http.Headers;

namespace DeepSigma.OpenFigiAPI.Tests;

/// <summary>An HTTP handler that answers with its responses in turn (the last again once they run out), and keeps what it
/// received. Each response is made afresh, since the pipeline disposes what it is given.</summary>
internal sealed class FakeHandler(params Func<HttpResponseMessage>[] responses) : HttpMessageHandler
{
    private int _next;

    public List<(HttpRequestMessage Request, string? Body)> Received { get; } = [];

    /// <summary>A response with this status and body, and a <c>retry-after</c> when given.</summary>
    public static Func<HttpResponseMessage> Status(HttpStatusCode status, string body = "", TimeSpan? retryAfter = null) => () =>
    {
        var response = new HttpResponseMessage(status) { Content = new StringContent(body) };
        if (retryAfter is { } wait) response.Headers.RetryAfter = new RetryConditionHeaderValue(wait);
        return response;
    };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Received.Add((request, request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));
        return responses[Math.Min(_next++, responses.Length - 1)]();
    }
}
