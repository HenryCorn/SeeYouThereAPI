// <copyright file="CachedFlightSearchClient.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Core.External
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Configuration;
    using Core.Infrastructure;
    using Core.Interfaces;
    using Core.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Decorator for IFlightSearchClient that adds caching functionality.
    /// </summary>
    public class CachedFlightSearchClient : IFlightSearchClient
    {
        private readonly IFlightSearchClient _inner;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachedFlightSearchClient> _logger;
        private readonly CacheOptions _cacheOptions;
        private readonly ICacheHeadersService _cacheHeadersService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedFlightSearchClient"/> class.
        /// </summary>
        /// <param name="inner">The decorated flight search client.</param>
        /// <param name="cache">The memory cache instance.</param>
        /// <param name="cacheOptions">The cache configuration options.</param>
        /// <param name="cacheHeadersService">The cache headers service.</param>
        /// <param name="logger">The logger.</param>
        public CachedFlightSearchClient(
            IFlightSearchClient inner,
            IMemoryCache cache,
            IOptions<CacheOptions> cacheOptions,
            ICacheHeadersService cacheHeadersService,
            ILogger<CachedFlightSearchClient> logger)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheOptions = cacheOptions?.Value ?? throw new ArgumentNullException(nameof(cacheOptions));
            _cacheHeadersService = cacheHeadersService ?? throw new ArgumentNullException(nameof(cacheHeadersService));
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<FlightSearchResult>> SearchFlightsAsync(
            FlightSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!_cacheOptions.Enabled || _cacheHeadersService.HasNoCacheHeader())
            {
                _logger.LogDebug("Cache bypassed for flight search request");
                return await _inner.SearchFlightsAsync(request, cancellationToken);
            }

            var cacheKey = GenerateCacheKey("SearchFlights", request);
            if (_cache.TryGetValue(cacheKey, out IEnumerable<FlightSearchResult> cachedResults))
            {
                _logger.LogInformation("Cache hit for flight search request {CacheKey}", cacheKey);
                return cachedResults;
            }

            _logger.LogDebug("Cache miss for flight search request {CacheKey}", cacheKey);
            var results = await _inner.SearchFlightsAsync(request, cancellationToken);
            var resultsList = results.ToList();

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(_cacheOptions.FlightSearchCacheTtlMinutes));

            _cache.Set(cacheKey, resultsList, cacheEntryOptions);
            _logger.LogDebug("Cached flight search results for {CacheKey}", cacheKey);

            return resultsList;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<FlightSearchResult>> GetCheapestDestinationsAsync(
            FlightSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!_cacheOptions.Enabled || _cacheHeadersService.HasNoCacheHeader())
            {
                _logger.LogDebug("Cache bypassed for cheapest destinations request");
                return await _inner.GetCheapestDestinationsAsync(request, cancellationToken);
            }

            var cacheKey = GenerateCacheKey("CheapestDestinations", request);
            if (_cache.TryGetValue(cacheKey, out IEnumerable<FlightSearchResult> cachedResults))
            {
                _logger.LogInformation("Cache hit for cheapest destinations request {CacheKey}", cacheKey);
                return cachedResults;
            }

            _logger.LogDebug("Cache miss for cheapest destinations request {CacheKey}", cacheKey);
            var results = await _inner.GetCheapestDestinationsAsync(request, cancellationToken);
            var resultsList = results.ToList();

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(_cacheOptions.FlightSearchCacheTtlMinutes));

            _cache.Set(cacheKey, resultsList, cacheEntryOptions);
            _logger.LogDebug("Cached cheapest destinations results for {CacheKey}", cacheKey);

            return resultsList;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<FlightSearchResult>> SearchFlightsFromMultipleOriginsAsync(
            MultiOriginFlightSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!_cacheOptions.Enabled || _cacheHeadersService.HasNoCacheHeader())
            {
                _logger.LogDebug("Cache bypassed for multiple origins flight search request");
                return await _inner.SearchFlightsFromMultipleOriginsAsync(request, cancellationToken);
            }

            var cacheKey = GenerateCacheKey("SearchFlightsMultipleOrigins", request);
            if (_cache.TryGetValue(cacheKey, out IEnumerable<FlightSearchResult> cachedResults))
            {
                _logger.LogInformation("Cache hit for multiple origins flight search request {CacheKey}", cacheKey);
                return cachedResults;
            }

            _logger.LogDebug("Cache miss for multiple origins flight search request {CacheKey}", cacheKey);
            var results = await _inner.SearchFlightsFromMultipleOriginsAsync(request, cancellationToken);
            var resultsList = results.ToList();

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(_cacheOptions.FlightSearchCacheTtlMinutes));

            _cache.Set(cacheKey, resultsList, cacheEntryOptions);
            _logger.LogDebug("Cached multiple origins flight search results for {CacheKey}", cacheKey);

            return resultsList;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<FlightSearchResult>> GetCheapestDestinationsFromMultipleOriginsAsync(
            MultiOriginFlightSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!_cacheOptions.Enabled || _cacheHeadersService.HasNoCacheHeader())
            {
                _logger.LogDebug("Cache bypassed for multiple origins cheapest destinations request");
                return await _inner.GetCheapestDestinationsFromMultipleOriginsAsync(request, cancellationToken);
            }

            var cacheKey = GenerateCacheKey("CheapestDestinationsMultipleOrigins", request);
            if (_cache.TryGetValue(cacheKey, out IEnumerable<FlightSearchResult> cachedResults))
            {
                _logger.LogInformation("Cache hit for multiple origins cheapest destinations request {CacheKey}", cacheKey);
                return cachedResults;
            }

            _logger.LogDebug("Cache miss for multiple origins cheapest destinations request {CacheKey}", cacheKey);
            var results = await _inner.GetCheapestDestinationsFromMultipleOriginsAsync(request, cancellationToken);
            var resultsList = results.ToList();

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(_cacheOptions.FlightSearchCacheTtlMinutes));

            _cache.Set(cacheKey, resultsList, cacheEntryOptions);
            _logger.LogDebug("Cached multiple origins cheapest destinations results for {CacheKey}", cacheKey);

            return resultsList;
        }

        private static string GenerateCacheKey(string operation, FlightSearchRequest request)
        {
            var key = new StringBuilder();
            key.Append(operation);
            key.Append(':');
            key.Append(request.Origin);
            key.Append(':');
            key.Append(request.ContinentFilter ?? "null");
            key.Append(':');
            key.Append(request.CountryFilter ?? "null");
            key.Append(':');

            if (request.DestinationListFilter != null && request.DestinationListFilter.Any())
            {
                key.Append(string.Join(",", request.DestinationListFilter.OrderBy(d => d)));
            }
            else
            {
                key.Append("null");
            }

            key.Append(':');
            key.Append(request.DepartureDate.ToString("yyyy-MM-dd"));
            key.Append(':');
            key.Append(request.ReturnDate?.ToString("yyyy-MM-dd") ?? "null");
            key.Append(':');
            key.Append(request.Currency);

            return key.ToString();
        }

        private static string GenerateCacheKey(string operation, MultiOriginFlightSearchRequest request)
        {
            var key = new StringBuilder();
            key.Append(operation);
            key.Append(':');
            key.Append(string.Join(",", request.Origins.OrderBy(o => o)));
            key.Append(':');
            key.Append(request.ContinentFilter ?? "null");
            key.Append(':');
            key.Append(request.CountryFilter ?? "null");
            key.Append(':');

            if (request.DestinationListFilter != null && request.DestinationListFilter.Any())
            {
                key.Append(string.Join(",", request.DestinationListFilter.OrderBy(d => d)));
            }
            else
            {
                key.Append("null");
            }

            key.Append(':');
            key.Append(request.DepartureDate.ToString("yyyy-MM-dd"));
            key.Append(':');
            key.Append(request.ReturnDate?.ToString("yyyy-MM-dd") ?? "null");
            key.Append(':');
            key.Append(request.Currency);

            return key.ToString();
        }
    }
}
