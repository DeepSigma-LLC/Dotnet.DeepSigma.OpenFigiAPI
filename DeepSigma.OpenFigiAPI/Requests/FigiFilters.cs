namespace DeepSigma.OpenFigiAPI;

/// <summary>
/// What narrows a mapping job, a search or a filter: the API's optional fields. Give an exchange code or a MIC, not both.
/// <see cref="SecurityType2"/> is required with <see cref="IdType.BaseTicker"/> and <see cref="IdType.ExchangeSymbol"/>;
/// <see cref="Expiration"/> with options and warrants; <see cref="Maturity"/> with pools (<see cref="OpenFigiRules"/>).
/// </summary>
public sealed record FigiFilters
{
    /// <summary>Bloomberg's exchange code (<c>US</c>, <c>LN</c>).</summary>
    public string? ExchangeCode { get; init; }
    /// <summary>The ISO 10383 market identifier code (<c>XNYS</c>).</summary>
    public string? MicCode { get; init; }
    /// <summary>The ISO 4217 currency (<c>USD</c>).</summary>
    public string? Currency { get; init; }
    /// <summary>The market sector (<c>marketSecDes</c>).</summary>
    public MarketSector? MarketSector { get; init; }
    /// <summary>The broad security type (<c>Common Stock</c>, <c>Equity Option</c>); free text as the API lists it.</summary>
    public string? SecurityType { get; init; }
    /// <summary>The detailed security type; options and warrants then need <see cref="Expiration"/>, pools <see cref="Maturity"/>.</summary>
    public SecurityType2? SecurityType2 { get; init; }
    /// <summary>Also unlisted and delisted equities; the API leaves them out by default.</summary>
    public bool? IncludeUnlistedEquities { get; init; }
    /// <summary>An option's right: call or put.</summary>
    public OptionType? OptionType { get; init; }
    /// <summary>An option's strike.</summary>
    public NumberRange? Strike { get; init; }
    /// <summary>A derivative's contract size.</summary>
    public NumberRange? ContractSize { get; init; }
    /// <summary>A bond's coupon, in percent.</summary>
    public NumberRange? Coupon { get; init; }
    /// <summary>An option's or warrant's expiry.</summary>
    public DateRange? Expiration { get; init; }
    /// <summary>A bond's or pool's maturity.</summary>
    public DateRange? Maturity { get; init; }
    /// <summary>A US state's code, for municipal securities.</summary>
    public string? StateCode { get; init; }

    /// <summary>No filters.</summary>
    public static readonly FigiFilters None = new();
}
