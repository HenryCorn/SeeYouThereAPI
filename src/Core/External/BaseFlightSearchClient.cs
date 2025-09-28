using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.External
{
    /// <summary>
    /// Base implementation for flight search clients that provides common functionality.
    /// </summary>
    public abstract class BaseFlightSearchClient : IFlightSearchClient
    {
        protected readonly ILogger _logger;

        protected BaseFlightSearchClient(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Searches for flights based on the provided criteria.
        /// </summary>
        public abstract Task<IEnumerable<FlightSearchResult>> SearchFlightsAsync(
            FlightSearchRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the cheapest destinations from the specified origin.
        /// </summary>
        public abstract Task<IEnumerable<FlightSearchResult>> GetCheapestDestinationsAsync(
            FlightSearchRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for flights from multiple origins based on the provided criteria.
        /// Requests are executed in parallel with rate limiting enforced.
        /// </summary>
        public virtual async Task<IEnumerable<FlightSearchResult>> SearchFlightsFromMultipleOriginsAsync(
            MultiOriginFlightSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Origins == null || !request.Origins.Any())
                throw new ArgumentException("At least one origin must be provided.", nameof(request.Origins));

            var searchTasks = request.Origins.Select(origin =>
            {
                var singleRequest = ConvertToSingleOriginRequest(request, origin);
                return SearchFlightsAsync(singleRequest, cancellationToken);
            }).ToArray();

            try
            {
                _logger.LogInformation("Starting parallel flight search for {OriginCount} origins", request.Origins.Count);
                var results = await Task.WhenAll(searchTasks);
                return results.SelectMany(r => r).ToList();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error occurred during parallel flight search from multiple origins");
                throw;
            }
        }

        /// <summary>
        /// Gets the cheapest destinations from multiple specified origins.
        /// Requests are executed in parallel with rate limiting enforced.
        /// </summary>
        public virtual async Task<IEnumerable<FlightSearchResult>> GetCheapestDestinationsFromMultipleOriginsAsync(
            MultiOriginFlightSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Origins == null || !request.Origins.Any())
                throw new ArgumentException("At least one origin must be provided.", nameof(request.Origins));

            var searchTasks = request.Origins.Select(origin =>
            {
                var singleRequest = ConvertToSingleOriginRequest(request, origin);
                return GetCheapestDestinationsAsync(singleRequest, cancellationToken);
            }).ToArray();

            try
            {
                _logger.LogInformation("Starting parallel cheapest destinations search for {OriginCount} origins", request.Origins.Count);
                var results = await Task.WhenAll(searchTasks);
                return results.SelectMany(r => r).ToList();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error occurred during parallel cheapest destinations search from multiple origins");
                throw;
            }
        }

        /// <summary>
        /// Converts a multi-origin request to a single-origin request for a specific origin.
        /// </summary>
        private static FlightSearchRequest ConvertToSingleOriginRequest(MultiOriginFlightSearchRequest multiRequest, string origin)
        {
            return new FlightSearchRequest
            {
                Origin = origin,
                ContinentFilter = multiRequest.ContinentFilter,
                CountryFilter = multiRequest.CountryFilter,
                DestinationListFilter = multiRequest.DestinationListFilter,
                DepartureDate = multiRequest.DepartureDate,
                ReturnDate = multiRequest.ReturnDate,
                Currency = multiRequest.Currency
            };
        }
    }
}
