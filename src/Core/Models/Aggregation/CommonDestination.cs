using System.Collections.Generic;

namespace Core.Models.Aggregation
{
    /// <summary>
    /// Represents a destination with aggregated price information from multiple origins.
    /// </summary>
    public class CommonDestination
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
        /// Gets or sets the total price across all origins.
        /// </summary>
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// Gets or sets the currency of the prices.
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// Gets or sets the price breakdown by origin.
        /// </summary>
        public Dictionary<string, decimal> PerOriginPrices { get; set; } = new();
    }
}
