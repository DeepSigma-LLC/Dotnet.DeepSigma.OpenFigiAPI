namespace DeepSigma.OpenFigiAPI.Tests;

/// <summary>The saved responses in TestData, copied beside the test program.</summary>
internal static class TestData
{
    public static string Read(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", name));
}
