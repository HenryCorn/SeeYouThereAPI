// <copyright file="DestinationsController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Web.Api.Controllers;

using System;
using System.Collections.Generic;
using System.Linq;
using Core.Interfaces;
using Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Org.OpenAPITools.Controllers;
using Org.OpenAPITools.Models;

/// <inheritdoc />
public class DestinationsController : DestinationsApiController
{
    private readonly IFlightSearchClient _flightSearchClient;
    private readonly ILogger<DestinationsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DestinationsController"/> class.
    /// </summary>
    /// <param name="flightSearchClient">The flight search client.</param>
    /// <param name="logger">The logger.</param>
    public DestinationsController(IFlightSearchClient flightSearchClient, ILogger<DestinationsController> logger)
    {
        _flightSearchClient = flightSearchClient ?? throw new ArgumentNullException(nameof(flightSearchClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public override IActionResult FindCheapestDestination(CheapestDestinationRequest cheapestDestinationRequest)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning(
                    "Request validation failed: Invalid model state {@ValidationErrors}",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)));

                return BadRequest(new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred",
                });
            }

            _logger.LogInformation("Searching for cheapest destination with {@RequestParameters}", new
            {
                OriginCount = cheapestDestinationRequest.Origins.Count,
                cheapestDestinationRequest.Origins,
                cheapestDestinationRequest.Date,
                cheapestDestinationRequest.RegionType,
                RegionsCount = cheapestDestinationRequest.Regions.Count,
            });

            if (!cheapestDestinationRequest.Origins.Any())
            {
                _logger.LogWarning("Request validation failed: No origins specified");
                return BadRequest("At least one origin must be specified");
            }

            var allResults = new Dictionary<string, List<FlightSearchResult>>();
            var searchStart = DateTime.UtcNow;

            foreach (var origin in cheapestDestinationRequest.Origins)
            {
                var originProcessStart = DateTime.UtcNow;
                _logger.LogDebug("Processing origin {Origin}", origin);

                try
                {
                    var searchRequest = CreateSearchRequest(origin, cheapestDestinationRequest);

                    _logger.LogDebug("Calling flight search service for origin {Origin}", origin);
                    var searchStartTime = DateTime.UtcNow;
                    var results = _flightSearchClient.GetCheapestDestinationsAsync(searchRequest).Result;
                    var searchDuration = DateTime.UtcNow - searchStartTime;

                    var flightSearchResults = results.ToList();
                    _logger.LogInformation(
                        "Retrieved {ResultCount} flight options for {Origin} in {DurationMs}ms",
                        flightSearchResults.Count(),
                        origin,
                        searchDuration.TotalMilliseconds);

                    allResults[origin] = flightSearchResults.ToList();
                }
                catch (Exception ex)
                {
                    if (ex is AggregateException aggregateEx && aggregateEx.InnerExceptions.Count == 1)
                    {
                        _logger.LogError(
                            aggregateEx.InnerException,
                            "Failed to process origin {Origin}: {ErrorType} - {ErrorMessage}",
                            origin,
                            aggregateEx.InnerException!.GetType().Name,
                            aggregateEx.InnerException.Message);
                    }
                    else
                    {
                        _logger.LogError(
                            ex,
                            "Failed to process origin {Origin}: {ErrorType} - {ErrorMessage}",
                            origin,
                            ex.GetType().Name,
                            ex.Message);
                    }
                }

                var originProcessDuration = DateTime.UtcNow - originProcessStart;
                _logger.LogDebug("Completed processing origin {Origin} in {DurationMs}ms", origin, originProcessDuration.TotalMilliseconds);
            }

            var searchDurationTotal = DateTime.UtcNow - searchStart;
            _logger.LogInformation(
                "Completed searching {OriginCount} origins in {DurationMs}ms",
                cheapestDestinationRequest.Origins.Count,
                searchDurationTotal.TotalMilliseconds);

            if (!allResults.Any())
            {
                _logger.LogWarning(
                    "No flight results found for any of the {OriginCount} origins",
                    cheapestDestinationRequest.Origins.Count);
                return NotFound("No flight results could be found for any origin");
            }

            _logger.LogDebug("Finding common destinations across {OriginCount} origins", allResults.Count);
            var commonDestinations = FindCommonDestinations(allResults);

            if (!commonDestinations.Any())
            {
                _logger.LogInformation("No common destinations found across origins {@Origins}", allResults.Keys);
                return NotFound("No common destination found for all specified origins");
            }

            var cheapestCommon = commonDestinations.OrderBy(d => d.TotalPrice).First();

            _logger.LogInformation("Found cheapest common destination {@Destination}", new
            {
                City = cheapestCommon.DestinationCity,
                Country = cheapestCommon.DestinationCountry,
                cheapestCommon.TotalPrice,
                cheapestCommon.Currency,
                OriginCount = cheapestCommon.PerOriginPrices.Count,
            });

            var response = new CheapestDestinationResponse
            {
                DestinationCity = cheapestCommon.DestinationCity,
                DestinationCountry = cheapestCommon.DestinationCountry!,
                TotalPrice = (float)cheapestCommon.TotalPrice,
                Currency = cheapestCommon.Currency,
                PerOriginPrices = cheapestCommon.PerOriginPrices
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            };

            _logger.LogInformation("Returning successful response for {Destination}", response.DestinationCity);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in FindCheapestDestination: {ErrorType}", ex.GetType().Name);
            return StatusCode(500, $"An error occurred while processing your request: {ex.Message}");
        }
    }

    private FlightSearchRequest CreateSearchRequest(string origin, CheapestDestinationRequest cheapestDestinationRequest)
    {
        var searchRequest = new FlightSearchRequest
        {
            Origin = origin,
            DepartureDate = new DateTime(
                cheapestDestinationRequest.Date.Year,
                cheapestDestinationRequest.Date.Month,
                cheapestDestinationRequest.Date.Day),
            Currency = "USD",
        };

        _logger.LogDebug("Configuring search with {RegionType} filter", cheapestDestinationRequest.RegionType);

        // Handle the regionType according to our updated OpenAPI spec
        switch (cheapestDestinationRequest.RegionType)
        {
            case CheapestDestinationRequest.RegionTypeEnum.ContinentEnum:
                var continentFilter = cheapestDestinationRequest.Regions.FirstOrDefault();
                _logger.LogDebug("Using continent filter: {ContinentFilter}", continentFilter);
                searchRequest.ContinentFilter = continentFilter;
                break;
            case CheapestDestinationRequest.RegionTypeEnum.CountryEnum:
                var countryFilter = cheapestDestinationRequest.Regions.FirstOrDefault();
                _logger.LogDebug("Using country filter: {CountryFilter}", countryFilter);
                searchRequest.CountryFilter = countryFilter;
                break;
            case CheapestDestinationRequest.RegionTypeEnum.CitiesEnum:
                var citiesCount = cheapestDestinationRequest.Regions.Count;
                _logger.LogDebug("Using cities filter with {CitiesCount} cities", citiesCount);
                searchRequest.DestinationListFilter = cheapestDestinationRequest.Regions;
                break;
            default:
                _logger.LogWarning("Unknown region type: {RegionType}", cheapestDestinationRequest.RegionType);
                break;
        }

        return searchRequest;
    }

    private List<CommonDestination> FindCommonDestinations(Dictionary<string, List<FlightSearchResult>> allResults)
    {
        var firstOrigin = allResults.Keys.First();
        var destinations = allResults[firstOrigin]
            .Select(r => r.DestinationCityCode)
            .ToHashSet();

        foreach (var origin in allResults.Keys.Skip(1))
        {
            var originDestinations = allResults[origin]
                .Select(r => r.DestinationCityCode)
                .ToHashSet();

            destinations.IntersectWith(originDestinations);
        }

        // Build common destination objects
        var commonDestinations = new List<CommonDestination>();
        foreach (var destination in destinations)
        {
            var destData = new CommonDestination
            {
                DestinationCity = destination,
                PerOriginPrices = new Dictionary<string, decimal>(),
                Currency = allResults.First().Value.First().Currency,
            };

            decimal totalPrice = 0;
            string destinationCountry = null;

            foreach (var origin in allResults.Keys)
            {
                var result = allResults[origin].FirstOrDefault(r => r.DestinationCityCode == destination);
                if (result != null)
                {
                    destData.PerOriginPrices[origin] = result.Price;
                    totalPrice += result.Price;
                    destinationCountry = result.DestinationCountryCode;
                }
            }

            destData.TotalPrice = totalPrice;
            destData.DestinationCountry = destinationCountry;

            commonDestinations.Add(destData);
        }

        return commonDestinations;
    }

    private class CommonDestination
    {
        public required string DestinationCity { get; set; }

        public string DestinationCountry { get; set; }

        public decimal TotalPrice { get; set; }

        public required string Currency { get; set; }

        public required Dictionary<string, decimal> PerOriginPrices { get; set; }
    }
}
