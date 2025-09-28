using System.Collections.Generic;
using System.Linq;

namespace Core.Models.Aggregation
{
    /// <summary>
    /// Represents aggregated flight data across multiple origins and destinations.
    /// </summary>
    public class FlightAggregation
    {
        /// <summary>
        /// Gets or sets the collection of per-origin price data.
        /// </summary>
        public Dictionary<string, OriginDestinationPrices> OriginData { get; set; } = new();

        /// <summary>
        /// Gets or sets the currency used for aggregated prices.
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// Gets the collection of destinations available from all origins.
        /// </summary>
        /// <returns>A set of destination city codes available from all origins.</returns>
        public HashSet<string> GetCommonDestinations()
        {
            if (!OriginData.Any())
                return new HashSet<string>();

            // Start with all destinations from the first origin
            var firstOrigin = OriginData.First().Value;
            var commonDestinations = new HashSet<string>(firstOrigin.Destinations.Keys);

            // Intersect with destinations from all other origins
            foreach (var origin in OriginData.Skip(1))
            {
                var originDestinations = new HashSet<string>(origin.Value.Destinations.Keys);
                commonDestinations.IntersectWith(originDestinations);
            }

            return commonDestinations;
        }

        /// <summary>
        /// Calculates the cheapest common destination across all origins.
        /// </summary>
        /// <returns>The destination with the lowest total price across all origins.</returns>
        public CommonDestination GetCheapestCommonDestination()
        {
            var commonDestinations = GetCommonDestinations();

            if (!commonDestinations.Any())
                return null;

            var destinationTotals = new Dictionary<string, CommonDestination>();

            // Calculate totals for each common destination
            foreach (var destination in commonDestinations)
            {
                var commonDest = new CommonDestination
                {
                    DestinationCityCode = destination,
                    Currency = Currency,
                    PerOriginPrices = new Dictionary<string, decimal>()
                };

                decimal totalPrice = 0;

                foreach (var origin in OriginData)
                {
                    string originCode = origin.Key;
                    var originPrices = origin.Value;

                    if (originPrices.Destinations.TryGetValue(destination, out var destPrice))
                    {
                        commonDest.PerOriginPrices[originCode] = destPrice.Price;
                        totalPrice += destPrice.Price;

                        // Get the country code from the first available data point
                        if (string.IsNullOrEmpty(commonDest.DestinationCountryCode))
                        {
                            commonDest.DestinationCountryCode = destPrice.DestinationCountryCode;
                        }
                    }
                }

                commonDest.TotalPrice = totalPrice;
                destinationTotals[destination] = commonDest;
            }

            // Find the cheapest destination
            return destinationTotals.Values
                .OrderBy(d => d.TotalPrice)
                .FirstOrDefault();
        }
    }
}
