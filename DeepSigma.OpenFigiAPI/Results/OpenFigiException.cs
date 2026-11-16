namespace DeepSigma.OpenFigiAPI;

/// <summary>
/// A request the API refused (its HTTP status, the API's message and, for 429, how long it asked to wait), or a response
/// that came back but reports an error or cannot be read (no status).
/// </summary>
public sealed class OpenFigiException(int? statusCode, string message, TimeSpan? retryAfter = null, Exception? inner = null)
    : Exception(message, inner)
{
    /// <summary>The HTTP status of a refused request; null when the response came back but its content is an error, or not
    /// what the API documents.</summary>
    public int? StatusCode { get; } = statusCode;
    /// <summary>How long a 429 asked to wait, when it said.</summary>
    public TimeSpan? RetryAfter { get; } = retryAfter;
}
