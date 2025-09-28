namespace Core.Models
{
    /// <summary>
    /// Represents a flight search request with filtering capabilities.
    /// </summary>
    public class FlightSearchRequest
    {
        /// <summary>
        /// Gets or sets the origin location code.
        /// </summary>
        public required string Origin { get; set; }

        /// <summary>
        /// Gets or sets the destination filter by continent.
        /// </summary>
        public string? ContinentFilter { get; set; }

        /// <summary>
        /// Gets or sets the destination filter by country.
        /// </summary>
        public string? CountryFilter { get; set; }

        /// <summary>
        /// Gets or sets the list of specific destination codes to filter by.
        /// </summary>
        public List<string>? DestinationListFilter { get; set; }

        /// <summary>
        /// Gets or sets the departure date.
        /// </summary>
        public DateTime DepartureDate { get; set; }

        /// <summary>
        /// Gets or sets the return date (optional for one-way flights).
        /// </summary>
        public DateTime? ReturnDate { get; set; }

        /// <summary>
        /// Gets or sets the preferred currency code for pricing information.
        /// </summary>
        public required string Currency { get; set; }
    }
}
