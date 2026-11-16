using System.Text.Json;
using Xunit;

namespace DeepSigma.OpenFigiAPI.Tests;

/// <summary>The request bodies as the API takes them, and the documented rules checked before anything is sent.</summary>
public sealed class RequestTests
{
    [Fact]
    public void AMappingBodyIsAnArrayOfJobsWithTheirFiltersInTheAPIsNames()
    {
        var option = new MappingJob(IdType.BaseTicker, "AAPL")
        {
            Filters = new FigiFilters
            {
                SecurityType2 = SecurityType2.Option, OptionType = OptionType.Call, Strike = new NumberRange(150m, null),
                Expiration = new DateRange(new DateOnly(2026, 12, 1), new DateOnly(2027, 6, 30)), ExchangeCode = "US",
            },
        };
        var body = JsonDocument.Parse(OpenFigiRequests.MappingBody([MappingJob.Isin("US0378331005"), option])).RootElement;

        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Assert.Equal("""{"idType":"ID_ISIN","idValue":"US0378331005"}""", body[0].GetRawText());
        var second = body[1];
        Assert.Equal(("BASE_TICKER", "Option", "Call", "US"), (second.GetProperty("idType").GetString(), second.GetProperty("securityType2").GetString(),
                                                          second.GetProperty("optionType").GetString(), second.GetProperty("exchCode").GetString()));
        Assert.Equal("[150,null]", second.GetProperty("strike").GetRawText());
        Assert.Equal("""["2026-12-01","2027-06-30"]""", second.GetProperty("expiration").GetRawText());
    }

    [Fact]
    public void AQueryBodyCarriesItsKeywordsItsPageAndItsFilters()
    {
        var query = new FigiQuery("apple") { Filters = new FigiFilters { MarketSector = MarketSector.Equity, IncludeUnlistedEquities = true } };
        Assert.Equal("""{"query":"apple","start":"abc","marketSecDes":"Equity","includeUnlistedEquities":true}""",
                     OpenFigiRequests.QueryBody(query, queryRequired: true, start: "abc"));
        Assert.Equal("""{"securityType2":"Common Stock"}""",
                     OpenFigiRequests.QueryBody(new FigiQuery(null) { Filters = new FigiFilters { SecurityType2 = SecurityType2.CommonStock } }, queryRequired: false));
    }

    [Fact]
    public void TheDocumentedRulesAreCheckedBeforeAnythingIsSent()
    {
        void Refused(MappingJob job, string rule) => Assert.Contains(rule, Assert.Throws<ArgumentException>(() => OpenFigiRules.Check(job)).Message);

        Refused(new MappingJob(IdType.Isin, " "), "names its idValue");
        Refused(new MappingJob(IdType.BaseTicker, "AAPL"), "needs securityType2");
        Refused(new MappingJob(IdType.ExchangeSymbol, "AAPL"), "needs securityType2");
        Refused(MappingJob.Ticker("AAPL", "US") with { Filters = new FigiFilters { ExchangeCode = "US", MicCode = "XNAS" } }, "not both");
        Refused(new MappingJob(IdType.BaseTicker, "AAPL") { Filters = new FigiFilters { SecurityType2 = SecurityType2.Option } }, "needs an expiration");
        Refused(new MappingJob(IdType.Cusip, "31417YAA1") { Filters = new FigiFilters { SecurityType2 = SecurityType2.Pool } }, "needs a maturity");
        Refused(MappingJob.Isin("X") with { Filters = new FigiFilters { Strike = new NumberRange(null, null) } }, "at least one end");
        Refused(MappingJob.Isin("X") with { Filters = new FigiFilters { Coupon = new NumberRange(5m, 4m) } }, "reversed");
        Refused(MappingJob.Isin("X") with { Filters = new FigiFilters { Maturity = new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2027, 6, 1)) } },
                "at most a year");
        OpenFigiRules.Check(MappingJob.Isin("X") with { Filters = new FigiFilters { Maturity = new DateRange(new DateOnly(2026, 1, 1), null) } });

        Assert.Contains("needs keywords", Assert.Throws<ArgumentException>(() => OpenFigiRules.Check(new FigiQuery(""), queryRequired: true)).Message);
        Assert.Contains("at most 12", Assert.Throws<ArgumentException>(() => OpenFigiRules.Check(new FigiQuery(string.Join(' ', Enumerable.Range(1, 13))),
                                                                                                  queryRequired: true)).Message);
        OpenFigiRules.Check(new FigiQuery(null), queryRequired: false);
    }

    [Fact]
    public void AnIdentifierTypeTheAPIAddsLaterIsPassedWithoutARelease()
    {
        var body = OpenFigiRequests.MappingBody([new MappingJob(new IdType("ID_SOMETHING_NEW"), "123")]);
        Assert.Equal("""[{"idType":"ID_SOMETHING_NEW","idValue":"123"}]""", body);
        Assert.Equal(IdType.Isin, new IdType("ID_ISIN"));
    }
}
