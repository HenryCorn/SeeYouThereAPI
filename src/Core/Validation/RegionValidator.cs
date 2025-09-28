using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Validation
{
    /// <summary>
    /// Provides validation services for ISO continent, country, and city codes.
    /// </summary>
    public class RegionValidator : IRegionValidator
    {
        // List of valid continent codes
        private static readonly HashSet<string> ValidContinentCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "AF", // Africa
            "AN", // Antarctica
            "AS", // Asia
            "EU", // Europe
            "NA", // North America
            "OC", // Oceania
            "SA"  // South America
        };

        // List of valid ISO 3166-1 alpha-2 country codes
        private static readonly HashSet<string> ValidCountryCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            // Europe
            "AD", "AL", "AT", "BA", "BE", "BG", "BY", "CH", "CY", "CZ", "DE", "DK",
            "EE", "ES", "FI", "FR", "GB", "GR", "HR", "HU", "IE", "IS", "IT", "LI",
            "LT", "LU", "LV", "MC", "MD", "ME", "MK", "MT", "NL", "NO", "PL", "PT",
            "RO", "RS", "RU", "SE", "SI", "SK", "SM", "UA", "UK", "VA",
            // North America
            "CA", "US", "MX", "BZ", "CR", "CU", "DO", "GT", "HN", "HT", "JM", "NI", "PA", "SV",
            // South America
            "AR", "BO", "BR", "CL", "CO", "EC", "GY", "PE", "PY", "SR", "UY", "VE",
            // Asia
            "AE", "AF", "AM", "AZ", "BD", "BH", "BN", "BT", "CN", "GE", "ID", "IL", "IN",
            "IQ", "IR", "JO", "JP", "KG", "KH", "KP", "KR", "KW", "KZ", "LA", "LB", "LK",
            "MM", "MN", "MY", "NP", "OM", "PH", "PK", "PS", "QA", "SA", "SG", "SY", "TH",
            "TJ", "TM", "TR", "TW", "UZ", "VN", "YE",
            // Africa
            "AO", "BF", "BI", "BJ", "BW", "CD", "CF", "CG", "CI", "CM", "CV", "DJ", "DZ",
            "EG", "EH", "ER", "ET", "GA", "GH", "GM", "GN", "GQ", "GW", "KE", "KM", "LR",
            "LS", "LY", "MA", "MG", "ML", "MR", "MU", "MW", "MZ", "NA", "NE", "NG", "RW",
            "SC", "SD", "SL", "SN", "SO", "SS", "SZ", "TD", "TG", "TN", "TZ", "UG", "ZA", "ZM", "ZW",
            // Oceania
            "AU", "FJ", "FM", "KI", "MH", "NR", "NZ", "PG", "PW", "SB", "TO", "TV", "VU", "WS"
        };

        // IATA Airport/City codes are 3-letter codes
        private const int IataCodeLength = 3;

        /// <summary>
        /// Validates a continent code.
        /// </summary>
        /// <param name="continentCode">The continent code to validate.</param>
        /// <returns>True if the continent code is valid; otherwise, false.</returns>
        public bool IsValidContinentCode(string continentCode)
        {
            if (string.IsNullOrWhiteSpace(continentCode))
                return false;

            return ValidContinentCodes.Contains(continentCode);
        }

        /// <summary>
        /// Validates a country code according to ISO 3166-1 alpha-2.
        /// </summary>
        /// <param name="countryCode">The country code to validate.</param>
        /// <returns>True if the country code is valid; otherwise, false.</returns>
        public bool IsValidCountryCode(string countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
                return false;

            return ValidCountryCodes.Contains(countryCode);
        }

        /// <summary>
        /// Validates an IATA airport/city code.
        /// </summary>
        /// <param name="cityCode">The city code to validate.</param>
        /// <returns>True if the city code appears to be a valid IATA code; otherwise, false.</returns>
        public bool IsValidCityCode(string cityCode)
        {
            if (string.IsNullOrWhiteSpace(cityCode))
                return false;

            // Basic IATA code validation - must be exactly 3 uppercase letters
            return cityCode.Length == IataCodeLength &&
                   cityCode.All(char.IsLetter) &&
                   cityCode.All(char.IsUpper);
        }

        /// <summary>
        /// Validates a list of region codes based on the specified type.
        /// </summary>
        /// <param name="regionType">The type of region (continent, country, city).</param>
        /// <param name="regionCodes">The list of region codes to validate.</param>
        /// <param name="invalidCodes">When this method returns, contains the list of invalid codes that were found, if any.</param>
        /// <returns>True if all region codes are valid; otherwise, false.</returns>
        public bool ValidateRegionCodes(RegionType regionType, IEnumerable<string> regionCodes, out List<string> invalidCodes)
        {
            invalidCodes = new List<string>();

            if (regionCodes == null || !regionCodes.Any())
                return false;

            bool allValid = true;

            foreach (var code in regionCodes)
            {
                bool isValid = regionType switch
                {
                    RegionType.Continent => IsValidContinentCode(code),
                    RegionType.Country => IsValidCountryCode(code),
                    RegionType.City => IsValidCityCode(code),
                    _ => false
                };

                if (!isValid)
                {
                    invalidCodes.Add(code);
                    allValid = false;
                }
            }

            return allValid;
        }
    }
}
