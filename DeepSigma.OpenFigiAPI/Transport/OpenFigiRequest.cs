namespace DeepSigma.OpenFigiAPI;

/// <summary>One call to the API: its method, path (<c>/v3/mapping</c>), JSON body (null for a GET) and limit group.</summary>
public sealed record OpenFigiRequest(HttpMethod Method, string Path, string? JsonBody, OpenFigiCallGroup Group);
