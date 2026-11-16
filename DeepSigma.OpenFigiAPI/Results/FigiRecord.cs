using System.Text.Json;

namespace DeepSigma.OpenFigiAPI;

/// <summary>
/// One FIGI and what the API says about it. Any field may be null (the API sends null, or <see cref="Metadata"/> says
/// attributes are not available). <see cref="Raw"/> keeps the record as received, so a field the API adds later is not
/// lost.
/// </summary>
public sealed record FigiRecord
{
    /// <summary>The FIGI of this instrument on this exchange.</summary>
    public required string Figi { get; init; }
    /// <summary>The instrument's name, as the API abbreviates it (<c>APPLE INC</c>).</summary>
    public string? Name { get; init; }
    /// <summary>The ticker on this exchange.</summary>
    public string? Ticker { get; init; }
    /// <summary>Bloomberg's exchange code (<c>US</c>, <c>UN</c>, <c>LN</c>).</summary>
    public string? ExchangeCode { get; init; }
    /// <summary>The market sector (<c>Equity</c>, <c>Corp</c>).</summary>
    public string? MarketSector { get; init; }
    /// <summary>The broad security type (<c>Common Stock</c>).</summary>
    public string? SecurityType { get; init; }
    /// <summary>The detailed security type.</summary>
    public string? SecurityType2 { get; init; }
    /// <summary>The FIGI of the share class, across countries.</summary>
    public string? ShareClassFigi { get; init; }
    /// <summary>The FIGI of the instrument across the exchanges of one country.</summary>
    public string? CompositeFigi { get; init; }
    /// <summary>A short description (for a bond, its ticker, coupon and maturity).</summary>
    public string? SecurityDescription { get; init; }
    /// <summary>The API's note when attributes are unavailable (<c>Metadata N/A</c>).</summary>
    public string? Metadata { get; init; }
    /// <summary>The record as received.</summary>
    public JsonElement Raw { get; init; }
}
