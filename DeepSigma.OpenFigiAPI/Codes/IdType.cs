// An open value: the API's own string, with the values known when this was written as named constants, each
// described by its name. A value OpenFIGI adds later is passed as new IdType("NEW_VALUE") without waiting for a
// release; OpenFigiClient.MappingValuesAsync reads the current lists.
#pragma warning disable CS1591

namespace DeepSigma.OpenFigiAPI;

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
