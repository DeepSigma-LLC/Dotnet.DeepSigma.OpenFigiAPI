// The named constants below are the API's own codes, each described by its name; the types say what they are.
#pragma warning disable CS1591

namespace DeepSigma.OpenFigiAPI;

// The API's codes as open values: each holds the API's own string, with the values known when this was written as
// named constants. A value OpenFIGI adds later is passed as new IdType("NEW_VALUE") without waiting for a release;
// OpenFigiClient.MappingValuesAsync reads the current lists.

/// <summary>What kind of identifier a mapping job holds (<c>idType</c>).</summary>
public readonly record struct IdType(string Value)
{
    public static readonly IdType Isin = new("ID_ISIN");
    public static readonly IdType Cusip = new("ID_CUSIP");
    /// <summary>An 8-character CUSIP, without its check digit.</summary>
    public static readonly IdType Cusip8 = new("ID_CUSIP_8_CHR");
    public static readonly IdType Cins = new("ID_CINS");
    public static readonly IdType Sedol = new("ID_SEDOL");
    public static readonly IdType CommonCode = new("ID_COMMON");
    /// <summary>The German Wertpapierkennnummer.</summary>
    public static readonly IdType Wertpapier = new("ID_WERTPAPIER");
    public static readonly IdType Italy = new("ID_ITALY");
    public static readonly IdType Trace = new("ID_TRACE");
    public static readonly IdType ExchangeSymbol = new("ID_EXCH_SYMBOL");
    public static readonly IdType FullExchangeSymbol = new("ID_FULL_EXCHANGE_SYMBOL");
    public static readonly IdType Ticker = new("TICKER");
    public static readonly IdType BaseTicker = new("BASE_TICKER");
    /// <summary>The FIGI itself.</summary>
    public static readonly IdType Figi = new("ID_BB_GLOBAL");
    public static readonly IdType CompositeFigi = new("COMPOSITE_ID_BB_GLOBAL");
    public static readonly IdType ShareClassFigi = new("ID_BB_GLOBAL_SHARE_CLASS_LEVEL");
    public static readonly IdType BloombergSecurityNumberDescription = new("ID_BB_SEC_NUM_DES");
    /// <summary>A listed US option's OCC symbol.</summary>
    public static readonly IdType OccSymbol = new("OCC_SYMBOL");
    public static readonly IdType OpraSymbol = new("OPRA_SYMBOL");
    /// <summary>A future's or option's unique identifier.</summary>
    public static readonly IdType UniqueFutureOption = new("UNIQUE_ID_FUT_OPT");
    public static readonly IdType TradingSystemIdentifier = new("TRADING_SYSTEM_IDENTIFIER");
    public static readonly IdType ShortCode = new("ID_SHORT_CODE");
    public static readonly IdType VendorIndexCode = new("VENDOR_INDEX_CODE");
    /// <summary>Listed by the API, not yet in its documentation.</summary>
    public static readonly IdType BarclaysTicker = new("BARCLAYS_TICKER");
    /// <summary>Listed by the API, not yet in its documentation.</summary>
    public static readonly IdType TradebookTicker = new("TRADEBOOK_TICKER");
    /// <summary>Legacy.</summary>
    public static readonly IdType BloombergId = new("ID_BB");
    /// <summary>Legacy.</summary>
    public static readonly IdType BloombergId8 = new("ID_BB_8_CHR");
    /// <summary>Legacy.</summary>
    public static readonly IdType BloombergUnique = new("ID_BB_UNIQUE");

    public override string ToString() => Value;
}

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

/// <summary>An option's right (<c>optionType</c>).</summary>
public readonly record struct OptionType(string Value)
{
    public static readonly OptionType Call = new("Call");
    public static readonly OptionType Put = new("Put");

    public override string ToString() => Value;
}

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
