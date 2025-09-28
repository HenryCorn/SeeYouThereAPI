namespace Core.Configuration
{
    /// <summary>
    /// Configuration settings for the Kiwi flight search client.
    /// </summary>
    public class KiwiFlightSearchOptions
    {
        /// <summary>
        /// Gets or sets the Kiwi API key.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the base URL for the Kiwi API.
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// Gets or sets the default currency to use for pricing.
        /// </summary>
        public string DefaultCurrency { get; set; }

        /// <summary>
        /// Gets or sets the timeout in seconds for API requests.
        /// </summary>
        public int TimeoutSeconds { get; set; }
    }
}
