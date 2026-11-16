using System.Net;
using System.Net.Http.Headers;
using System.Text;
using DeepSigma.DataAccess.Web.Throttling;

namespace DeepSigma.OpenFigiAPI;

/// <summary>
/// The package's own transport, on the DeepSigma web handlers (<c>DeepSigma.DataAccess.Web</c>): each call is sent with
/// an <see cref="HttpClient"/> whose pipeline retries a 429 (waiting for its <c>retry-after</c>), a 500 or a 503 with
/// capped exponential backoff (<see cref="RetryAfterDelegatingHandler"/>), and spaces requests to keep within the API's
/// limit for the call's group (<see cref="MinIntervalDelegatingHandler"/>; every attempt is spaced, retries included).
/// The spacing is shared by every client in the process, keyed by group, since the API counts by key or address, not by
/// client. Other statuses are returned at once, never retried. Safe to share across threads; dispose it when done, which
/// closes the connections it opened (a handler passed in is left to its owner).
/// </summary>
public sealed class HttpOpenFigiTransport : IOpenFigiTransport, IDisposable
{
    /// <summary>The throttle keys the spacing is shared under, per group.</summary>
    public const string MappingThrottleKey = "openfigi:mapping", SearchThrottleKey = "openfigi:search";

    private readonly OpenFigiOptions _options;
    private readonly Dictionary<OpenFigiCallGroup, HttpClient> _clients;
    private readonly HttpMessageHandler? _ownedHandler;

    /// <param name="options">How to call the API; the defaults when null (no key, the keyless limits).</param>
    /// <param name="handler">The handler that sends the request at the end of the pipeline; a
    /// <see cref="SocketsHttpHandler"/> when null. A test passes a fake one. It is not disposed by the transport.</param>
    public HttpOpenFigiTransport(OpenFigiOptions? options = null, HttpMessageHandler? handler = null)
    {
        _options = options ?? new OpenFigiOptions();
        _ownedHandler = handler is null ? new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) } : null;
        var inner = handler ?? _ownedHandler!;
        _clients = new()
        {
            [OpenFigiCallGroup.Mapping] = Client(inner, MappingThrottleKey, _options.MappingRequestsPerMinute ?? (_options.HasApiKey ? 250 : 25)),
            [OpenFigiCallGroup.Search] = Client(inner, SearchThrottleKey, _options.SearchRequestsPerMinute ?? (_options.HasApiKey ? 20 : 5)),
        };
    }

    /// <inheritdoc/>
    public bool SendsApiKey => _options.HasApiKey;

    /// <summary>The group's pipeline (<see cref="HttpPipeline"/>): retry, then (when limiting) spacing, then the handler
    /// that sends, which the group's client shares with the other group and never disposes.</summary>
    private HttpClient Client(HttpMessageHandler inner, string throttleKey, int perMinute) =>
        HttpPipeline.CreateClient(inner, new HttpPipelineOptions
        {
            Retry = new RetryAfterOptions
            {
                MaxAttempts = _options.MaxAttempts, BaseBackoff = _options.BaseBackoff, MaxBackoff = _options.MaxBackoff,
                RetryStatusCodes = [HttpStatusCode.TooManyRequests, HttpStatusCode.InternalServerError, HttpStatusCode.ServiceUnavailable],
            },
            Throttle = _options.LimitRequests && perMinute > 0
                ? new MinIntervalOptions { Key = throttleKey, MinInterval = TimeSpan.FromMinutes(1) / perMinute }
                : null,
            Clock = _options.Clock,
            BaseAddress = _options.BaseAddress,
            Timeout = System.Threading.Timeout.InfiniteTimeSpan,                // the call's own timeout covers every attempt
        }, disposeHandler: false);

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
        return new OpenFigiResponse((int)response.StatusCode, body, RetryAfterHeader.GetDelay(response, _options.Clock));
    }

    /// <summary>Disposes the pipelines and the handler the transport made; one passed in is its owner's.</summary>
    public void Dispose()
    {
        foreach (var client in _clients.Values) client.Dispose();
        _ownedHandler?.Dispose();
    }
}
