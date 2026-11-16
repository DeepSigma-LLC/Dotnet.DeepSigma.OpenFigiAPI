namespace DeepSigma.OpenFigiAPI;

/// <summary>
/// The API's documented rules, checked before a request is sent so a broken job or query costs no call. Each check
/// throws an <see cref="ArgumentException"/> naming the rule. <see cref="OpenFigiClient"/> runs them on everything it
/// sends; they are public for a caller that builds requests itself.
/// </summary>
public static class OpenFigiRules
{
    /// <summary>At most this many keywords in a search or filter.</summary>
    public const int MaximumQueryTerms = 12;

    /// <summary>Checks a mapping job: its type and value, the identifier types that need securityType2, and its filters.</summary>
    public static void Check(MappingJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        if (string.IsNullOrWhiteSpace(job.IdType.Value)) throw new ArgumentException("a mapping job names its idType", nameof(job));
        if (string.IsNullOrWhiteSpace(job.IdValue)) throw new ArgumentException($"a {job.IdType} job names its idValue", nameof(job));
        if ((job.IdType == IdType.BaseTicker || job.IdType == IdType.ExchangeSymbol) && job.Filters.SecurityType2 is null)
            throw new ArgumentException($"a {job.IdType} job needs securityType2", nameof(job));
        CheckFilters(job.Filters, nameof(job));
    }

    /// <summary>Checks a search: it needs keywords, at most <see cref="MaximumQueryTerms"/>, and its filters.</summary>
    public static void CheckSearch(FigiQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (string.IsNullOrWhiteSpace(query.Query)) throw new ArgumentException("a search needs keywords", nameof(query));
        CheckFilter(query);
    }

    /// <summary>Checks a filter: keywords are optional, at most <see cref="MaximumQueryTerms"/>; and its filters.</summary>
    public static void CheckFilter(FigiQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.Query is { } text && text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length > MaximumQueryTerms)
            throw new ArgumentException($"a query may have at most {MaximumQueryTerms} terms", nameof(query));
        CheckFilters(query.Filters, nameof(query));
    }

    private static void CheckFilters(FigiFilters filters, string parameter)
    {
        if (filters.ExchangeCode is not null && filters.MicCode is not null)
            throw new ArgumentException("give exchCode or micCode, not both", parameter);
        if ((filters.SecurityType2 == SecurityType2.Option || filters.SecurityType2 == SecurityType2.Warrant) && filters.Expiration is null)
            throw new ArgumentException($"securityType2 {filters.SecurityType2} needs an expiration range", parameter);
        if (filters.SecurityType2 == SecurityType2.Pool && filters.Maturity is null)
            throw new ArgumentException("securityType2 Pool needs a maturity range", parameter);
        CheckRange(filters.Strike, "strike", parameter);
        CheckRange(filters.ContractSize, "contractSize", parameter);
        CheckRange(filters.Coupon, "coupon", parameter);
        CheckRange(filters.Expiration, "expiration", parameter);
        CheckRange(filters.Maturity, "maturity", parameter);
    }

    private static void CheckRange(NumberRange? range, string name, string parameter)
    {
        if (range is not { } r) return;
        if (r.From is null && r.To is null) throw new ArgumentException($"{name} needs at least one end", parameter);
        if (r.From > r.To) throw new ArgumentException($"{name} runs from {r.From} to {r.To}: its ends are reversed", parameter);
    }

    private static void CheckRange(DateRange? range, string name, string parameter)
    {
        if (range is not { } r) return;
        if (r.From is null && r.To is null) throw new ArgumentException($"{name} needs at least one end", parameter);
        if (r.From is { } from && r.To is { } to)
        {
            if (from > to) throw new ArgumentException($"{name} runs from {from:yyyy-MM-dd} to {to:yyyy-MM-dd}: its ends are reversed", parameter);
            if (to > from.AddYears(1)) throw new ArgumentException($"{name} may span at most a year", parameter);
        }
    }
}
