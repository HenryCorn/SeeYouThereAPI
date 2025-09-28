namespace Core.Models
{
    /// <summary>
    /// Represents a flight search result with destination and pricing information.
    /// </summary>
    public class FlightSearchResult
    {
        /// <summary>
        /// Gets or sets the origin location code.
        /// </summary>
        public string Origin { get; set; }

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
        /// Gets or sets the currency code for the price.
        /// </summary>
        public string Currency { get; set; }
    }
}
