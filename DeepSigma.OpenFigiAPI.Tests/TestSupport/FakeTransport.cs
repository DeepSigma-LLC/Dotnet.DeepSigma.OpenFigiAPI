namespace DeepSigma.OpenFigiAPI.Tests;

/// <summary>A transport that answers each request by a function of it, and keeps what was sent.</summary>
internal sealed class FakeTransport(Func<OpenFigiRequest, OpenFigiResponse> answer, bool sendsApiKey = false) : IOpenFigiTransport
{
    public List<OpenFigiRequest> Sent { get; } = [];

    public bool SendsApiKey => sendsApiKey;

    public Task<OpenFigiResponse> SendAsync(OpenFigiRequest request, CancellationToken cancellation = default)
    {
        Sent.Add(request);
        return Task.FromResult(answer(request));
    }
}
