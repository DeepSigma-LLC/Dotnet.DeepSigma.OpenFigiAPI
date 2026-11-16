using Xunit;

namespace DeepSigma.OpenFigiAPI.Tests;

/// <summary>
/// Every parser on saved responses, with no web request: real responses recorded from the API on 2026-11-15
/// (<c>mapping_*</c>, <c>search_page_with_next</c>, <c>filter_page_with_total</c>, <c>values_exch_code</c>, the error
/// bodies) and cut-down ones for the shapes a live call rarely shows (<c>mapping_documented_shapes</c>,
/// <c>search_last_page</c>, <c>search_error</c>).
/// </summary>
public sealed class ParserTests
{
    [Fact]
    public void AMappingResponseHasOneResultPerJobInOrder()
    {
        // Apple by ISIN (one record per exchange), IBM by ticker on US, and an ISIN in the wrong format
        var results = OpenFigiParser.ParseMapping(TestData.Read("mapping_isin_ticker_invalid.json"), jobCount: 3);

        var apple = MappingResults.Found(results[0]);
        Assert.True(apple.Records.Count > 100);
        var composite = Assert.Single(apple.Records, record => record.ExchangeCode == "US");
        Assert.Equal(("BBG000B9XRY4", "AAPL", "APPLE INC", "BBG000B9XRY4", "BBG001S5N8V8", "Common Stock", "Equity"),
                     (composite.Figi, composite.Ticker, composite.Name, composite.CompositeFigi, composite.ShareClassFigi, composite.SecurityType2,
                      composite.MarketSector));
        Assert.Equal("BBG000B9XRY4", composite.Raw.GetProperty("figi").GetString());
        Assert.All(apple.Records, record => Assert.Equal("BBG001S5N8V8", record.ShareClassFigi));   // one share class; a composite per country

        var ibm = Assert.Single(MappingResults.Found(results[1]).Records);
        Assert.Equal(("IBM", "US"), (ibm.Ticker, ibm.ExchangeCode));

        Assert.Equal("Invalid idValue format.", MappingResults.Failed(results[2]).Message);
    }

    [Fact]
    public void NotFoundIsReadFromAWarningAsTheAPISendsItAndFromAnErrorAsItsDocumentationShows()
    {
        var live = OpenFigiParser.ParseMapping(TestData.Read("mapping_not_found_and_cusip.json"), jobCount: 2);
        Assert.Equal(OpenFigiParser.NoIdentifierFound, MappingResults.NotFound(live[0]).Message);
        Assert.Contains(MappingResults.Found(live[1]).Records, record => record.Figi == "BBG000B9XRY4");   // Apple by CUSIP

        var shapes = OpenFigiParser.ParseMapping(TestData.Read("mapping_documented_shapes.json"));
        MappingResults.Found(shapes[0]);
        MappingResults.NotFound(shapes[1]);                                                               // "error": "No identifier found."
        MappingResults.NotFound(shapes[2]);                                                               // "warning": "No identifier found."
        Assert.StartsWith("Too many results", MappingResults.Failed(shapes[3]).Message);
        var bare = Assert.Single(MappingResults.Found(shapes[4]).Records);
        Assert.Equal(("BBG00XXXXXX1", "Metadata N/A", null), (bare.Figi, bare.Metadata, bare.Name));
    }

    [Fact]
    public void AMappingResponseThatDoesNotMatchItsJobsIsRefused()
    {
        Assert.Contains("2 results for 3 jobs",
                        Assert.Throws<OpenFigiException>(() => OpenFigiParser.ParseMapping(TestData.Read("mapping_not_found_and_cusip.json"), jobCount: 3)).Message);
        Assert.Throws<OpenFigiException>(() => OpenFigiParser.ParseMapping("{\"data\": []}"));
        Assert.Throws<OpenFigiException>(() => OpenFigiParser.ParseMapping("not json"));
    }

    [Fact]
    public void SearchAndFilterPagesCarryTheNextTokenAndTheTotal()
    {
        var search = OpenFigiParser.ParseSearchPage(TestData.Read("search_page_with_next.json"));
        Assert.Equal(100, search.Records.Count);
        Assert.False(string.IsNullOrEmpty(search.Next));
        Assert.Null(search.Total);

        var last = OpenFigiParser.ParseSearchPage(TestData.Read("search_last_page.json"));
        Assert.Equal(("IBM", null), (Assert.Single(last.Records).Ticker, last.Next));

        var filter = OpenFigiParser.ParseFilterPage(TestData.Read("filter_page_with_total.json"));
        Assert.Equal(4, filter.Total);
        Assert.Equal(4, filter.Records.Count);
        Assert.Null(filter.Next);

        var searchError = Assert.Throws<OpenFigiException>(() => OpenFigiParser.ParseSearchPage(TestData.Read("search_error.json")));
        Assert.Contains("at most 12", searchError.Message);
        Assert.Null(searchError.StatusCode);                          // the response came back: its content is the error
    }

    [Fact]
    public void AFieldsValuesAreRead()
    {
        var values = OpenFigiParser.ParseMappingValues(TestData.Read("values_exch_code.json"));
        Assert.True(values.Count > 1000);
        Assert.Contains("US", values);
        Assert.Contains("LN", values);
        Assert.Throws<OpenFigiException>(() => OpenFigiParser.ParseMappingValues("{\"error\": \"Invalid key.\"}"));
    }

    /// <summary>Every named constant is a value the API lists (saved from <c>/v3/mapping/values</c> on 2026-11-15), and every
    /// identifier type it lists has a constant.</summary>
    [Fact]
    public void TheNamedConstantsAreTheAPIsOwnValues()
    {
        static IReadOnlyList<string> Constants<T>() => typeof(T).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(field => field.FieldType == typeof(T)).Select(field => field.GetValue(null)!.ToString()!).ToList();

        var idTypes = OpenFigiParser.ParseMappingValues(TestData.Read("values_id_type.json"));
        Assert.Empty(Constants<IdType>().Except(idTypes));
        Assert.Empty(idTypes.Except(Constants<IdType>()));
        Assert.Empty(Constants<SecurityType2>().Except(OpenFigiParser.ParseMappingValues(TestData.Read("values_security_type2.json"))));
        Assert.Empty(Constants<MarketSector>().Except(OpenFigiParser.ParseMappingValues(TestData.Read("values_market_sector.json"))));
    }

    [Theory]
    [InlineData(400, "error_400.txt", "the request is malformed", "must be a JSON array")]
    [InlineData(401, "error_401.txt", "the API key is not valid", "Invalid API key.")]
    [InlineData(413, "error_413.txt", "too many jobs", "only contain 10 mapping jobs")]
    [InlineData(429, "error_429.txt", "the rate limit was reached", "Too many requests")]
    public void ARefusedRequestsPlainTextBodyBecomesTheException(int status, string file, string meaning, string message)
    {
        var exception = OpenFigiParser.ParseError(status, TestData.Read(file), status == 429 ? TimeSpan.FromSeconds(30) : null);
        Assert.Equal(status, exception.StatusCode);
        Assert.Contains(meaning, exception.Message);
        Assert.Contains(message, exception.Message);
        Assert.Equal(status == 429 ? TimeSpan.FromSeconds(30) : null, exception.RetryAfter);
        Assert.Contains("Down for maintenance", OpenFigiParser.ParseError(503, "{\"error\": \"Down for maintenance\"}").Message);
    }
}
