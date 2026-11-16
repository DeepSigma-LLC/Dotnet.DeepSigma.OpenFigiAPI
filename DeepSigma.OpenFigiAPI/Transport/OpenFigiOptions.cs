namespace DeepSigma.OpenFigiAPI;

/// <summary>How <see cref="HttpOpenFigiTransport"/> calls the API.</summary>
public sealed record OpenFigiOptions
{
    /// <summary>The key (<c>X-OPENFIGI-APIKEY</c>); optional. With one the API allows more requests and 100 jobs a
    /// request instead of 10.</summary>
    public string? ApiKey { get; init; }

    /// <summary>The API's address.</summary>
    public Uri BaseAddress { get; init; } = new("https://api.openfigi.com");

    /// <summary>Space requests to keep within the API's limits. Off for a caller that limits them elsewhere.</summary>
    public bool LimitRequests { get; init; } = true;

    /// <summary>Mapping and field-value requests a minute; by default the API's: 25 without a key, 250 with one.</summary>
    public int? MappingRequestsPerMinute { get; init; }

    /// <summary>Search and filter requests a minute; by default the API's: 5 without a key, 20 with one.</summary>
    public int? SearchRequestsPerMinute { get; init; }

    /// <summary>How many times a request is sent in all, when it meets a 429, 500 or 503.</summary>
    public int MaxAttempts { get; init; } = 5;

    /// <summary>The first wait after a 429 without <c>retry-after</c>, a 500 or a 503; doubled each time, with jitter, up
    /// to <see cref="MaxBackoff"/>.</summary>
    public TimeSpan BaseBackoff { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>The longest wait between attempts, whatever a 429 asks or the backoff reaches.</summary>
    public TimeSpan MaxBackoff { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>How long one call may take in all: every attempt, the waits between them, and the response's body.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>The clock waits are measured on; the system's by default.</summary>
    public TimeProvider Clock { get; init; } = TimeProvider.System;

    /// <summary>Whether a key is set.</summary>
    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);
}
