namespace DeepSigma.OpenFigiAPI;

/// <summary>The job failed (an identifier in the wrong format, more than 15,000 results, an error on the API's side).</summary>
public sealed record MappingFailed(string Message);
