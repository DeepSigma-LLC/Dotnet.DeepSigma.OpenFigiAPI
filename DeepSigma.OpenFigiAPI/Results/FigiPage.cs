namespace DeepSigma.OpenFigiAPI;

/// <summary>One page of a search or filter: its records, the token of the next page (null on the last), and for a
/// filter how many results there are in all.</summary>
public sealed record FigiPage(IReadOnlyList<FigiRecord> Records, string? Next, long? Total);
