using Core.Interfaces;
using Core.Models;

namespace Core.Tests;

/// <summary>
/// A fake implementation of IFlightSearchClient for testing purposes.
/// </summary>
public class FakeFlightSearchClient : IFlightSearchClient
{
    private readonly List<FlightSearchResult> _mockResults;

    /// <summary>
    /// Initializes a new instance of the FakeFlightSearchClient class.
    /// </summary>
    public FakeFlightSearchClient()
    {
        _mockResults = new List<FlightSearchResult>
        {
            new()
            {
                Origin = "LON",
                DestinationCityCode = "PAR",
                DestinationCountryCode = "FR",
                Price = 75.99m,
                Currency = "EUR"
            },
            new()
            {
                Origin = "LON",
                DestinationCityCode = "BCN",
                DestinationCountryCode = "ES",
                Price = 89.99m,
                Currency = "EUR"
            },
            new()
            {
                Origin = "LON",
                DestinationCityCode = "ROM",
                DestinationCountryCode = "IT",
                Price = 120.50m,
                Currency = "EUR"
            }
        };
    }

    /// <inheritdoc />
    public Task<IEnumerable<FlightSearchResult>> SearchFlightsAsync(FlightSearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrEmpty(request.Origin))
            throw new ArgumentException("Origin is required", nameof(request));

        var results = _mockResults.Where(r => r.Origin == request.Origin);

        if (!string.IsNullOrEmpty(request.ContinentFilter))
        {
            // In a real implementation, this would filter by continent
            // For this mock, we'll just return all results
        }

        if (!string.IsNullOrEmpty(request.CountryFilter))
        {
            results = results.Where(r => r.DestinationCountryCode == request.CountryFilter);
        }

        if (request.DestinationListFilter != null && request.DestinationListFilter.Any())
        {
            results = results.Where(r => request.DestinationListFilter.Contains(r.DestinationCityCode));
        }

        return Task.FromResult(results);
    }

    /// <inheritdoc />
    public Task<IEnumerable<FlightSearchResult>> GetCheapestDestinationsAsync(FlightSearchRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrEmpty(request.Origin))
            throw new ArgumentException("Origin is required", nameof(request));

        var results = SearchFlightsAsync(request).Result;

        return Task.FromResult<IEnumerable<FlightSearchResult>>(results.OrderBy(r => r.Price));
    }
}