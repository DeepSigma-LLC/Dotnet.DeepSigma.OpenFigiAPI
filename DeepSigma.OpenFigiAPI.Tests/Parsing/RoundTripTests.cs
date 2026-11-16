using System.Text.Json;
using Xunit;

namespace DeepSigma.OpenFigiAPI.Tests;

/// <summary>What the package writes, it reads back as the same thing, with no web request: a mapping job
/// (<see cref="OpenFigiRequests.MappingJobJson"/>, <see cref="OpenFigiParser.ParseMappingJob(string)"/>) and mapping answers
/// (<see cref="OpenFigiResponses"/>, <see cref="OpenFigiParser.ParseMapping"/>), the answers taken from saved responses.</summary>
public sealed class RoundTripTests
{
    private static readonly MappingJob EveryFilter = new(IdType.BaseTicker, "AAPL")
    {
        Filters = new FigiFilters
        {
            ExchangeCode = "US", Currency = "USD", MarketSector = MarketSector.Equity, SecurityType = "Equity Option",
            SecurityType2 = SecurityType2.Option, IncludeUnlistedEquities = false, OptionType = OptionType.Call,
            Strike = new NumberRange(150.5m, null), ContractSize = new NumberRange(100m, 100m), Coupon = new NumberRange(null, 5m),
            Expiration = new DateRange(new DateOnly(2026, 12, 1), new DateOnly(2027, 6, 30)), Maturity = new DateRange(null, new DateOnly(2027, 1, 1)),
            StateCode = "NY",
        },
    };

    [Fact]
    public void AJobIsReadBackAsTheSameJob()
    {
        MappingJob[] jobs = [MappingJob.Isin("US0378331005"), MappingJob.TickerOnMic("BRK/B", "XNYS"), EveryFilter];
        foreach (var job in jobs)
            Assert.Equal(job, OpenFigiParser.ParseMappingJob(OpenFigiRequests.MappingJobJson(job)));

        // one job's JSON is its element of the mapping body
        var body = JsonDocument.Parse(OpenFigiRequests.MappingBody(jobs)).RootElement;
        Assert.Equal([.. jobs.Select(OpenFigiRequests.MappingJobJson)], body.EnumerateArray().Select(element => element.GetRawText()));
        Assert.Equal(jobs, body.EnumerateArray().Select(OpenFigiParser.ParseMappingJob));
        Assert.Equal("""{"idType":"TICKER","idValue":"BRK/B","micCode":"XNYS"}""", OpenFigiRequests.MappingJobJson(jobs[1]));
    }

    [Fact]
    public void AJobWithAFieldTheAPIDoesNotHaveIsRefused()
    {
        void Refused(string json, string why) =>
            Assert.Contains(why, Assert.Throws<OpenFigiException>(() => OpenFigiParser.ParseMappingJob(json)).Message);

        Refused("""{"idType":"TICKER","idValue":"IBM","marketSector":"Equity"}""", "no field marketSector");   // the API's name is marketSecDes
        Refused("""{"idValue":"IBM"}""", "no idType");
        Refused("""{"idType":"TICKER","idValue":"IBM","strike":[1]}""", "pair");
        Refused("""{"idType":"TICKER","idValue":"IBM","expiration":["2026-13-01",null]}""", "a date");
        Refused("[]", "a JSON object");
        Assert.Throws<ArgumentException>(() => OpenFigiRequests.MappingJobJson(new MappingJob(IdType.Isin, " ")));   // the rules, as for a body
    }

    [Theory]
    [InlineData("mapping_isin_ticker_invalid.json")]
    [InlineData("mapping_not_found_and_cusip.json")]
    [InlineData("mapping_documented_shapes.json")]
    public void SavedAnswersAreWrittenBackAndReadAsTheSameResults(string saved)
    {
        var results = OpenFigiParser.ParseMapping(TestData.Read(saved));
        var again = OpenFigiParser.ParseMapping(OpenFigiResponses.MappingResponseJson(results));
        Assert.Equal(results.Count, again.Count);
        foreach (var (first, second) in results.Zip(again))
            Assert.Equal(Describe(first), Describe(second));
        // one result alone, as a cache keeps it
        Assert.Equal(Describe(results[0]), Describe(OpenFigiParser.ParseMappingResult(JsonDocument.Parse(OpenFigiResponses.MappingResultJson(results[0])).RootElement)));
    }

    [Fact]
    public void ARecordKeepsTheFieldsItCameWithAndOneBuiltInCodeIsWrittenFromItsProperties()
    {
        // a field the package does not know survives the round trip, through the record as received
        var received = OpenFigiParser.ParseMapping("""[{"data":[{"figi":"BBG000BLNNH6","ticker":"IBM","newField":"kept"}]}]""")[0];
        var written = JsonDocument.Parse(OpenFigiResponses.MappingResultJson(received)).RootElement;
        Assert.Equal("kept", written.GetProperty("data")[0].GetProperty("newField").GetString());

        var built = new MappingFound([new FigiRecord { Figi = "BBG000BLNNH6", Ticker = "IBM", ShareClassFigi = "BBG001S5S399", SecurityType = "Common Stock" }]);
        Assert.Equal("""{"data":[{"figi":"BBG000BLNNH6","ticker":"IBM","securityType":"Common Stock","shareClassFIGI":"BBG001S5S399"}]}""",
                     OpenFigiResponses.MappingResultJson(built));
        var back = Assert.Single(MappingResults.Found(OpenFigiParser.ParseMappingResult(JsonDocument.Parse(OpenFigiResponses.MappingResultJson(built)).RootElement)).Records);
        Assert.Equal(("BBG000BLNNH6", "IBM", "BBG001S5S399", "Common Stock"), (back.Figi, back.Ticker, back.ShareClassFigi, back.SecurityType));

        Assert.Equal("""{"warning":"No identifier found."}""", OpenFigiResponses.MappingResultJson(new MappingNotFound(OpenFigiParser.NoIdentifierFound)));
        Assert.Equal("""{"error":"Invalid idValue format."}""", OpenFigiResponses.MappingResultJson(new MappingFailed("Invalid idValue format.")));
    }

    /// <summary>A result as text: its case, and its records as received (compact) or its message.</summary>
    private static string Describe(MappingResult result) => result switch
    {
        MappingFound found => "found " + string.Join("|", found.Records.Select(record => JsonSerializer.Serialize(record.Raw))),
        MappingNotFound notFound => "not found " + notFound.Message,
        MappingFailed failed => "failed " + failed.Message,
    };
}
