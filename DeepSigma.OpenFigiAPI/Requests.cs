namespace DeepSigma.OpenFigiAPI;

/// <summary>A range filter, <c>[From, To]</c>; one end may be open (null), not both.</summary>
public readonly record struct NumberRange(decimal? From, decimal? To);

/// <summary>A date range filter, <c>[From, To]</c>; one end may be open (null), not both; at most a year apart.</summary>
public readonly record struct DateRange(DateOnly? From, DateOnly? To);

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

/// <summary>
/// One identifier to map to FIGIs: its type, its value and what narrows it. Built directly, or by the factories for the
/// common cases (<see cref="Isin"/>, <see cref="Ticker"/>, <see cref="OccSymbol"/>, ...).
/// </summary>
public sealed record MappingJob(IdType IdType, string IdValue)
{
    /// <summary>What narrows the job.</summary>
    public FigiFilters Filters { get; init; } = FigiFilters.None;

    /// <summary>An ISIN.</summary>
    public static MappingJob Isin(string isin) => new(IdType.Isin, isin);
    /// <summary>A 9-character CUSIP.</summary>
    public static MappingJob Cusip(string cusip) => new(IdType.Cusip, cusip);
    /// <summary>A SEDOL.</summary>
    public static MappingJob Sedol(string sedol) => new(IdType.Sedol, sedol);
    /// <summary>A FIGI, to read its attributes.</summary>
    public static MappingJob Figi(string figi) => new(IdType.Figi, figi);

    /// <summary>A ticker, on an exchange (Bloomberg's code) when given. Class shares are written with a slash: <c>BRK/B</c>.</summary>
    public static MappingJob Ticker(string ticker, string? exchangeCode = null) =>
        new(IdType.Ticker, ticker) { Filters = new FigiFilters { ExchangeCode = exchangeCode } };

    /// <summary>A ticker on a venue named by its MIC.</summary>
    public static MappingJob TickerOnMic(string ticker, string micCode) =>
        new(IdType.Ticker, ticker) { Filters = new FigiFilters { MicCode = micCode } };

    /// <summary>A listed US option by its OCC symbol (<c>AAPL  260116C00200000</c>).</summary>
    public static MappingJob OccSymbol(string occSymbol) => new(IdType.OccSymbol, occSymbol);
}

/// <summary>A search or filter: keywords (required for a search, optional for a filter) and what narrows it.</summary>
public sealed record FigiQuery(string? Query)
{
    /// <summary>What narrows the search or filter.</summary>
    public FigiFilters Filters { get; init; } = FigiFilters.None;
}
