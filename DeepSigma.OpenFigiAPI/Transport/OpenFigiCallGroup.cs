namespace DeepSigma.OpenFigiAPI;

/// <summary>Which of the API's limits a call counts against: mapping (and the field values), or search and filter.</summary>
public enum OpenFigiCallGroup
{
    /// <summary><c>/v3/mapping</c> and <c>/v3/mapping/values</c>.</summary>
    Mapping,
    /// <summary><c>/v3/search</c> and <c>/v3/filter</c>.</summary>
    Search,
}
