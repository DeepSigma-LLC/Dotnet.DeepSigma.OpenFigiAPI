using System.Text.Json;
using System.Text.Json.Nodes;

namespace DeepSigma.OpenFigiAPI;

/// <summary>
/// Mapping answers written back in the API's own shape, the reverse of <see cref="OpenFigiParser"/>: to store an answer
/// as it was received (a cache, an archive) and read it later with the same parser as a live one. A record keeps the
/// fields it came with (<see cref="FigiRecord.Raw"/>); one built in code is written from its properties.
/// </summary>
public static class OpenFigiResponses
{
    /// <summary>One element of a mapping response: <c>{"data": [...]}</c>, <c>{"warning": "..."}</c> for nothing found,
    /// <c>{"error": "..."}</c> for a failed job. <see cref="OpenFigiParser.ParseMappingResult"/> reads it back as the same
    /// result.</summary>
    public static string MappingResultJson(MappingResult result) => ResultObject(result).ToJsonString();

    /// <summary>A whole mapping response, one element per result, in order (<see cref="OpenFigiParser.ParseMapping"/>).</summary>
    public static string MappingResponseJson(IReadOnlyList<MappingResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        var array = new JsonArray();
        foreach (var result in results) array.Add(ResultObject(result));
        return array.ToJsonString();
    }

    private static JsonObject ResultObject(MappingResult result) => result switch
    {
        MappingFound found => new JsonObject { ["data"] = new JsonArray([.. found.Records.Select(RecordNode)]) },
        MappingNotFound notFound => new JsonObject { ["warning"] = notFound.Message },
        MappingFailed failed => new JsonObject { ["error"] = failed.Message },
    };

    private static JsonNode RecordNode(FigiRecord record)
    {
        if (record.Raw.ValueKind == JsonValueKind.Object) return JsonNode.Parse(record.Raw.GetRawText())!;
        var item = new JsonObject { ["figi"] = record.Figi };
        void Put(string name, string? value)
        {
            if (value is not null) item[name] = value;
        }
        Put("name", record.Name);
        Put("ticker", record.Ticker);
        Put("exchCode", record.ExchangeCode);
        Put("marketSector", record.MarketSector);
        Put("securityType", record.SecurityType);
        Put("securityType2", record.SecurityType2);
        Put("shareClassFIGI", record.ShareClassFigi);
        Put("compositeFIGI", record.CompositeFigi);
        Put("securityDescription", record.SecurityDescription);
        Put("metadata", record.Metadata);
        return item;
    }
}
