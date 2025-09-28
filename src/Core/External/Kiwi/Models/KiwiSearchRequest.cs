using System.Text.Json.Serialization;

namespace Core.External.Kiwi.Models
{
    /// <summary>
    /// Represents a request to the Kiwi API for flight search.
    /// </summary>
    public class KiwiSearchRequest
    {
        /// <summary>
        /// Gets or sets the origin location code(s).
        /// </summary>
        [JsonPropertyName("fly_from")]
        public string FlyFrom { get; set; }
        
        /// <summary>
        /// Gets or sets the destination location code(s).
        /// </summary>
        [JsonPropertyName("fly_to")]
        public string FlyTo { get; set; }
        
        /// <summary>
        /// Gets or sets the departure date range.
        /// Format: dd/MM/yyyy
        /// </summary>
        [JsonPropertyName("date_from")]
        public string DateFrom { get; set; }
        
        /// <summary>
        /// Gets or sets the departure date range.
        /// Format: dd/MM/yyyy
        /// </summary>
        [JsonPropertyName("date_to")]
        public string DateTo { get; set; }
        
        /// <summary>
        /// Gets or sets the return departure date range.
        /// Format: dd/MM/yyyy
        /// </summary>
        [JsonPropertyName("return_from")]
        public string ReturnFrom { get; set; }
        
        /// <summary>
        /// Gets or sets the return departure date range.
        /// Format: dd/MM/yyyy
        /// </summary>
        [JsonPropertyName("return_to")]
        public string ReturnTo { get; set; }
        
        /// <summary>
        /// Gets or sets the currency for pricing.
        /// </summary>
        [JsonPropertyName("curr")]
        public string Currency { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether to search for one-way flights.
        /// </summary>
        [JsonPropertyName("one_for_city")]
        public int OneForCity { get; set; } = 0;
        
        /// <summary>
        /// Gets or sets the maximum number of results to return.
        /// </summary>
        [JsonPropertyName("limit")]
        public int Limit { get; set; } = 100;
    }
}
