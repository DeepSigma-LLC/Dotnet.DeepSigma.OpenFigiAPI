// An open value: the API's own string, with the values known when this was written as named constants, each
// described by its name. A value OpenFIGI adds later is passed as new MarketSector("NEW_VALUE") without waiting for a
// release; OpenFigiClient.MappingValuesAsync reads the current lists.
#pragma warning disable CS1591

namespace DeepSigma.OpenFigiAPI;

/// <summary>The market sector (<c>marketSecDes</c>): <c>Equity</c>, <c>Corp</c>, <c>Govt</c>, ...</summary>
public readonly record struct MarketSector(string Value)
{
    public static readonly MarketSector Commodity = new("Comdty");
    public static readonly MarketSector Corporate = new("Corp");
    public static readonly MarketSector Currency = new("Curncy");
    public static readonly MarketSector Equity = new("Equity");
    public static readonly MarketSector Government = new("Govt");
    public static readonly MarketSector Index = new("Index");
    public static readonly MarketSector MoneyMarket = new("M-Mkt");
    public static readonly MarketSector Mortgage = new("Mtge");
    public static readonly MarketSector Municipal = new("Muni");
    public static readonly MarketSector Preferred = new("Pfd");

    public override string ToString() => Value;
}
