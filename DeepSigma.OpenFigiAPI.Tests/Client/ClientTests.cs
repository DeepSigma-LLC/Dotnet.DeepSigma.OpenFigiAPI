using System.Text.Json;
using Xunit;

namespace DeepSigma.OpenFigiAPI.Tests;

/// <summary>The client on a fake transport: batching by key, results in the jobs' order, paging, field values kept, and
/// refusals as exceptions.</summary>
public sealed class ClientTests
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    /// <summary>A mapping answer: for each job, found with a FIGI made from its value, or not found when it starts with "missing".</summary>
    private static OpenFigiResponse MappingAnswer(OpenFigiRequest request)
    {
        var jobs = JsonDocument.Parse(request.JsonBody!).RootElement.EnumerateArray().Select(job => job.GetProperty("idValue").GetString()!);
        var results = jobs.Select(value => value.StartsWith("missing")
            ? (object)new { warning = OpenFigiParser.NoIdentifierFound }
            : new { data = new[] { new { figi = "FIGI-" + value, ticker = value } } });
        return new OpenFigiResponse(200, JsonSerializer.Serialize(results));
    }

    [Theory]
    [InlineData(false, 25, 3)]
    [InlineData(true, 250, 3)]
    public async Task JobsAreSentInBatchesAndReturnedInTheirOrder(bool hasKey, int jobCount, int expectedRequests)
    {
        var transport = new FakeTransport(MappingAnswer, sendsApiKey: hasKey);
        var client = new OpenFigiClient(transport);
        var jobs = Enumerable.Range(1, jobCount).Select(i => MappingJob.Ticker(i % 7 == 0 ? $"missing{i}" : $"T{i}", "US")).ToList();

        var results = await client.MapAsync(jobs, Cancellation);

        Assert.Equal(expectedRequests, transport.Sent.Count);
        Assert.All(transport.Sent, request => Assert.Equal(("/v3/mapping", OpenFigiCallGroup.Mapping, OpenFigiCall.Mapping), (request.Path, request.Group, request.Call)));
        Assert.Equal(jobCount, results.Count);
        for (var i = 0; i < jobCount; i++)
        {
            if (jobs[i].IdValue.StartsWith("missing")) MappingResults.NotFound(results[i]);
            else Assert.Equal("FIGI-" + jobs[i].IdValue, Assert.Single(MappingResults.Found(results[i]).Records).Figi);
        }
    }

    [Fact]
    public async Task ABrokenJobIsRefusedBeforeAnyRequestIsSent()
    {
        var transport = new FakeTransport(MappingAnswer);
        var client = new OpenFigiClient(transport);
        await Assert.ThrowsAsync<ArgumentException>(() => client.MapAsync([MappingJob.Isin("US0378331005"), new MappingJob(IdType.BaseTicker, "AAPL")], Cancellation));
        Assert.Empty(transport.Sent);
    }

    [Fact]
    public async Task ARefusedRequestThrowsWithItsStatus()
    {
        var client = new OpenFigiClient(new FakeTransport(_ => new OpenFigiResponse(401, TestData.Read("error_401.txt")), sendsApiKey: true));
        var refused = await Assert.ThrowsAsync<OpenFigiException>(() => client.MapAsync(MappingJob.Isin("US0378331005"), Cancellation));
        Assert.Equal(401, refused.StatusCode);
        Assert.Contains("Invalid API key.", refused.Message);
    }

    [Fact]
    public async Task ASearchIsFollowedPageByPageUpToItsLimit()
    {
        var pages = new Dictionary<string, string>
        {
            [""] = """{"data": [{"figi": "A"}, {"figi": "B"}], "next": "p2"}""",
            ["p2"] = """{"data": [{"figi": "C"}, {"figi": "D"}], "next": "p3"}""",
            ["p3"] = """{"data": [{"figi": "E"}]}""",
        };
        var transport = new FakeTransport(request =>
        {
            var body = JsonDocument.Parse(request.JsonBody!).RootElement;
            return new OpenFigiResponse(200, pages[body.TryGetProperty("start", out var start) ? start.GetString()! : ""]);
        });
        var client = new OpenFigiClient(transport);

        var all = await client.SearchAsync(new FigiQuery("example"), cancellation: Cancellation).Select(record => record.Figi).ToListAsync(Cancellation);
        Assert.Equal(["A", "B", "C", "D", "E"], all);
        Assert.All(transport.Sent, request => Assert.Equal(("/v3/search", OpenFigiCallGroup.Search, OpenFigiCall.Search), (request.Path, request.Group, request.Call)));
        Assert.Equal(3, transport.Sent.Count);

        transport.Sent.Clear();
        var three = await client.FilterAsync(new FigiQuery(null), maxResults: 3, cancellation: Cancellation).Select(record => record.Figi).ToListAsync(Cancellation);
        Assert.Equal(["A", "B", "C"], three);
        Assert.Equal(2, transport.Sent.Count);                                              // the third page is never asked for
        Assert.All(transport.Sent, request => Assert.Equal(("/v3/filter", OpenFigiCall.Filter), (request.Path, request.Call)));

        var first = await client.SearchPageAsync(new FigiQuery("example"), cancellation: Cancellation);
        Assert.Equal("p2", first.Next);
    }

    [Fact]
    public async Task AFieldsValuesAreAskedForOnceUnlessRefreshed()
    {
        var transport = new FakeTransport(_ => new OpenFigiResponse(200, TestData.Read("values_exch_code.json")));
        var client = new OpenFigiClient(transport);

        Assert.Contains("US", await client.MappingValuesAsync(MappingField.ExchangeCode, cancellation: Cancellation));
        await client.MappingValuesAsync(MappingField.ExchangeCode, cancellation: Cancellation);
        Assert.Equal(("/v3/mapping/values/exchCode", "GET"), (Assert.Single(transport.Sent).Path, transport.Sent[0].Method.Method));
        Assert.Equal((OpenFigiCall.MappingValues, MappingField.ExchangeCode), (transport.Sent[0].Call, transport.Sent[0].Field));
        await client.MappingValuesAsync(MappingField.ExchangeCode, refresh: true, cancellation: Cancellation);
        Assert.Equal(2, transport.Sent.Count);
    }
}
