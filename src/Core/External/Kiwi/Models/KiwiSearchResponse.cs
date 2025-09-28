using System.Text.Json.Serialization;

namespace Core.External.Kiwi.Models
{
    /// <summary>
    /// Represents a response from the Kiwi API for flight search.
    /// </summary>
    public class KiwiSearchResponse
    {
        /// <summary>
        /// Gets or sets the search identifier.
        /// </summary>
        [JsonPropertyName("search_id")]
        public string SearchId { get; set; }

        /// <summary>
        /// Gets or sets the currency used for pricing.
        /// </summary>
        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        /// <summary>
        /// Gets or sets the flight data results.
        /// </summary>
        [JsonPropertyName("data")]
        public List<KiwiFlightData> Data { get; set; }
    }

    /// <summary>
    /// Represents flight data from the Kiwi API.
    /// </summary>
    public class KiwiFlightData
    {
        /// <summary>
        /// Gets or sets the flight identifier.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the flight price.
        /// </summary>
        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the routes for this flight option.
        /// </summary>
        [JsonPropertyName("route")]
        public List<KiwiRouteData> Route { get; set; }

        /// <summary>
        /// Gets or sets the cityFrom.
        /// </summary>
        [JsonPropertyName("cityFrom")]
        public string CityFrom { get; set; }

        /// <summary>
        /// Gets or sets the cityTo.
        /// </summary>
        [JsonPropertyName("cityTo")]
        public string CityTo { get; set; }

        /// <summary>
        /// Gets or sets the flyFrom code.
        /// </summary>
        [JsonPropertyName("flyFrom")]
        public string FlyFrom { get; set; }

        /// <summary>
        /// Gets or sets the flyTo code.
        /// </summary>
        [JsonPropertyName("flyTo")]
        public string FlyTo { get; set; }

        /// <summary>
        /// Gets or sets the country code for the destination.
        /// </summary>
        [JsonPropertyName("countryTo")]
        public KiwiCountryData CountryTo { get; set; }
    }

    /// <summary>
    /// Represents route data for a flight segment.
    /// </summary>
    public class KiwiRouteData
    {
        /// <summary>
        /// Gets or sets the identifier for this route segment.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the origin airport code.
        /// </summary>
        [JsonPropertyName("flyFrom")]
        public string FlyFrom { get; set; }

        /// <summary>
        /// Gets or sets the destination airport code.
        /// </summary>
        [JsonPropertyName("flyTo")]
        public string FlyTo { get; set; }

        /// <summary>
        /// Gets or sets the origin city name.
        /// </summary>
        [JsonPropertyName("cityFrom")]
        public string CityFrom { get; set; }

        /// <summary>
        /// Gets or sets the destination city name.
        /// </summary>
        [JsonPropertyName("cityTo")]
        public string CityTo { get; set; }

        /// <summary>
        /// Gets or sets the local departure time.
        /// </summary>
        [JsonPropertyName("local_departure")]
        public DateTime LocalDeparture { get; set; }

        /// <summary>
        /// Gets or sets the local arrival time.
        /// </summary>
        [JsonPropertyName("local_arrival")]
        public DateTime LocalArrival { get; set; }
    }

    /// <summary>
    /// Represents country data.
    /// </summary>
    public class KiwiCountryData
    {
        /// <summary>
        /// Gets or sets the country code.
        /// </summary>
        [JsonPropertyName("code")]
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the country name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
