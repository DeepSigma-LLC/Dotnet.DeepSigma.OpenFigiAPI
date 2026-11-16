using Xunit;

namespace DeepSigma.OpenFigiAPI.Tests;

/// <summary>
/// Against the live API, only on request: set <c>OPENFIGI_LIVE_TEST=1</c> (and <c>OPENFIGI_API_KEY</c> for the keyed
/// limits). Otherwise skipped, so the suite never depends on the network. Each call is one the API documents, with
/// public identifiers only.
/// </summary>
public sealed class LiveTests
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static OpenFigiClient Client()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("OPENFIGI_LIVE_TEST") == "1", "set OPENFIGI_LIVE_TEST=1 to call the live API");
        return OpenFigiClient.Create(new OpenFigiOptions { ApiKey = Environment.GetEnvironmentVariable("OPENFIGI_API_KEY") });
    }

    [Fact]
    public async Task TheLiveAPIMapsSearchesAndListsValues()
    {
        var client = Client();
        var results = await client.MapAsync([MappingJob.Isin("US0378331005"), MappingJob.Ticker("ZQZQZQZQ", "US")], Cancellation);
        Assert.Contains(MappingResults.Found(results[0]).Records, record => record.Figi == "BBG000B9XRY4");
        MappingResults.NotFound(results[1]);

        var page = await client.SearchPageAsync(new FigiQuery("IBM") { Filters = new FigiFilters { ExchangeCode = "US" } }, cancellation: Cancellation);
        Assert.NotEmpty(page.Records);

        Assert.Contains("ID_ISIN", await client.MappingValuesAsync(MappingField.IdType, cancellation: Cancellation));
    }
}
