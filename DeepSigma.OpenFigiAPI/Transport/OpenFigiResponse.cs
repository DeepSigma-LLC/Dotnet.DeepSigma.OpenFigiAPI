namespace DeepSigma.OpenFigiAPI;

/// <summary>What came back: the HTTP status, the body as text, and a 429's <c>retry-after</c>.</summary>
public sealed record OpenFigiResponse(int StatusCode, string Body, TimeSpan? RetryAfter = null);
