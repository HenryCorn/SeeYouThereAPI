using Core.Configuration;
using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace Core.External.Amadeus
{
    /// <summary>
    /// Implementation of IFlightSearchClient using the Amadeus API.
    /// </summary>
    public class AmadeusFlightSearchClient : IFlightSearchClient
    {
        private readonly HttpClient _httpClient;
        private readonly AmadeusOptions _options;
        private readonly ILogger<AmadeusFlightSearchClient> _logger;
        private string _accessToken = null!;
        private DateTime _tokenExpiry = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="AmadeusFlightSearchClient"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client to use for API calls.</param>
        /// <param name="options">The options for the Amadeus flight search client.</param>
        /// <param name="logger">The logger.</param>
        public AmadeusFlightSearchClient(
            HttpClient httpClient,
            IOptions<AmadeusOptions> options,
            ILogger<AmadeusFlightSearchClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Configure the HttpClient
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<FlightSearchResult>> SearchFlightsAsync(FlightSearchRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrEmpty(request.Origin))
                throw new ArgumentException("Origin is required", nameof(request));

            try
            {
                try
                {
                    _logger.LogInformation("Ensuring valid access token for Amadeus API");
                    await EnsureValidAccessTokenAsync();
                }
                catch (Exception tokenEx)
                {
                    _logger.LogError(tokenEx, "Failed to obtain Amadeus access token: {Message}", tokenEx.Message);
                    throw new Exception("Failed to authenticate with the flight search provider. Please check API credentials.", tokenEx);
                }

                var searchUrl = BuildSearchUrl(request);
                _logger.LogInformation("Searching flights from {Origin} with Amadeus API. URL: {Url}", request.Origin, searchUrl);

                using var requestMessage = new HttpRequestMessage(HttpMethod.Get, searchUrl);
                requestMessage.Headers.Add("Authorization", $"Bearer {_accessToken}");
                
                _logger.LogInformation("Sending request to Amadeus API");
                var response = await _httpClient.SendAsync(requestMessage);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Amadeus API returned error: Status {Status}, Content: {Content}",
                        (int)response.StatusCode, errorContent);
                    throw new Exception($"Flight search provider returned error {(int)response.StatusCode}: {errorContent}");
                }
                
                _logger.LogInformation("Received successful response from Amadeus API");
                
                string responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Response content: {Content}", responseContent);
                
                var amadeusResponse = JsonSerializer.Deserialize<AmadeusFlightOffersResponse>(responseContent);
                
                if (amadeusResponse == null)
                {
                    _logger.LogError("Failed to deserialize Amadeus API response");
                    throw new Exception("Failed to process flight search results");
                }
                
                var results = MapFromAmadeusResponse(amadeusResponse, request.Currency);
                var flightSearchResults = results.ToList();
                _logger.LogInformation("Mapped {Count} flight search results", flightSearchResults.Count());

                return flightSearchResults;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error calling Amadeus API: {Message}", ex.Message);
                throw new Exception($"Error searching flights: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing Amadeus API response: {Message}", ex.Message);
                throw new Exception("Error processing flight search results", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in flight search: {Message}", ex.Message);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<FlightSearchResult>> GetCheapestDestinationsAsync(FlightSearchRequest request)
        {
            var results = await SearchFlightsAsync(request);
            return results.OrderBy(r => r.Price);
        }

        private async Task EnsureValidAccessTokenAsync()
        {
            // Check if we need a new token
            if (string.IsNullOrEmpty(_accessToken) || DateTime.UtcNow >= _tokenExpiry)
            {
                _logger.LogInformation("Getting new Amadeus access token");
                
                var tokenUrl = "/v1/security/oauth2/token";
                var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = _options.ApiKey,
                    ["client_secret"] = _options.ApiSecret
                });

                try
                {
                    var tokenResponse = await _httpClient.PostAsync(tokenUrl, tokenRequest);
                    
                    if (!tokenResponse.IsSuccessStatusCode)
                    {
                        var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                        _logger.LogError("Failed to obtain token: Status {Status}, Content: {Content}", 
                            (int)tokenResponse.StatusCode, errorContent);
                        throw new Exception($"Failed to authenticate: {errorContent}");
                    }
                    
                    var responseContent = await tokenResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation("Token response: {Response}", responseContent);
                    
                    var tokenResult = JsonSerializer.Deserialize<AmadeusTokenResponse>(responseContent);
                    
                    if (tokenResult == null || string.IsNullOrEmpty(tokenResult.AccessToken))
                        throw new Exception("Failed to obtain access token from Amadeus");

                    _accessToken = tokenResult.AccessToken;
                    _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResult.ExpiresIn - 60); // Buffer of 60 seconds
                    
                    _logger.LogInformation("Successfully obtained Amadeus access token, expires at {Expiry}", _tokenExpiry);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error obtaining Amadeus access token: {Message}", ex.Message);
                    throw new Exception("Failed to authenticate with Amadeus API", ex);
                }
            }
        }

        private string BuildSearchUrl(FlightSearchRequest request)
        {
            var currency = !string.IsNullOrEmpty(request.Currency) ? request.Currency : _options.DefaultCurrency;
            var departureDate = request.DepartureDate.ToString("yyyy-MM-dd");

            var sb = new StringBuilder("/v2/shopping/flight-offers?");
            sb.Append($"originLocationCode={request.Origin}");

            // Handle destination filtering
            if (!string.IsNullOrEmpty(request.CountryFilter))
            {
                // For Amadeus, we need to provide a specific airport code
                // This is a simplification - in a real app, you'd need to convert country code to airport codes
                sb.Append($"&destinationLocationCode={request.CountryFilter}");
            }
            else if (request.DestinationListFilter != null && request.DestinationListFilter.Any())
            {
                // Take the first destination for simplicity
                // In a real app, you'd need to make multiple requests for each destination
                sb.Append($"&destinationLocationCode={request.DestinationListFilter.First()}");
            }
            else if (!string.IsNullOrEmpty(request.ContinentFilter))
            {
                // This is a simplification - Amadeus doesn't support continent filtering directly
                // In a real app, you'd need to convert continent to country/city codes
                _logger.LogWarning("Continent filtering not directly supported by Amadeus API");
            }

            sb.Append($"&departureDate={departureDate}");
            sb.Append($"&adults=1"); // Default to one adult
            sb.Append($"&currencyCode={currency}");
            sb.Append("&max=100"); // Limit results

            // Add return date if provided
            if (request.ReturnDate.HasValue)
            {
                var returnDate = request.ReturnDate.Value.ToString("yyyy-MM-dd");
                sb.Append($"&returnDate={returnDate}");
            }

            return sb.ToString();
        }

        private IEnumerable<FlightSearchResult> MapFromAmadeusResponse(AmadeusFlightOffersResponse response,
            string requestCurrency)
        {
            if (!response.Data.Any())
                return [];

            var groupedResults = new Dictionary<string, FlightSearchResult>();
            var currency = !string.IsNullOrEmpty(requestCurrency) ? requestCurrency : _options.DefaultCurrency;

            foreach (var offer in response.Data)
            {
                if (!offer.Itineraries.Any() || !offer.Itineraries[0].Segments.Any())
                    continue;

                var firstSegment = offer.Itineraries[0].Segments[0];
                var origin = firstSegment.Departure.IataCode;
                var destination = firstSegment.Arrival.IataCode;

                string? destinationCountryCode = null;
                if (response.Dictionaries?.Locations != null &&
                    response.Dictionaries.Locations.TryGetValue(destination, out var locationData))
                {
                    destinationCountryCode = locationData.CountryCode;
                }

                if (string.IsNullOrEmpty(origin) || string.IsNullOrEmpty(destination))
                    continue;

                if (!groupedResults.TryGetValue(destination, out var existingResult) ||
                    decimal.Parse(offer.Price.Total) < existingResult.Price)
                {
                    groupedResults[destination] = new FlightSearchResult
                    {
                        Origin = origin,
                        DestinationCityCode = destination,
                        DestinationCountryCode = destinationCountryCode!,
                        Price = decimal.Parse(offer.Price.Total),
                        Currency = currency
                    };
                }
            }

            return groupedResults.Values;
        }
    }

    #region Amadeus Models

    public class AmadeusTokenResponse
    {
        [JsonPropertyName("access_token")] public required string AccessToken { get; set; }

        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    }

    public class AmadeusFlightOffersResponse
    {
        [JsonPropertyName("data")] public required List<AmadeusFlightOffer> Data { get; set; }

        [JsonPropertyName("dictionaries")] public required AmadeusDictionaries Dictionaries { get; set; }
    }

    public class AmadeusDictionaries
    {
        [JsonPropertyName("locations")] public required Dictionary<string, AmadeusLocation> Locations { get; set; }
    }

    public class AmadeusLocation
    {
        [JsonPropertyName("countryCode")] public required string CountryCode { get; set; }
    }

    public class AmadeusFlightOffer
    {
        [JsonPropertyName("id")] public string Id { get; set; }

        [JsonPropertyName("price")] public AmadeusPrice Price { get; set; }

        [JsonPropertyName("itineraries")] public List<AmadeusItinerary> Itineraries { get; set; }
    }

    public class AmadeusPrice
    {
        [JsonPropertyName("currency")] public string Currency { get; set; }

        [JsonPropertyName("total")] public string Total { get; set; }
    }

    public class AmadeusItinerary
    {
        [JsonPropertyName("segments")] public List<AmadeusSegment> Segments { get; set; }
    }

    public class AmadeusSegment
    {
        [JsonPropertyName("departure")] public AmadeusLocationPoint Departure { get; set; }

        [JsonPropertyName("arrival")] public AmadeusLocationPoint Arrival { get; set; }
    }

    public class AmadeusLocationPoint
    {
        [JsonPropertyName("iataCode")] public string IataCode { get; set; }
    }

    #endregion
}
