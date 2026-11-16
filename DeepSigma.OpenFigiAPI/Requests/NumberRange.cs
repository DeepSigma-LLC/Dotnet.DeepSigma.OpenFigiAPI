namespace DeepSigma.OpenFigiAPI;

/// <summary>A range filter, <c>[From, To]</c>; one end may be open (null), not both.</summary>
public readonly record struct NumberRange(decimal? From, decimal? To);
