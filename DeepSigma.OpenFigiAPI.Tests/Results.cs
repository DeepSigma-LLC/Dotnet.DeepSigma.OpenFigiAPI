using Xunit.Sdk;

namespace DeepSigma.OpenFigiAPI.Tests;

/// <summary>A mapping result's case, or a test failure naming what it was instead.</summary>
internal static class Results
{
    public static MappingFound Found(MappingResult result) => result is MappingFound found ? found : throw Unexpected("found", result);
    public static MappingNotFound NotFound(MappingResult result) => result is MappingNotFound notFound ? notFound : throw Unexpected("not found", result);
    public static MappingFailed Failed(MappingResult result) => result is MappingFailed failed ? failed : throw Unexpected("failed", result);

    private static XunitException Unexpected(string expected, MappingResult result) =>
        new($"expected a mapping result {expected}, got {result switch { MappingFound f => f, MappingNotFound n => n, MappingFailed x => (object)x }}");
}
