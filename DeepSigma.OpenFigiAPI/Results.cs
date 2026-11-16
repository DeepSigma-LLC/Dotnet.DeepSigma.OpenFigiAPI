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

/// <summary>The job's identifier matched: one record, or several (one per exchange, for instance).</summary>
public sealed record MappingFound(IReadOnlyList<FigiRecord> Records);

/// <summary>Nothing matched the job (the API's <c>warning</c>, or the <c>error</c> "No identifier found.").</summary>
public sealed record MappingNotFound(string Message);

/// <summary>The job failed (an identifier in the wrong format, more than 15,000 results, an error on the API's side).</summary>
public sealed record MappingFailed(string Message);

/// <summary>What one mapping job came to.</summary>
public union MappingResult(MappingFound, MappingNotFound, MappingFailed);

/// <summary>One page of a search or filter: its records, the token of the next page (null on the last), and for a
/// filter how many results there are in all.</summary>
public sealed record FigiPage(IReadOnlyList<FigiRecord> Records, string? Next, long? Total);

/// <summary>
/// A request the API refused or could not answer: its HTTP status (or 200 for an error inside a search's response), the
/// API's message, and, for 429, how long it asked to wait.
/// </summary>
public sealed class OpenFigiException(int statusCode, string message, TimeSpan? retryAfter = null, Exception? inner = null)
    : Exception(message, inner)
{
    /// <summary>The HTTP status; 200 for an error inside a successful response.</summary>
    public int StatusCode { get; } = statusCode;
    /// <summary>How long a 429 asked to wait, when it said.</summary>
    public TimeSpan? RetryAfter { get; } = retryAfter;
}
