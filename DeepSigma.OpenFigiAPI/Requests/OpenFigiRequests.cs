using System.Globalization;
using System.Text.Json.Nodes;

namespace DeepSigma.OpenFigiAPI;

/// <summary>
/// The request bodies as the API takes them, written without sending anything: for <see cref="OpenFigiClient"/>, and for
/// a caller that sends requests its own way. The rules (<see cref="OpenFigiRules"/>) are checked first.
/// </summary>
public static class OpenFigiRequests
{
    /// <summary>The body of <c>POST /v3/mapping</c>: a JSON array with one object per job.</summary>
    public static string MappingBody(IReadOnlyList<MappingJob> jobs)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        var array = new JsonArray();
        foreach (var job in jobs) array.Add(JobObject(job));
        return array.ToJsonString();
    }

    /// <summary>One job as the mapping body carries it (<c>{"idType": ..., "idValue": ..., "micCode": ...}</c>): to store
    /// or log a job, or to key a cache by it. <see cref="OpenFigiParser.ParseMappingJob(string)"/> reads it back.</summary>
    public static string MappingJobJson(MappingJob job) => JobObject(job).ToJsonString();

    private static JsonObject JobObject(MappingJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        OpenFigiRules.Check(job);
        var item = new JsonObject { ["idType"] = job.IdType.Value, ["idValue"] = job.IdValue };
        WriteFilters(item, job.Filters);
        return item;
    }

    /// <summary>The body of <c>POST /v3/search</c>, from the page <paramref name="start"/> names (the previous page's
    /// <c>next</c>; null for the first). A search needs keywords.</summary>
    public static string SearchBody(FigiQuery query, string? start = null)
    {
        OpenFigiRules.CheckSearch(query);
        return QueryBody(query, start);
    }

    /// <summary>The body of <c>POST /v3/filter</c>, from the page <paramref name="start"/> names. A filter may have no
    /// keywords: its filters alone.</summary>
    public static string FilterBody(FigiQuery query, string? start = null)
    {
        OpenFigiRules.CheckFilter(query);
        return QueryBody(query, start);
    }

    private static string QueryBody(FigiQuery query, string? start)
    {
        var item = new JsonObject();
        if (!string.IsNullOrWhiteSpace(query.Query)) item["query"] = query.Query;
        if (start is not null) item["start"] = start;
        WriteFilters(item, query.Filters);
        return item.ToJsonString();
    }

    private static void WriteFilters(JsonObject item, FigiFilters filters)
    {
        Put(item, "exchCode", filters.ExchangeCode);
        Put(item, "micCode", filters.MicCode);
        Put(item, "currency", filters.Currency);
        Put(item, "marketSecDes", filters.MarketSector?.Value);
        Put(item, "securityType", filters.SecurityType);
        Put(item, "securityType2", filters.SecurityType2?.Value);
        if (filters.IncludeUnlistedEquities is { } unlisted) item["includeUnlistedEquities"] = unlisted;
        Put(item, "optionType", filters.OptionType?.Value);
        Put(item, "strike", filters.Strike);
        Put(item, "contractSize", filters.ContractSize);
        Put(item, "coupon", filters.Coupon);
        Put(item, "expiration", filters.Expiration);
        Put(item, "maturity", filters.Maturity);
        Put(item, "stateCode", filters.StateCode);
    }

    private static void Put(JsonObject item, string name, string? value)
    {
        if (value is not null) item[name] = value;
    }

    private static void Put(JsonObject item, string name, NumberRange? range)
    {
        if (range is { } r) item[name] = new JsonArray(r.From is { } from ? JsonValue.Create(from) : null, r.To is { } to ? JsonValue.Create(to) : null);
    }

    private static void Put(JsonObject item, string name, DateRange? range)
    {
        if (range is { } r)
            item[name] = new JsonArray(r.From is { } from ? JsonValue.Create(from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) : null,
                                       r.To is { } to ? JsonValue.Create(to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) : null);
    }
}
