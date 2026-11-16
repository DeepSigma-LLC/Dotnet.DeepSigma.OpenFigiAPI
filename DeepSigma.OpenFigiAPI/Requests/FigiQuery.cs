namespace DeepSigma.OpenFigiAPI;

/// <summary>A search or filter: keywords (required for a search, optional for a filter) and what narrows it.</summary>
public sealed record FigiQuery(string? Query)
{
    /// <summary>What narrows the search or filter.</summary>
    public FigiFilters Filters { get; init; } = FigiFilters.None;
}
