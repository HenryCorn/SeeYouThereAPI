using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Logging;

namespace Core.External.Amadeus.Testing
{
    /// <summary>
    /// Implementation of IFlightSearchClient that provides test data for development and testing.
    /// </summary>
    public class TestFlightSearchClient : IFlightSearchClient
    {
        private readonly ILogger<TestFlightSearchClient> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestFlightSearchClient"/> class.
        /// </summary>
        public TestFlightSearchClient(ILogger<TestFlightSearchClient> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public Task<IEnumerable<FlightSearchResult>> SearchFlightsAsync(FlightSearchRequest request)
        {
            _logger.LogInformation("Using test flight search client for origin {Origin}", request.Origin);

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrEmpty(request.Origin))
                throw new ArgumentException("Origin is required", nameof(request));

            var results = GenerateTestResults(request);
            return Task.FromResult(results);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<FlightSearchResult>> GetCheapestDestinationsAsync(FlightSearchRequest request)
        {
            var results = GenerateTestResults(request);
            return Task.FromResult<IEnumerable<FlightSearchResult>>(results.OrderBy(r => r.Price));
        }

        private IEnumerable<FlightSearchResult> GenerateTestResults(FlightSearchRequest request)
        {
            var random = new Random(request.Origin.GetHashCode());
            var results = new List<FlightSearchResult>();
            var currency = !string.IsNullOrEmpty(request.Currency) ? request.Currency : "USD";

            var destinations = GetTestDestinations(request);

            _logger.LogDebug("Generating {Count} test destinations for origin {Origin}", destinations.Count, request.Origin);

            foreach (var (code, country) in destinations)
            {
                var price = Math.Round((decimal)(random.NextDouble() * 500 + 100), 2);
                results.Add(new FlightSearchResult
                {
                    Origin = request.Origin,
                    DestinationCityCode = code,
                    DestinationCountryCode = country,
                    Price = price,
                    Currency = currency
                });

                _logger.LogDebug("Generated test flight: {Origin} → {Destination} ({Country}): {Price} {Currency}",
                    request.Origin, code, country, price, currency);
            }

            _logger.LogInformation("Generated {Count} test flight results for {Origin}", results.Count, request.Origin);
            return results;
        }

        private List<(string Code, string Country)> GetTestDestinations(FlightSearchRequest request)
        {
            var europeanDestinations = new List<(string Code, string Country)>
            {
                ("CDG", "FR"), // Paris
                ("FCO", "IT"), // Rome
                ("MAD", "ES"), // Madrid
                ("AMS", "NL"), // Amsterdam
                ("ATH", "GR"), // Athens
                ("LIS", "PT"), // Lisbon
                ("ZRH", "CH"), // Zurich
                ("VIE", "AT"), // Vienna
                ("CPH", "DK"), // Copenhagen
                ("ARN", "SE"), // Stockholm
                ("TXL", "DE"), // Berlin
                ("DUB", "IE")  // Dublin
            };

            if (!string.IsNullOrEmpty(request.ContinentFilter))
            {
                if (request.ContinentFilter.ToUpperInvariant() == "EU")
                {
                    return europeanDestinations;
                }

                return request.ContinentFilter.ToUpperInvariant() switch
                {
                    "NA" => new List<(string, string)> { ("JFK", "US"), ("ORD", "US"), ("YYZ", "CA"), ("MEX", "MX") },
                    "SA" => new List<(string, string)> { ("GRU", "BR"), ("EZE", "AR"), ("BOG", "CO"), ("SCL", "CL") },
                    "AS" => new List<(string, string)> { ("HND", "JP"), ("PEK", "CN"), ("SIN", "SG"), ("DEL", "IN") },
                    "AF" => new List<(string, string)> { ("JNB", "ZA"), ("CAI", "EG"), ("NBO", "KE"), ("LOS", "NG") },
                    "OC" => new List<(string, string)> { ("SYD", "AU"), ("AKL", "NZ"), ("NAN", "FJ") },
                    _ => europeanDestinations
                };
            }

            if (!string.IsNullOrEmpty(request.CountryFilter))
            {
                return request.CountryFilter.ToUpperInvariant() switch
                {
                    "FR" => new List<(string, string)> { ("CDG", "FR"), ("NCE", "FR"), ("LYS", "FR"), ("MRS", "FR") },
                    "IT" => new List<(string, string)> { ("FCO", "IT"), ("MXP", "IT"), ("VCE", "IT"), ("NAP", "IT") },
                    "ES" => new List<(string, string)> { ("MAD", "ES"), ("BCN", "ES"), ("AGP", "ES"), ("IBZ", "ES") },
                    "DE" => new List<(string, string)> { ("TXL", "DE"), ("MUC", "DE"), ("FRA", "DE"), ("DUS", "DE") },
                    _ => new List<(string, string)> { (request.CountryFilter, request.CountryFilter) }
                };
            }
            {
                if (request.DestinationListFilter != null && request.DestinationListFilter.Any())
                {
                    return request.DestinationListFilter
                        .Select(d => (d, d.Substring(0, 2)))
                        .ToList();
                }
                return europeanDestinations;
            }
        }
    }
}
