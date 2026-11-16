namespace DeepSigma.OpenFigiAPI;

/// <summary>A date range filter, <c>[From, To]</c>; one end may be open (null), not both; at most a year apart.</summary>
public readonly record struct DateRange(DateOnly? From, DateOnly? To);
