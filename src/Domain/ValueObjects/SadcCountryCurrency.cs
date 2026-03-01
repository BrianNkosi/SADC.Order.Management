namespace SADC.Order.Management.Domain.ValueObjects;

/// <summary>
/// Provides validation for SADC (Southern African Development Community) country codes
/// and their valid ISO 4217 currency codes, including CMA (Common Monetary Area) rules.
///
/// CMA members (South Africa, Namibia, Lesotho, Eswatini) accept ZAR alongside their local currencies.
/// </summary>
public static class SadcCountryCurrency
{
    /// <summary>
    /// SADC member states with their valid currencies.
    /// CMA members: ZA, NA, LS, SZ — all accept ZAR in addition to local currency.
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> CountryCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        // CMA Members — accept ZAR interchangeably
        ["ZA"] = ["ZAR"],
        ["NA"] = ["NAD", "ZAR"],
        ["LS"] = ["LSL", "ZAR"],
        ["SZ"] = ["SZL", "ZAR"],

        // Non-CMA SADC Members
        ["BW"] = ["BWP"],
        ["MZ"] = ["MZN"],
        ["ZM"] = ["ZMW"],
        ["ZW"] = ["ZWL", "USD"], // Zimbabwe accepts USD
        ["AO"] = ["AOA"],
        ["CD"] = ["CDF"],
        ["MG"] = ["MGA"],
        ["MW"] = ["MWK"],
        ["MU"] = ["MUR"],
        ["SC"] = ["SCR"],
        ["TZ"] = ["TZS"],
        ["KM"] = ["KMF"]
    };

    /// <summary>
    /// All valid SADC country codes.
    /// </summary>
    public static IReadOnlyCollection<string> ValidCountryCodes => CountryCurrencies.Keys;

    /// <summary>
    /// All currencies used across SADC.
    /// </summary>
    public static IReadOnlySet<string> AllCurrencies { get; } =
        CountryCurrencies.Values.SelectMany(c => c).ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// CMA member country codes.
    /// </summary>
    public static IReadOnlySet<string> CmaCountries { get; } =
        new HashSet<string>(["ZA", "NA", "LS", "SZ"], StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if the country code is a valid SADC member state.
    /// </summary>
    public static bool IsValidCountry(string countryCode)
        => CountryCurrencies.ContainsKey(countryCode);

    /// <summary>
    /// Checks if the currency code is valid for the given SADC country.
    /// </summary>
    public static bool IsValidCurrencyForCountry(string countryCode, string currencyCode)
        => CountryCurrencies.TryGetValue(countryCode, out var currencies)
           && currencies.Contains(currencyCode.ToUpperInvariant());

    /// <summary>
    /// Returns the set of valid currencies for a given country, or empty if the country is invalid.
    /// </summary>
    public static IReadOnlySet<string> GetValidCurrencies(string countryCode)
        => CountryCurrencies.TryGetValue(countryCode, out var currencies)
            ? currencies
            : new HashSet<string>();

    /// <summary>
    /// Whether the country is a CMA member (accepts ZAR).
    /// </summary>
    public static bool IsCmaMember(string countryCode)
        => CmaCountries.Contains(countryCode);
}
