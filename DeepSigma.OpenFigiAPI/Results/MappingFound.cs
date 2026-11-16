namespace DeepSigma.OpenFigiAPI;

/// <summary>The job's identifier matched: one record, or several (one per exchange, for instance).</summary>
public sealed record MappingFound(IReadOnlyList<FigiRecord> Records);
