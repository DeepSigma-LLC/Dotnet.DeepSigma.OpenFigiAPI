using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace DeepSigma.OpenFigiAPI;

/// <summary>
/// The OpenFIGI API: mapping identifiers to FIGIs, search and filter, and the valid values of the mapping fields. Every
/// request is checked against the API's rules before it is sent (<see cref="OpenFigiRules"/>), written by
/// <see cref="OpenFigiRequests"/>, sent through an <see cref="IOpenFigiTransport"/>, and read by
/// <see cref="OpenFigiParser"/>. A request the API refuses throws an <see cref="OpenFigiException"/>; within a mapping
/// response, each job has its own result.
/// </summary>
/// <param name="transport">How calls travel: <see cref="HttpOpenFigiTransport"/>, or a caller's own.</param>
/// <param name="hasApiKey">Whether the transport sends a key: the API then takes 100 jobs a request instead of 10.</param>
public sealed class OpenFigiClient(IOpenFigiTransport transport, bool hasApiKey)
{
    /// <summary>Jobs per mapping request without a key, and with one.</summary>
    public const int JobsPerRequestWithoutKey = 10, JobsPerRequestWithKey = 100;

    private readonly ConcurrentDictionary<string, IReadOnlyList<string>> _values = new();

    /// <summary>A client calling the API directly (<see cref="HttpOpenFigiTransport"/>).</summary>
    /// <param name="options">How to call the API; the defaults when null (no key).</param>
    /// <param name="handler">The handler that sends requests; a test passes a fake one (<see cref="HttpOpenFigiTransport"/>).</param>
    public static OpenFigiClient Create(OpenFigiOptions? options = null, HttpMessageHandler? handler = null)
    {
        options ??= new OpenFigiOptions();
        return new OpenFigiClient(new HttpOpenFigiTransport(options, handler), options.HasApiKey);
    }

    /// <summary>How many jobs one mapping request carries: 10 without a key, 100 with one.</summary>
    public int JobsPerRequest => hasApiKey ? JobsPerRequestWithKey : JobsPerRequestWithoutKey;

    // ------------------------------------------------------------------ mapping

    /// <summary>Maps every job, in requests of <see cref="JobsPerRequest"/>; one result per job, in the jobs' order.
    /// Every job is checked before the first request is sent.</summary>
    public async Task<IReadOnlyList<MappingResult>> MapAsync(IReadOnlyList<MappingJob> jobs, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        foreach (var job in jobs) OpenFigiRules.Check(job);
        var results = new List<MappingResult>(jobs.Count);
        foreach (var batch in jobs.Chunk(JobsPerRequest))
        {
            var response = await SendAsync(new OpenFigiRequest(HttpMethod.Post, "/v3/mapping", OpenFigiRequests.MappingBody(batch), OpenFigiCallGroup.Mapping),
                                           cancellation);
            results.AddRange(OpenFigiParser.ParseMapping(response.Body, batch.Length));
        }
        return results;
    }

    /// <summary>Maps one job.</summary>
    public async Task<MappingResult> MapAsync(MappingJob job, CancellationToken cancellation = default) =>
        (await MapAsync([job], cancellation))[0];

    // ------------------------------------------------------------------ search and filter

    /// <summary>One page of a search (<c>/v3/search</c>): from the first page, or from <paramref name="start"/> (a page's
    /// <see cref="FigiPage.Next"/>).</summary>
    public Task<FigiPage> SearchPageAsync(FigiQuery query, string? start = null, CancellationToken cancellation = default) =>
        PageAsync("/v3/search", query, queryRequired: true, start, OpenFigiParser.ParseSearchPage, cancellation);

    /// <summary>One page of a filter (<c>/v3/filter</c>, sorted by FIGI, with the total).</summary>
    public Task<FigiPage> FilterPageAsync(FigiQuery query, string? start = null, CancellationToken cancellation = default) =>
        PageAsync("/v3/filter", query, queryRequired: false, start, OpenFigiParser.ParseFilterPage, cancellation);

    /// <summary>Every record of a search, page after page, up to <paramref name="maxResults"/> when given.</summary>
    public IAsyncEnumerable<FigiRecord> SearchAsync(FigiQuery query, int? maxResults = null, CancellationToken cancellation = default) =>
        AllAsync(start => SearchPageAsync(query, start, cancellation), maxResults, cancellation);

    /// <summary>Every record of a filter, page after page, up to <paramref name="maxResults"/> when given.</summary>
    public IAsyncEnumerable<FigiRecord> FilterAsync(FigiQuery query, int? maxResults = null, CancellationToken cancellation = default) =>
        AllAsync(start => FilterPageAsync(query, start, cancellation), maxResults, cancellation);

    // ------------------------------------------------------------------ field values

    /// <summary>The valid values of a mapping field (<c>/v3/mapping/values/{key}</c>), kept for this client's life unless
    /// <paramref name="refresh"/>.</summary>
    public async Task<IReadOnlyList<string>> MappingValuesAsync(MappingField field, bool refresh = false, CancellationToken cancellation = default)
    {
        if (!refresh && _values.TryGetValue(field.Value, out var known)) return known;
        var response = await SendAsync(new OpenFigiRequest(HttpMethod.Get, $"/v3/mapping/values/{Uri.EscapeDataString(field.Value)}", null,
                                                           OpenFigiCallGroup.Mapping), cancellation);
        var values = OpenFigiParser.ParseMappingValues(response.Body);
        _values[field.Value] = values;
        return values;
    }

    // ------------------------------------------------------------------

    private async Task<FigiPage> PageAsync(string path, FigiQuery query, bool queryRequired, string? start, Func<string, FigiPage> parse,
                                           CancellationToken cancellation)
    {
        var response = await SendAsync(new OpenFigiRequest(HttpMethod.Post, path, OpenFigiRequests.QueryBody(query, queryRequired, start),
                                                           OpenFigiCallGroup.Search), cancellation);
        return parse(response.Body);
    }

    private static async IAsyncEnumerable<FigiRecord> AllAsync(Func<string?, Task<FigiPage>> page, int? maxResults,
                                                              [EnumeratorCancellation] CancellationToken cancellation)
    {
        if (maxResults is <= 0) yield break;
        var count = 0;
        string? start = null;
        do
        {
            cancellation.ThrowIfCancellationRequested();
            var current = await page(start);
            foreach (var record in current.Records)
            {
                yield return record;
                if (++count == maxResults) yield break;
            }
            start = current.Records.Count == 0 ? null : current.Next;
        }
        while (start is not null);
    }

    private async Task<OpenFigiResponse> SendAsync(OpenFigiRequest request, CancellationToken cancellation)
    {
        var response = await transport.SendAsync(request, cancellation);
        if (response.StatusCode is < 200 or >= 300) throw OpenFigiParser.ParseError(response.StatusCode, response.Body, response.RetryAfter);
        return response;
    }
}
