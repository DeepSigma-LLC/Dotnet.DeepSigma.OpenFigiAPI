// An open value: the API's own string, with the values known when this was written as named constants, each
// described by its name. A value OpenFIGI adds later is passed as new MappingField("NEW_VALUE") without waiting for a
// release; OpenFigiClient.MappingValuesAsync reads the current lists.
#pragma warning disable CS1591

namespace DeepSigma.OpenFigiAPI;

/// <summary>A mapping field whose valid values the API lists (<c>GET /v3/mapping/values/{key}</c>).</summary>
public readonly record struct MappingField(string Value)
{
    public static readonly MappingField IdType = new("idType");
    public static readonly MappingField ExchangeCode = new("exchCode");
    public static readonly MappingField MicCode = new("micCode");
    public static readonly MappingField Currency = new("currency");
    public static readonly MappingField MarketSector = new("marketSecDes");
    public static readonly MappingField SecurityType = new("securityType");
    public static readonly MappingField SecurityType2 = new("securityType2");
    public static readonly MappingField StateCode = new("stateCode");

    public override string ToString() => Value;
}
