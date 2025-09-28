using Core.Configuration;
using Core.External.Kiwi.Models;
using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Web;

namespace Core.External.Kiwi
{
    /// <summary>
    /// Implementation of IFlightSearchClient that uses the Kiwi API.
    /// </summary>
    public class KiwiFlightSearchClient : BaseFlightSearchClient
    {
        private readonly HttpClient _httpClient;
        private readonly KiwiFlightSearchOptions _options;
        private new readonly ILogger<KiwiFlightSearchClient> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="KiwiFlightSearchClient"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client to use for API calls.</param>
        /// <param name="options">The options for the Kiwi flight search client.</param>
        /// <param name="logger">The logger.</param>
        public KiwiFlightSearchClient(
            HttpClient httpClient,
            IOptions<KiwiFlightSearchOptions> options,
            ILogger<KiwiFlightSearchClient> logger)
            : base(logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;

            // Configure the HttpClient
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("apikey", _options.ApiKey);
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        }

        /// <inheritdoc/>
        public override async Task<IEnumerable<FlightSearchResult>> SearchFlightsAsync(
            FlightSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrEmpty(request.Origin))
                throw new ArgumentException("Origin is required", nameof(request));

            try
            {
                var kiwiRequest = MapToKiwiRequest(request);
                var queryParams = BuildQueryString(kiwiRequest);

                _logger.LogInformation("Searching flights from {Origin} with Kiwi API", request.Origin);

                var response = await _httpClient.GetAsync($"/v2/search?{queryParams}", cancellationToken);

                response.EnsureSuccessStatusCode();

                var kiwiResponse = await response.Content.ReadFromJsonAsync<KiwiSearchResponse>(cancellationToken: cancellationToken);

                return MapFromKiwiResponse(kiwiResponse);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error calling Kiwi API: {Message}", ex.Message);
                throw new Exception($"Error searching flights: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing Kiwi API response: {Message}", ex.Message);
                throw new Exception("Error processing flight search results", ex);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Flight search was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in flight search: {Message}", ex.Message);
                throw;
            }
        }

        /// <inheritdoc/>
        public override async Task<IEnumerable<FlightSearchResult>> GetCheapestDestinationsAsync(
            FlightSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var results = await SearchFlightsAsync(request, cancellationToken);
            return results.OrderBy(r => r.Price);
        }

        private KiwiSearchRequest MapToKiwiRequest(FlightSearchRequest request)
        {
            // Format dates for Kiwi API (dd/MM/yyyy)
            string departureDate = request.DepartureDate.ToString("dd/MM/yyyy");

            // Initialize flyTo with any specific filters
            string flyTo = string.Empty;

            // Apply continent filter
            if (!string.IsNullOrEmpty(request.ContinentFilter))
            {
                flyTo += $"{request.ContinentFilter}-";
            }

            // Apply country filter
            if (!string.IsNullOrEmpty(request.CountryFilter))
            {
                flyTo += request.CountryFilter;
            }
            // Apply destination list filter
            else if (request.DestinationListFilter != null && request.DestinationListFilter.Any())
            {
                flyTo = string.Join(",", request.DestinationListFilter);
            }

            // If no filter is specified, search everywhere
            if (string.IsNullOrEmpty(flyTo))
            {
                flyTo = "anywhere";
            }

            var kiwiRequest = new KiwiSearchRequest
            {
                FlyFrom = request.Origin,
                FlyTo = flyTo,
                DateFrom = departureDate,
                DateTo = departureDate, // Same date for now (could expand for date range)
                Currency = !string.IsNullOrEmpty(request.Currency) ?
                           request.Currency :
                           _options.DefaultCurrency
            };

            // Add return dates if this is a round trip
            if (request.ReturnDate.HasValue)
            {
                string returnDate = request.ReturnDate.Value.ToString("dd/MM/yyyy");
                kiwiRequest.ReturnFrom = returnDate;
                kiwiRequest.ReturnTo = returnDate;
            }

            return kiwiRequest;
        }

        private IEnumerable<FlightSearchResult> MapFromKiwiResponse(KiwiSearchResponse response)
        {
            if (response?.Data == null)
                return Enumerable.Empty<FlightSearchResult>();

            return response.Data.Select(d => new FlightSearchResult
            {
                Origin = d.FlyFrom,
                DestinationCityCode = d.FlyTo,
                DestinationCountryCode = d.CountryTo?.Code,
                Price = d.Price,
                Currency = response.Currency
            });
        }

        private string BuildQueryString(KiwiSearchRequest request)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);

            // Add all properties with JsonPropertyName attributes
            var properties = typeof(KiwiSearchRequest).GetProperties();
            foreach (var prop in properties)
            {
                var value = prop.GetValue(request);
                if (value != null)
                {
                    var jsonPropNameAttr = prop.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute), false)
                        .FirstOrDefault() as System.Text.Json.Serialization.JsonPropertyNameAttribute;

                    if (jsonPropNameAttr != null)
                    {
                        query[jsonPropNameAttr.Name] = value.ToString();
                    }
                }
            }

            return query.ToString();
        }
    }
}
