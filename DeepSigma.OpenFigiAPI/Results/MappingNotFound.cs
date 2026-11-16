namespace DeepSigma.OpenFigiAPI;

/// <summary>Nothing matched the job (the API's <c>warning</c>, or the <c>error</c> "No identifier found.").</summary>
public sealed record MappingNotFound(string Message);
