using System.Collections.Generic;
using System.Linq;
using Core.Models;
using Core.Models.Aggregation;
using Xunit;

namespace Core.Tests.Models.Aggregation
{
    public class FlightAggregationTests
    {
        [Fact]
        public void GetCommonDestinations_WithNoOrigins_ReturnsEmptySet()
        {
            // Arrange
            var aggregation = new FlightAggregation();

            // Act
            var result = aggregation.GetCommonDestinations();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetCommonDestinations_WithSingleOrigin_ReturnsAllDestinations()
        {
            // Arrange
            var aggregation = new FlightAggregation
            {
                OriginData = new Dictionary<string, OriginDestinationPrices>
                {
                    ["JFK"] = new OriginDestinationPrices
                    {
                        OriginCode = "JFK",
                        Currency = "USD",
                        Destinations = new Dictionary<string, DestinationPrice>
                        {
                            ["CDG"] = new DestinationPrice { DestinationCityCode = "CDG", DestinationCountryCode = "FR", Price = 500 },
                            ["LHR"] = new DestinationPrice { DestinationCityCode = "LHR", DestinationCountryCode = "GB", Price = 450 }
                        }
                    }
                }
            };

            // Act
            var result = aggregation.GetCommonDestinations();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains("CDG", result);
            Assert.Contains("LHR", result);
        }

        [Fact]
        public void GetCommonDestinations_WithMultipleOrigins_ReturnsIntersection()
        {
            // Arrange
            var aggregation = new FlightAggregation
            {
                OriginData = new Dictionary<string, OriginDestinationPrices>
                {
                    ["JFK"] = new OriginDestinationPrices
                    {
                        OriginCode = "JFK",
                        Currency = "USD",
                        Destinations = new Dictionary<string, DestinationPrice>
                        {
                            ["CDG"] = new DestinationPrice { DestinationCityCode = "CDG", DestinationCountryCode = "FR", Price = 500 },
                            ["LHR"] = new DestinationPrice { DestinationCityCode = "LHR", DestinationCountryCode = "GB", Price = 450 }
                        }
                    },
                    ["SFO"] = new OriginDestinationPrices
                    {
                        OriginCode = "SFO",
                        Currency = "USD",
                        Destinations = new Dictionary<string, DestinationPrice>
                        {
                            ["CDG"] = new DestinationPrice { DestinationCityCode = "CDG", DestinationCountryCode = "FR", Price = 600 },
                            ["FCO"] = new DestinationPrice { DestinationCityCode = "FCO", DestinationCountryCode = "IT", Price = 700 }
                        }
                    }
                }
            };

            // Act
            var result = aggregation.GetCommonDestinations();

            // Assert
            Assert.Single(result);
            Assert.Contains("CDG", result);
        }

        [Fact]
        public void GetCheapestCommonDestination_WithNoCommonDestinations_ReturnsNull()
        {
            // Arrange
            var aggregation = new FlightAggregation
            {
                OriginData = new Dictionary<string, OriginDestinationPrices>
                {
                    ["JFK"] = new OriginDestinationPrices
                    {
                        OriginCode = "JFK",
                        Destinations = new Dictionary<string, DestinationPrice>
                        {
                            ["CDG"] = new DestinationPrice { DestinationCityCode = "CDG", DestinationCountryCode = "FR", Price = 500 }
                        }
                    },
                    ["SFO"] = new OriginDestinationPrices
                    {
                        OriginCode = "SFO",
                        Destinations = new Dictionary<string, DestinationPrice>
                        {
                            ["LHR"] = new DestinationPrice { DestinationCityCode = "LHR", DestinationCountryCode = "GB", Price = 600 }
                        }
                    }
                }
            };

            // Act
            var result = aggregation.GetCheapestCommonDestination();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCheapestCommonDestination_WithCommonDestinations_ReturnsCheapest()
        {
            // Arrange
            var aggregation = new FlightAggregation
            {
                Currency = "USD",
                OriginData = new Dictionary<string, OriginDestinationPrices>
                {
                    ["JFK"] = new OriginDestinationPrices
                    {
                        OriginCode = "JFK",
                        Currency = "USD",
                        Destinations = new Dictionary<string, DestinationPrice>
                        {
                            ["CDG"] = new DestinationPrice { DestinationCityCode = "CDG", DestinationCountryCode = "FR", Price = 500 },
                            ["FCO"] = new DestinationPrice { DestinationCityCode = "FCO", DestinationCountryCode = "IT", Price = 600 }
                        }
                    },
                    ["SFO"] = new OriginDestinationPrices
                    {
                        OriginCode = "SFO",
                        Currency = "USD",
                        Destinations = new Dictionary<string, DestinationPrice>
                        {
                            ["CDG"] = new DestinationPrice { DestinationCityCode = "CDG", DestinationCountryCode = "FR", Price = 600 },
                            ["FCO"] = new DestinationPrice { DestinationCityCode = "FCO", DestinationCountryCode = "IT", Price = 400 }
                        }
                    }
                }
            };

            // Act
            var result = aggregation.GetCheapestCommonDestination();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("FCO", result.DestinationCityCode);
            Assert.Equal("IT", result.DestinationCountryCode);
            Assert.Equal(1000m, result.TotalPrice);
            Assert.Equal(600m, result.PerOriginPrices["JFK"]);
            Assert.Equal(400m, result.PerOriginPrices["SFO"]);
        }

        [Fact]
        public void GetCheapestCommonDestination_WithMultipleCommonDestinations_ReturnsLowestTotal()
        {
            // Arrange
            var aggregation = new FlightAggregation
            {
                Currency = "USD",
                OriginData = new Dictionary<string, OriginDestinationPrices>
                {
                    ["JFK"] = new OriginDestinationPrices
                    {
                        OriginCode = "JFK",
                        Currency = "USD",
                        Destinations = new Dictionary<string, DestinationPrice>
                        {
                            ["CDG"] = new DestinationPrice { DestinationCityCode = "CDG", DestinationCountryCode = "FR", Price = 500 },
                            ["FCO"] = new DestinationPrice { DestinationCityCode = "FCO", DestinationCountryCode = "IT", Price = 600 }
                        }
                    },
                    ["SFO"] = new OriginDestinationPrices
                    {
                        OriginCode = "SFO",
                        Currency = "USD",
                        Destinations = new Dictionary<string, DestinationPrice>
                        {
                            ["CDG"] = new DestinationPrice { DestinationCityCode = "CDG", DestinationCountryCode = "FR", Price = 600 },
                            ["FCO"] = new DestinationPrice { DestinationCityCode = "FCO", DestinationCountryCode = "IT", Price = 700 }
                        }
                    }
                }
            };

            // Act
            var result = aggregation.GetCheapestCommonDestination();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("CDG", result.DestinationCityCode);
            Assert.Equal("FR", result.DestinationCountryCode);
            Assert.Equal(1100m, result.TotalPrice);
            Assert.Equal(500m, result.PerOriginPrices["JFK"]);
            Assert.Equal(600m, result.PerOriginPrices["SFO"]);
        }
    }
}
