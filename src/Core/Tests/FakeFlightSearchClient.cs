using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.External;

namespace Core.Tests;

/// <summary>
/// A fake implementation of IFlightSearchClient for testing purposes.
/// </summary>
public class FakeFlightSearchClient : BaseFlightSearchClient
{
    private readonly List<FlightSearchResult> _mockResults;

    /// <summary>
    /// Initializes a new instance of the FakeFlightSearchClient class.
    /// </summary>
    public FakeFlightSearchClient(ILogger<FakeFlightSearchClient>? logger = null)
        : base(logger ?? new Microsoft.Extensions.Logging.Abstractions.NullLogger<FakeFlightSearchClient>())
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
    public override Task<IEnumerable<FlightSearchResult>> SearchFlightsAsync(
        FlightSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrEmpty(request.Origin))
            throw new ArgumentException("Origin is required", nameof(request));

        // Check if the operation has been cancelled
        cancellationToken.ThrowIfCancellationRequested();

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
    public override Task<IEnumerable<FlightSearchResult>> GetCheapestDestinationsAsync(
        FlightSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrEmpty(request.Origin))
            throw new ArgumentException("Origin is required", nameof(request));

        // Check if the operation has been cancelled
        cancellationToken.ThrowIfCancellationRequested();

        return SearchFlightsAsync(request, cancellationToken)
            .ContinueWith<IEnumerable<FlightSearchResult>>(task => task.Result.OrderBy(r => r.Price),
                cancellationToken);
    }
}
