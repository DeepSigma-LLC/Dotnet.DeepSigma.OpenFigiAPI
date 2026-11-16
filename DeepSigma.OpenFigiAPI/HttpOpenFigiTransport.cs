using System.Net;
using System.Net.Http.Headers;
using System.Text;
using DeepSigma.DataAccess.Web.Throttling;

namespace DeepSigma.OpenFigiAPI;

/// <summary>How <see cref="HttpOpenFigiTransport"/> calls the API.</summary>
public sealed record OpenFigiOptions
{
    /// <summary>The key (<c>X-OPENFIGI-APIKEY</c>); optional. With one the API allows more requests and 100 jobs a
    /// request instead of 10.</summary>
    public string? ApiKey { get; init; }

    /// <summary>The API's address.</summary>
    public Uri BaseAddress { get; init; } = new("https://api.openfigi.com");

    /// <summary>Space requests to keep within the API's limits. Off for a caller that limits them elsewhere.</summary>
    public bool LimitRequests { get; init; } = true;

    /// <summary>Mapping and field-value requests a minute; by default the API's: 25 without a key, 250 with one.</summary>
    public int? MappingRequestsPerMinute { get; init; }

    /// <summary>Search and filter requests a minute; by default the API's: 5 without a key, 20 with one.</summary>
    public int? SearchRequestsPerMinute { get; init; }

    /// <summary>How many times a request is sent in all, when it meets a 429, 500 or 503.</summary>
    public int MaxAttempts { get; init; } = 5;

    /// <summary>The first wait after a 429 without <c>retry-after</c>, a 500 or a 503; doubled each time, with jitter, up
    /// to <see cref="MaxBackoff"/>.</summary>
    public TimeSpan BaseBackoff { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>The longest wait between attempts, whatever a 429 asks or the backoff reaches.</summary>
    public TimeSpan MaxBackoff { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>How long one call may take in all: every attempt, the waits between them, and the response's body.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>The clock waits are measured on; the system's by default.</summary>
    public TimeProvider Clock { get; init; } = TimeProvider.System;

    /// <summary>Whether a key is set.</summary>
    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);
}

/// <summary>
/// The package's own transport, on the DeepSigma web handlers (<c>DeepSigma.DataAccess.Web</c>): each call is sent with
/// an <see cref="HttpClient"/> whose pipeline retries a 429 (waiting for its <c>retry-after</c>), a 500 or a 503 with
/// capped exponential backoff (<see cref="RetryAfterDelegatingHandler"/>), and spaces requests to keep within the API's
/// limit for the call's group (<see cref="MinIntervalDelegatingHandler"/>; every attempt is spaced, retries included).
/// The spacing is shared by every client in the process, keyed by group, since the API counts by key or address, not by
/// client. Other statuses are returned at once, never retried. Safe to share across threads.
/// </summary>
public sealed class HttpOpenFigiTransport : IOpenFigiTransport
{
    /// <summary>The throttle keys the spacing is shared under, per group.</summary>
    public const string MappingThrottleKey = "openfigi:mapping", SearchThrottleKey = "openfigi:search";

    private readonly OpenFigiOptions _options;
    private readonly Dictionary<OpenFigiCallGroup, HttpClient> _clients;

    /// <param name="options">How to call the API; the defaults when null (no key, the keyless limits).</param>
    /// <param name="handler">The handler that sends the request at the end of the pipeline; a
    /// <see cref="SocketsHttpHandler"/> when null. A test passes a fake one. It is not disposed by the transport.</param>
    public HttpOpenFigiTransport(OpenFigiOptions? options = null, HttpMessageHandler? handler = null)
    {
        _options = options ?? new OpenFigiOptions();
        var inner = handler ?? new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) };
        _clients = new()
        {
            [OpenFigiCallGroup.Mapping] = Client(inner, MappingThrottleKey, _options.MappingRequestsPerMinute ?? (_options.HasApiKey ? 250 : 25)),
            [OpenFigiCallGroup.Search] = Client(inner, SearchThrottleKey, _options.SearchRequestsPerMinute ?? (_options.HasApiKey ? 20 : 5)),
        };
    }

    /// <summary>The group's pipeline: retry, then (when limiting) spacing, then the handler that sends.</summary>
    private HttpClient Client(HttpMessageHandler inner, string throttleKey, int perMinute)
    {
        HttpMessageHandler pipeline = new NotDisposing(inner);
        if (_options.LimitRequests && perMinute > 0)
            pipeline = new MinIntervalDelegatingHandler(new MinIntervalOptions { Key = throttleKey, MinInterval = TimeSpan.FromMinutes(1) / perMinute },
                                                        _options.Clock) { InnerHandler = pipeline };
        pipeline = new RetryAfterDelegatingHandler(new RetryAfterOptions
        {
            MaxAttempts = _options.MaxAttempts, BaseBackoff = _options.BaseBackoff, MaxBackoff = _options.MaxBackoff,
            RetryStatusCodes = [HttpStatusCode.TooManyRequests, HttpStatusCode.InternalServerError, HttpStatusCode.ServiceUnavailable],
        }, _options.Clock) { InnerHandler = pipeline };
        return new HttpClient(pipeline) { BaseAddress = _options.BaseAddress, Timeout = System.Threading.Timeout.InfiniteTimeSpan };
    }

    /// <summary>Sends the call through its group's pipeline and returns what the API answered, whatever its status.</summary>
    public async Task<OpenFigiResponse> SendAsync(OpenFigiRequest request, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var message = new HttpRequestMessage(request.Method, request.Path.TrimStart('/'));
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (_options.HasApiKey) message.Headers.Add("X-OPENFIGI-APIKEY", _options.ApiKey);
        if (request.JsonBody is not null) message.Content = new StringContent(request.JsonBody, Encoding.UTF8, "application/json");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(_options.Timeout);
        using var response = await _clients[request.Group].SendAsync(message, timeout.Token);
        var body = await response.Content.ReadAsStringAsync(timeout.Token);
        TimeSpan? retryAfter = response.Headers.RetryAfter switch
        {
            { Delta: { } delta } => delta,
            { Date: { } date } => date - _options.Clock.GetUtcNow(),
            _ => null,
        };
        return new OpenFigiResponse((int)response.StatusCode, body, retryAfter);
    }

    /// <summary>Passes requests to a handler the transport does not own, so disposing a pipeline leaves it open.</summary>
    private sealed class NotDisposing(HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        protected override void Dispose(bool disposing) { }
    }
}
