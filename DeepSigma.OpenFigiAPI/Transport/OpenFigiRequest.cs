namespace DeepSigma.OpenFigiAPI;

/// <summary>One call to the API: its method, path (<c>/v3/mapping</c>), JSON body (null for a GET) and limit group.
/// <see cref="OpenFigiClient"/> also names the call (<see cref="Call"/>) and, for a field's values, the field
/// (<see cref="Field"/>), so a transport that routes calls by name need not read the path.</summary>
public sealed record OpenFigiRequest(HttpMethod Method, string Path, string? JsonBody, OpenFigiCallGroup Group)
{
    /// <summary>Which call this is; null for a request built outside the client that did not say.</summary>
    public OpenFigiCall? Call { get; init; }

    /// <summary>The mapping field whose values a <see cref="OpenFigiCall.MappingValues"/> call asks for; null otherwise.</summary>
    public MappingField? Field { get; init; }
}
