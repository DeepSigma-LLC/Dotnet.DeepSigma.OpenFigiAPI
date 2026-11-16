# DeepSigma.OpenFigiAPI

A .NET client for the [OpenFIGI](https://www.openfigi.com/api) API: map identifiers (ISIN, CUSIP, SEDOL, ticker, OCC
symbol and the rest) to FIGIs, search and filter for instruments with paging, and read the valid values of each mapping
field.

- **Typed requests, checked before sending.** Mapping jobs, queries and filters are typed, and the API's documented rules
  are checked before a request is sent, so a broken job costs no call: an exchange code or a MIC, not both;
  `securityType2` with `BASE_TICKER` and `ID_EXCH_SYMBOL`; an expiration with options and warrants, a maturity with
  pools; range ends in order, date ranges at most a year; at most 12 query terms.
- **Open codes.** Identifier types, security types and market sectors are small value types holding the API's own string,
  with the known values as constants (`IdType.Isin`, `SecurityType2.Option`). A value the API adds later works at once:
  `new IdType("NEW_VALUE")`.
- **One result per job.** Mapping takes any number of jobs, sends them 10 to a request (100 with a key), and returns one
  result per job, in order: `MappingFound` (its records), `MappingNotFound` or `MappingFailed`, a C# union. A request
  the API refuses throws `OpenFigiException` with its status.
- **Parsers you can call yourself.** Every response is read by a public static method of `OpenFigiParser`, the same one
  the client uses, so a saved response is read without a web request, and changes to deserialization are tested on saved
  responses.
- **Your transport, or ours.** Calls go through `IOpenFigiTransport`. `HttpOpenFigiTransport` sends them on the
  DeepSigma web handlers (`DeepSigma.DataAccess.Web`): it keeps to the API's rate limits, waits out a 429 and retries a
  500 or 503 with backoff. A program with its own gateway for vendor calls implements the interface and keeps everything
  else.

## Target framework

.NET 11 (C# 15).

## Installation

The package is published, with the other DeepSigma packages it uses (`DeepSigma.DataAccess.Web`), to the DeepSigma-LLC
package feed on GitHub (`https://nuget.pkg.github.com/DeepSigma-LLC/index.json`). Add that feed as a package source, with
a GitHub token allowed to read packages, then:

```bash
dotnet add package DeepSigma.OpenFigiAPI
```

## Getting started

```csharp
using DeepSigma.OpenFigiAPI;

// The key is optional; with one the API allows more requests, and 100 jobs a request instead of 10.
var client = OpenFigiClient.Create(new OpenFigiOptions { ApiKey = Environment.GetEnvironmentVariable("OPENFIGI_API_KEY") });

// Map identifiers to FIGIs: one result per job, in order.
var results = await client.MapAsync([
    MappingJob.Isin("US0378331005"),
    MappingJob.Ticker("IBM", exchangeCode: "US"),
    MappingJob.Cusip("459200101"),
]);
foreach (var result in results)
{
    var line = result switch
    {
        MappingFound found => string.Join(", ", found.Records.Select(r => $"{r.Figi} {r.Ticker} {r.ExchangeCode}")),
        MappingNotFound notFound => $"not found: {notFound.Message}",
        MappingFailed failed => $"failed: {failed.Message}",
    };
    Console.WriteLine(line);
}

// Narrow a job with filters: a listed option by its underlying, right, strike and expiry.
var option = new MappingJob(IdType.BaseTicker, "AAPL")
{
    Filters = new FigiFilters
    {
        SecurityType2 = SecurityType2.Option, OptionType = OptionType.Call, ExchangeCode = "US",
        Strike = new NumberRange(200m, 200m), Expiration = new DateRange(new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 31)),
    },
};

// Search: every record, page after page, up to a limit.
await foreach (var record in client.SearchAsync(new FigiQuery("international business machines"), maxResults: 50))
    Console.WriteLine($"{record.Figi} {record.Name} {record.SecurityType2}");

// Filter: sorted by FIGI, with the total; one page at a time for a screen.
var page = await client.FilterPageAsync(new FigiQuery("apple") { Filters = new FigiFilters { SecurityType2 = SecurityType2.CommonStock } });
Console.WriteLine($"{page.Total} in all; next page: {page.Next}");

// The valid values of a mapping field, kept for the client's life.
var exchanges = await client.MappingValuesAsync(MappingField.ExchangeCode);
```

## Reading saved responses

Each response has a parser that needs no client and no network:

| Method | Reads |
|---|---|
| `OpenFigiParser.ParseMapping(json, jobCount)` | a `/v3/mapping` response: one `MappingResult` per job |
| `OpenFigiParser.ParseMappingResult(element)` | one element of it |
| `OpenFigiParser.ParseSearchPage(json)` | a `/v3/search` response: records and the next page's token |
| `OpenFigiParser.ParseFilterPage(json)` | a `/v3/filter` response: records, the next token and the total |
| `OpenFigiParser.ParseMappingValues(json)` | a `/v3/mapping/values/{key}` response |
| `OpenFigiParser.ParseRecord(element)` | one record; `FigiRecord.Raw` keeps it as received |
| `OpenFigiParser.ParseError(status, body)` | a refused request's body, as the `OpenFigiException` the client throws |

`OpenFigiException.StatusCode` is the HTTP status of a refused request, and null when the response came back but its
content is an error (a search's `error`) or not what the API documents.

A miss is `MappingNotFound` whether the API sends it as a `warning` (as it does) or as the `error` "No identifier found."
(as its documentation's example shows).

## Using your own transport

```csharp
public sealed class MyGatewayTransport(MyGateway gateway) : IOpenFigiTransport
{
    // whether the gateway sends an API key: the client then sends mapping jobs 100 to a request instead of 10
    public bool SendsApiKey => gateway.HasOpenFigiKey;

    public async Task<OpenFigiResponse> SendAsync(OpenFigiRequest request, CancellationToken cancellation = default)
    {
        // send request.Method, request.Path and request.JsonBody your way; request.Group says which limit it counts against
        var answer = await gateway.SendAsync(request.Method, request.Path, request.JsonBody, cancellation);
        return new OpenFigiResponse(answer.Status, answer.Body, answer.RetryAfter);
    }
}

var client = new OpenFigiClient(new MyGatewayTransport(gateway));
```

The request bodies alone are `OpenFigiRequests.MappingBody(jobs)`, `SearchBody(query, start)` and
`FilterBody(query, start)`; the rules `OpenFigiRules.Check(job)`, `CheckSearch(query)` and `CheckFilter(query)`.

`HttpOpenFigiTransport` is `IDisposable`: dispose it when done, which closes the connections it opened. A client made by
`OpenFigiClient.Create` keeps its transport for its own life; a program that makes many clients makes one transport and
passes it to each.

## Limits

| Calls | Without a key | With a key |
|---|---|---|
| mapping and field values | 25 requests a minute, 10 jobs each | 250 requests a minute, 100 jobs each |
| search and filter | 5 requests a minute | 20 requests a minute |

`HttpOpenFigiTransport` keeps to these by default, spacing requests evenly (25 a minute is one every 2.4 seconds) with
`DeepSigma.DataAccess.Web`'s `MinIntervalDelegatingHandler`. The spacing is shared by every client in the process,
keyed by group (`openfigi:mapping`, `openfigi:search`), since the API counts requests by key or address. Retries are
`RetryAfterDelegatingHandler`'s: 429, 500 and 503, up to `MaxAttempts`, each attempt spaced too. `OpenFigiOptions`
changes the limits, the attempts and the backoff, or turns limiting off (`LimitRequests = false`) for a caller that
limits elsewhere. `OpenFigiClient.Create(options, handler)` takes the handler at the end of the pipeline, which a test
replaces with a fake one.

## Layout

One type per file, in one namespace (`DeepSigma.OpenFigiAPI`), so a program needs one `using`; the folders organise the
source:

| Folder | What |
|---|---|
| (root) | `OpenFigiClient` |
| `Codes/` | The open code types: `IdType`, `SecurityType2`, `MarketSector`, `OptionType`, `MappingField` |
| `Requests/` | `MappingJob`, `FigiQuery`, `FigiFilters`, `NumberRange`, `DateRange`; `OpenFigiRules`; `OpenFigiRequests` (the bodies) |
| `Results/` | `FigiRecord`, `FigiPage`, `MappingResult` and its cases, `OpenFigiException` |
| `Parsing/` | `OpenFigiParser` |
| `Transport/` | `IOpenFigiTransport`, `OpenFigiRequest`, `OpenFigiResponse`, `OpenFigiCallGroup`, `HttpOpenFigiTransport`, `OpenFigiOptions` |

The tests mirror it (`Parsing/`, `Requests/`, `Client/`, `Transport/`, `Live/`), with the fakes and helpers in
`TestSupport/` and the saved responses in `TestData/`.

## Tests

```bash
dotnet test --solution DeepSigma.OpenFigiAPI.slnx
```

The parsers are tested on saved responses in `DeepSigma.OpenFigiAPI.Tests/TestData` (recorded from the live API, and
cut-down ones for rarer shapes), the client on a fake transport, the HTTP transport on a fake handler. One test calls
the live API, only when `OPENFIGI_LIVE_TEST=1` is set (with `OPENFIGI_API_KEY` for the keyed limits).

## Licence

MIT. FIGIs and OpenFIGI's data are subject to OpenFIGI's own terms of use.
