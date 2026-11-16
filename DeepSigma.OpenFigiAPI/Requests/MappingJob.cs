namespace DeepSigma.OpenFigiAPI;

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
