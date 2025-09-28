using System.Collections.Generic;

namespace Core.Validation
{
    /// <summary>
    /// Interface for validating region codes (continent, country, city)
    /// </summary>
    public interface IRegionValidator
    {
        /// <summary>
        /// Validates a continent code.
        /// </summary>
        /// <param name="continentCode">The continent code to validate.</param>
        /// <returns>True if the continent code is valid; otherwise, false.</returns>
        bool IsValidContinentCode(string continentCode);

        /// <summary>
        /// Validates a country code.
        /// </summary>
        /// <param name="countryCode">The country code to validate.</param>
        /// <returns>True if the country code is valid; otherwise, false.</returns>
        bool IsValidCountryCode(string countryCode);

        /// <summary>
        /// Validates a city code.
        /// </summary>
        /// <param name="cityCode">The city code to validate.</param>
        /// <returns>True if the city code is valid; otherwise, false.</returns>
        bool IsValidCityCode(string cityCode);

        /// <summary>
        /// Validates a list of region codes based on the specified type.
        /// </summary>
        /// <param name="regionType">The type of region (continent, country, city).</param>
        /// <param name="regionCodes">The list of region codes to validate.</param>
        /// <param name="invalidCodes">When this method returns, contains the list of invalid codes that were found, if any.</param>
        /// <returns>True if all region codes are valid; otherwise, false.</returns>
        bool ValidateRegionCodes(RegionType regionType, IEnumerable<string> regionCodes, out List<string> invalidCodes);
    }
}
