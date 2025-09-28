namespace Core.Configuration
{
    /// <summary>
    /// Configuration settings for the Amadeus flight search client.
    /// </summary>
    public class AmadeusOptions
    {
        /// <summary>
        /// Gets or sets the Amadeus API key (Client ID).
        /// </summary>
        public required string ApiKey { get; set; }
        
        /// <summary>
        /// Gets or sets the Amadeus API secret (Client Secret).
        /// </summary>
        public required string ApiSecret { get; set; }

        /// <summary>
        /// Gets or sets the base URL for the Amadeus API.
        /// </summary>
        public required string BaseUrl { get; set; }

        /// <summary>
        /// Gets or sets the default currency to use for pricing.
        /// </summary>
        public required string DefaultCurrency { get; set; }

        /// <summary>
        /// Gets or sets the timeout in seconds for API requests.
        /// </summary>
        public int TimeoutSeconds { get; set; }
    }
}
