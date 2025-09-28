using System.Collections.Generic;

namespace Core.Models.Aggregation
{
    /// <summary>
    /// Represents the cheapest flight prices from a single origin to multiple destinations.
    /// </summary>
    public class OriginDestinationPrices
    {
        /// <summary>
        /// Gets or sets the origin location code.
        /// </summary>
        public string OriginCode { get; set; }

        /// <summary>
        /// Gets or sets the collection of destination prices, keyed by destination code.
        /// </summary>
        public Dictionary<string, DestinationPrice> Destinations { get; set; } = new();

        /// <summary>
        /// Gets or sets the currency used for all prices from this origin.
        /// </summary>
        public string Currency { get; set; }
    }

    /// <summary>
    /// Represents price information for a specific destination.
    /// </summary>
    public class DestinationPrice
    {
        /// <summary>
        /// Gets or sets the destination city code.
        /// </summary>
        public string DestinationCityCode { get; set; }

        /// <summary>
        /// Gets or sets the destination country code.
        /// </summary>
        public string DestinationCountryCode { get; set; }

        /// <summary>
        /// Gets or sets the flight price.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Creates a new DestinationPrice from a FlightSearchResult.
        /// </summary>
        /// <param name="result">The flight search result.</param>
        /// <returns>A new DestinationPrice.</returns>
        public static DestinationPrice FromFlightSearchResult(FlightSearchResult result)
        {
            return new DestinationPrice
            {
                DestinationCityCode = result.DestinationCityCode,
                DestinationCountryCode = result.DestinationCountryCode,
                Price = result.Price
            };
        }
    }
}
