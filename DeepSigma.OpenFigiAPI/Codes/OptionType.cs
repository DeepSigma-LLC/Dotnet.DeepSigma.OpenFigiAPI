// An open value: the API's own string, with the values known when this was written as named constants, each
// described by its name. A value OpenFIGI adds later is passed as new OptionType("NEW_VALUE") without waiting for a
// release; OpenFigiClient.MappingValuesAsync reads the current lists.
#pragma warning disable CS1591

namespace DeepSigma.OpenFigiAPI;

/// <summary>An option's right (<c>optionType</c>).</summary>
public readonly record struct OptionType(string Value)
{
    public static readonly OptionType Call = new("Call");
    public static readonly OptionType Put = new("Put");

    public override string ToString() => Value;
}
