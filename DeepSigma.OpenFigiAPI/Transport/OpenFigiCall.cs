namespace DeepSigma.OpenFigiAPI;

/// <summary>Which of the API's calls a request is: for a transport that sends each call its own way (a gateway with an
/// endpoint per call) without reading the path.</summary>
public enum OpenFigiCall
{
    /// <summary><c>POST /v3/mapping</c>.</summary>
    Mapping,
    /// <summary><c>GET /v3/mapping/values/{field}</c>; the field is <see cref="OpenFigiRequest.Field"/>.</summary>
    MappingValues,
    /// <summary><c>POST /v3/search</c>.</summary>
    Search,
    /// <summary><c>POST /v3/filter</c>.</summary>
    Filter,
}
