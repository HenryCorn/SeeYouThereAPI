using Core.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    /// <summary>
    /// Defines the contract for a flight search service.
    /// </summary>
    /// <remarks>
    /// This interface abstracts the underlying flight search provider, allowing for
    /// different implementations (like Skyscanner, Amadeus, Kiwi, etc.) while maintaining
    /// a consistent API for the application.
    /// </remarks>
    public interface IFlightSearchClient
    {
        /// <summary>
        /// Searches for flights based on the provided criteria.
        /// </summary>
        /// <param name="request">The search parameters including origin, destination filters, dates, and currency.</param>
        /// <param name="cancellationToken">A token for cancelling the operation.</param>
        /// <returns>A collection of flight search results matching the criteria.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the request is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown when required fields in the request are missing or invalid.</exception>
        Task<IEnumerable<FlightSearchResult>> SearchFlightsAsync(FlightSearchRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the cheapest destinations from the specified origin.
        /// </summary>
        /// <param name="request">The search parameters including origin, destination filters, dates, and currency.</param>
        /// <param name="cancellationToken">A token for cancelling the operation.</param>
        /// <returns>A collection of flight search results ordered by price.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the request is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown when required fields in the request are missing or invalid.</exception>
        Task<IEnumerable<FlightSearchResult>> GetCheapestDestinationsAsync(FlightSearchRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for flights from multiple origins based on the provided criteria.
        /// Requests are executed in parallel with rate limiting enforced.
        /// </summary>
        /// <param name="request">The search parameters including origins, destination filters, dates, and currency.</param>
        /// <param name="cancellationToken">A token for cancelling the operation.</param>
        /// <returns>A collection of flight search results from all origins matching the criteria.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the request is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown when required fields in the request are missing or invalid.</exception>
        Task<IEnumerable<FlightSearchResult>> SearchFlightsFromMultipleOriginsAsync(MultiOriginFlightSearchRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the cheapest destinations from multiple specified origins.
        /// Requests are executed in parallel with rate limiting enforced.
        /// </summary>
        /// <param name="request">The search parameters including origins, destination filters, dates, and currency.</param>
        /// <param name="cancellationToken">A token for cancelling the operation.</param>
        /// <returns>A collection of flight search results from all origins ordered by price.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the request is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown when required fields in the request are missing or invalid.</exception>
        Task<IEnumerable<FlightSearchResult>> GetCheapestDestinationsFromMultipleOriginsAsync(MultiOriginFlightSearchRequest request, CancellationToken cancellationToken = default);
    }
}
