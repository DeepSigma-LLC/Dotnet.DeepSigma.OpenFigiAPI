// An open value: the API's own string, with the values known when this was written as named constants, each
// described by its name. A value OpenFIGI adds later is passed as new SecurityType2("NEW_VALUE") without waiting for a
// release; OpenFigiClient.MappingValuesAsync reads the current lists.
#pragma warning disable CS1591

namespace DeepSigma.OpenFigiAPI;

/// <summary>The detailed security type (<c>securityType2</c>): <c>Common Stock</c>, <c>Option</c>, <c>Corp</c>, ...</summary>
public readonly record struct SecurityType2(string Value)
{
    public static readonly SecurityType2 CommonStock = new("Common Stock");
    public static readonly SecurityType2 Preference = new("Preference");
    public static readonly SecurityType2 DepositaryReceipt = new("Depositary Receipt");
    public static readonly SecurityType2 MutualFund = new("Mutual Fund");
    public static readonly SecurityType2 Option = new("Option");
    public static readonly SecurityType2 Warrant = new("Warrant");
    public static readonly SecurityType2 Future = new("Future");
    public static readonly SecurityType2 Pool = new("Pool");
    public static readonly SecurityType2 Corp = new("Corp");
    public static readonly SecurityType2 Govt = new("Govt");
    public static readonly SecurityType2 Muni = new("Muni");
    public static readonly SecurityType2 Mortgage = new("Mtge");
    public static readonly SecurityType2 Index = new("Index");
    public static readonly SecurityType2 Currency = new("Curncy");

    public override string ToString() => Value;
}
