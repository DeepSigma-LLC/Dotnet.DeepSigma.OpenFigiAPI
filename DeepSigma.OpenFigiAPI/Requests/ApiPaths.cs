namespace DeepSigma.OpenFigiAPI;

/// <summary>The API's paths, relative to its address (version 3).</summary>
internal static class ApiPaths
{
    public const string Mapping = "/v3/mapping";
    public const string Search = "/v3/search";
    public const string Filter = "/v3/filter";

    /// <summary>The path of a mapping field's valid values.</summary>
    public static string MappingValues(MappingField field) => $"/v3/mapping/values/{Uri.EscapeDataString(field.Value)}";
}
