namespace DeepSigma.OpenFigiAPI;

/// <summary>
/// How a call travels. <see cref="HttpOpenFigiTransport"/> sends it with an <see cref="HttpClient"/>, keeping to the API's
/// limits; a program with its own gateway for vendor calls (quotas, retries, an archive of responses) implements this to
/// send it there instead, and keeps the rest of the package: the requests, the rules and the parsers. A transport returns
/// what the API answered, whatever the status; <see cref="OpenFigiClient"/> decides what a status means.
/// </summary>
public interface IOpenFigiTransport
{
    /// <summary>Whether calls carry an API key: the API then takes 100 jobs a mapping request instead of 10, so the client
    /// sends batches of that size.</summary>
    bool SendsApiKey { get; }

    /// <summary>Sends the call and returns what the API answered, whatever its status.</summary>
    Task<OpenFigiResponse> SendAsync(OpenFigiRequest request, CancellationToken cancellation = default);
}
