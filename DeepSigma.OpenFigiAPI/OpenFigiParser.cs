using System.Text.Json;

namespace DeepSigma.OpenFigiAPI;

/// <summary>
/// Every response of the API read into this package's types, with no HTTP and no client: a saved response is read with
/// the same code as a live one, and a change to deserialization is tested on saved responses without a web request.
/// <see cref="OpenFigiClient"/> reads every response it receives through these methods.
/// </summary>
public static class OpenFigiParser
{
    /// <summary>The API's message when a job matched nothing; documented as a <c>warning</c>, shown in its own example
    /// as an <c>error</c>. Either is <see cref="MappingNotFound"/>.</summary>
    public const string NoIdentifierFound = "No identifier found.";

    /// <summary>A <c>/v3/mapping</c> response: one result per job, in the jobs' order. A response whose count differs from
    /// <paramref name="jobCount"/> (when given) is refused, since the results could not be matched to their jobs.</summary>
    public static IReadOnlyList<MappingResult> ParseMapping(string json, int? jobCount = null)
    {
        using var document = Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new OpenFigiException(200, $"a mapping response is a JSON array, not {document.RootElement.ValueKind}");
        var results = document.RootElement.EnumerateArray().Select(element => ParseMappingResult(element.Clone())).ToList();
        if (jobCount is { } expected && results.Count != expected)
            throw new OpenFigiException(200, $"the mapping response has {results.Count} results for {expected} jobs");
        return results;
    }

    /// <summary>One element of a mapping response: found (its records), not found, or failed.</summary>
    public static MappingResult ParseMappingResult(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) return new MappingFailed($"a mapping result is a JSON object, not {element.ValueKind}");
        if (element.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            return new MappingFound(data.EnumerateArray().Select(ParseRecord).ToList());
        if (Text(element, "warning") is { } warning) return new MappingNotFound(warning);
        if (Text(element, "error") is { } error)
            return string.Equals(error.Trim(), NoIdentifierFound, StringComparison.OrdinalIgnoreCase) ? new MappingNotFound(error) : new MappingFailed(error);
        return new MappingFailed("a mapping result has no data, warning or error");
    }

    /// <summary>A <c>/v3/search</c> response: a page of records and the next page's token. An <c>error</c> in it throws.</summary>
    public static FigiPage ParseSearchPage(string json) => ParsePage(json, "search");

    /// <summary>A <c>/v3/filter</c> response: a page of records, the next page's token and the total.</summary>
    public static FigiPage ParseFilterPage(string json) => ParsePage(json, "filter");

    /// <summary>A <c>/v3/mapping/values/{key}</c> response: the field's valid values.</summary>
    public static IReadOnlyList<string> ParseMappingValues(string json)
    {
        using var document = Parse(json);
        var root = document.RootElement;
        if (Text(root, "error") is { } error) throw new OpenFigiException(200, error);
        if (!root.TryGetProperty("values", out var values) || values.ValueKind != JsonValueKind.Array)
            throw new OpenFigiException(200, Text(root, "warning") ?? "a values response has no values");
        return values.EnumerateArray().Select(value => value.ValueKind == JsonValueKind.String ? value.GetString()! : value.GetRawText()).ToList();
    }

    /// <summary>One record of a mapping, search or filter response.</summary>
    public static FigiRecord ParseRecord(JsonElement element) => new()
    {
        Figi = Text(element, "figi") ?? throw new OpenFigiException(200, "a record has no figi"),
        Name = Text(element, "name"),
        Ticker = Text(element, "ticker"),
        ExchangeCode = Text(element, "exchCode"),
        MarketSector = Text(element, "marketSector"),
        SecurityType = Text(element, "securityType"),
        SecurityType2 = Text(element, "securityType2"),
        ShareClassFigi = Text(element, "shareClassFIGI"),
        CompositeFigi = Text(element, "compositeFIGI"),
        SecurityDescription = Text(element, "securityDescription"),
        Metadata = Text(element, "metadata"),
        Raw = element.Clone(),
    };

    /// <summary>A refused request's body (plain text, or JSON with an <c>error</c>) as the exception the client throws;
    /// <paramref name="retryAfter"/> is a 429's <c>retry-after</c>.</summary>
    public static OpenFigiException ParseError(int statusCode, string? body, TimeSpan? retryAfter = null)
    {
        var message = body?.Trim() ?? "";
        if (message.StartsWith('{'))
        {
            try
            {
                using var document = JsonDocument.Parse(message);
                message = Text(document.RootElement, "error") ?? Text(document.RootElement, "message") ?? message;
            }
            catch (JsonException) { }
        }
        var meaning = statusCode switch
        {
            400 => "the request is malformed",
            401 => "the API key is not valid",
            404 => "no such address",
            413 => "too many jobs in one request",
            415 => "the body is not JSON",
            429 => "the rate limit was reached",
            500 or 503 => "the API failed or is down for maintenance",
            _ => "the request failed",
        };
        return new OpenFigiException(statusCode, message.Length == 0 ? $"OpenFIGI: {meaning} (HTTP {statusCode})" : $"OpenFIGI: {meaning} (HTTP {statusCode}): {message}",
                                     retryAfter);
    }

    private static FigiPage ParsePage(string json, string call)
    {
        using var document = Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new OpenFigiException(200, $"a {call} response is a JSON object, not {root.ValueKind}");
        if (Text(root, "error") is { } error) throw new OpenFigiException(200, $"OpenFIGI {call}: {error}");
        var records = root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array
            ? data.EnumerateArray().Select(ParseRecord).ToList()
            : [];
        long? total = root.TryGetProperty("total", out var t) && t.ValueKind == JsonValueKind.Number ? t.GetInt64() : null;
        return new FigiPage(records, Text(root, "next"), total);
    }

    private static JsonDocument Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new OpenFigiException(200, $"the response is not JSON: {exception.Message}", inner: exception);
        }
    }

    private static string? Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
