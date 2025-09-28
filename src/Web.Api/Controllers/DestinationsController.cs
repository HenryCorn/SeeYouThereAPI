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
            _logger.LogInformation("Finding cheapest destinations for {OriginsCount} origins", cheapestDestinationRequest.Origins.Count);

            var allResults = new Dictionary<string, List<FlightSearchResult>>();
            foreach (var origin in cheapestDestinationRequest.Origins)
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

                switch (cheapestDestinationRequest.RegionType)
                {
                    case CheapestDestinationRequest.RegionTypeEnum.ContinentEnum:
                        searchRequest.ContinentFilter = cheapestDestinationRequest.Regions?.FirstOrDefault();
                        break;
                    case CheapestDestinationRequest.RegionTypeEnum.CountryEnum:
                        searchRequest.CountryFilter = cheapestDestinationRequest.Regions?.FirstOrDefault();
                        break;
                    case CheapestDestinationRequest.RegionTypeEnum.CitiesEnum:
                        searchRequest.DestinationListFilter = cheapestDestinationRequest.Regions;
                        break;
                }

                var results = _flightSearchClient.GetCheapestDestinationsAsync(searchRequest).Result;
                allResults[origin] = results.ToList();
            }

            var commonDestinations = FindCommonDestinations(allResults);

            if (!commonDestinations.Any())
            {
                return NotFound("No common destination found for all specified origins");
            }

            var cheapestCommon = commonDestinations.OrderBy(d => d.TotalPrice).First();

            var response = new CheapestDestinationResponse
            {
                DestinationCity = cheapestCommon.DestinationCity,
                DestinationCountry = cheapestCommon.DestinationCountry,
                TotalPrice = (float)cheapestCommon.TotalPrice,
                Currency = cheapestCommon.Currency,
                PerOriginPrices = cheapestCommon.PerOriginPrices
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding cheapest destinations: {Message}", ex.Message);
            return StatusCode(500, "An error occurred while processing your request");
        }
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
            string? destinationCountry = null;

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

        public string? DestinationCountry { get; set; }

        public decimal TotalPrice { get; set; }

        public required string Currency { get; set; }

        public required Dictionary<string, decimal> PerOriginPrices { get; set; }
    }
}