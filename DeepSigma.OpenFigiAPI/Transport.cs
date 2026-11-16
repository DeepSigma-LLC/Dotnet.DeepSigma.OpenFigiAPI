namespace DeepSigma.OpenFigiAPI;

/// <summary>Which of the API's limits a call counts against: mapping (and the field values), or search and filter.</summary>
public enum OpenFigiCallGroup
{
    /// <summary><c>/v3/mapping</c> and <c>/v3/mapping/values</c>.</summary>
    Mapping,
    /// <summary><c>/v3/search</c> and <c>/v3/filter</c>.</summary>
    Search,
}

/// <summary>One call to the API: its method, path (<c>/v3/mapping</c>), JSON body (null for a GET) and limit group.</summary>
public sealed record OpenFigiRequest(HttpMethod Method, string Path, string? JsonBody, OpenFigiCallGroup Group);

/// <summary>What came back: the HTTP status, the body as text, and a 429's <c>retry-after</c>.</summary>
public sealed record OpenFigiResponse(int StatusCode, string Body, TimeSpan? RetryAfter = null);

/// <summary>
/// How a call travels. <see cref="HttpOpenFigiTransport"/> sends it with an <see cref="HttpClient"/>, keeping to the API's
/// limits; a program with its own gateway for vendor calls (quotas, retries, an archive of responses) implements this to
/// send it there instead, and keeps the rest of the package: the requests, the rules and the parsers. A transport returns
/// what the API answered, whatever the status; <see cref="OpenFigiClient"/> decides what a status means.
/// </summary>
public interface IOpenFigiTransport
{
    /// <summary>Sends the call and returns what the API answered, whatever its status.</summary>
    Task<OpenFigiResponse> SendAsync(OpenFigiRequest request, CancellationToken cancellation = default);
}
