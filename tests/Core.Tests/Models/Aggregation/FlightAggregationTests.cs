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
            var aggregation = new FlightAggregation();


            var result = aggregation.GetCommonDestinations();

            Assert.Empty(result);
        }

        [Fact]
        public void GetCommonDestinations_WithSingleOrigin_ReturnsAllDestinations()
        {
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

            var result = aggregation.GetCommonDestinations();

            Assert.Equal(2, result.Count);
            Assert.Contains("CDG", result);
            Assert.Contains("LHR", result);
        }

        [Fact]
        public void GetCommonDestinations_WithMultipleOrigins_ReturnsIntersection()
        {
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

            var result = aggregation.GetCommonDestinations();

            Assert.Single(result);
            Assert.Contains("CDG", result);
        }

        [Fact]
        public void GetCheapestCommonDestination_WithNoCommonDestinations_ReturnsNull()
        {
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

            var result = aggregation.GetCheapestCommonDestination();

            Assert.Null(result);
        }

        [Fact]
        public void GetCheapestCommonDestination_WithCommonDestinations_ReturnsCheapest()
        {
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

            var result = aggregation.GetCheapestCommonDestination();

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

            var result = aggregation.GetCheapestCommonDestination();

            Assert.NotNull(result);
            Assert.Equal("CDG", result.DestinationCityCode);
            Assert.Equal("FR", result.DestinationCountryCode);
            Assert.Equal(1100m, result.TotalPrice);
            Assert.Equal(500m, result.PerOriginPrices["JFK"]);
            Assert.Equal(600m, result.PerOriginPrices["SFO"]);
        }

        [Fact]
        public void GetOptimalCommonDestination_WithNoCommonDestinations_ReturnsNull()
        {
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

            var result = aggregation.GetOptimalCommonDestination();

            Assert.Null(result);
        }

        [Fact]
        public void GetOptimalCommonDestination_WithLowestTotalPrice_ReturnsBestDestination()
        {
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

            var result = aggregation.GetOptimalCommonDestination();

            Assert.NotNull(result);
            Assert.Equal("FCO", result.DestinationCityCode);
            Assert.Equal("IT", result.DestinationCountryCode);
            Assert.Equal(1000m, result.TotalPrice);
            Assert.Equal(500m, result.MedianPrice);
            Assert.Equal(600m, result.PerOriginPrices["JFK"]);
            Assert.Equal(400m, result.PerOriginPrices["SFO"]);
        }

        [Fact]
        public void GetOptimalCommonDestination_WithEqualTotalPrice_UsesMedianPriceToBreakTie()
        {
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
                            ["CDG"] = new DestinationPrice { DestinationCityCode = "CDG", DestinationCountryCode = "FR", Price = 300 },
                            ["FCO"] = new DestinationPrice { DestinationCityCode = "FCO", DestinationCountryCode = "IT", Price = 500 }
                        }
                    },
                    ["SFO"] = new OriginDestinationPrices
                    {
                        OriginCode = "SFO",
                        Currency = "USD",
                        Destinations = new Dictionary<string, DestinationPrice>
                        {
                            ["CDG"] = new DestinationPrice { DestinationCityCode = "CDG", DestinationCountryCode = "FR", Price = 700 },
                            ["FCO"] = new DestinationPrice { DestinationCityCode = "FCO", DestinationCountryCode = "IT", Price = 500 }
                        }
                    }
                }
            };

            var result = aggregation.GetOptimalCommonDestination();

            Assert.NotNull(result);
            Assert.Equal("CDG", result.DestinationCityCode);
            Assert.Equal("FR", result.DestinationCountryCode);
        }

        [Fact]
        public void GetOptimalCommonDestination_WithEqualTotalAndMedian_UsesLexicographicOrdering()
        {

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
                            ["AMS"] = new DestinationPrice { DestinationCityCode = "AMS", DestinationCountryCode = "NL", Price = 500 },
                            ["BCN"] = new DestinationPrice { DestinationCityCode = "BCN", DestinationCountryCode = "ES", Price = 500 }
                        }
                    },
                    ["SFO"] = new OriginDestinationPrices
                    {
                        OriginCode = "SFO",
                        Currency = "USD",
                        Destinations = new Dictionary<string, DestinationPrice>
                        {
                            ["AMS"] = new DestinationPrice { DestinationCityCode = "AMS", DestinationCountryCode = "NL", Price = 500 },
                            ["BCN"] = new DestinationPrice { DestinationCityCode = "BCN", DestinationCountryCode = "ES", Price = 500 }
                        }
                    }
                }
            };

            var result = aggregation.GetOptimalCommonDestination();

            Assert.NotNull(result);
            Assert.Equal("AMS", result.DestinationCityCode);
            Assert.Equal("NL", result.DestinationCountryCode);
        }

        [Fact]
        public void GetOptimalCommonDestination_WithThreeOrigins_CalculatesCorrectMedian()
        {
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
                            ["CDG"] = new DestinationPrice { DestinationCityCode = "CDG", DestinationCountryCode = "FR", Price = 200 },
                            ["FCO"] = new DestinationPrice { DestinationCityCode = "FCO", DestinationCountryCode = "IT", Price = 300 }
                        }
                    },
                    ["SFO"] = new OriginDestinationPrices
                    {
                        OriginCode = "SFO",
                        Currency = "USD",
                        Destinations = new Dictionary<string, DestinationPrice>
                        {
                            ["CDG"] = new DestinationPrice { DestinationCityCode = "CDG", DestinationCountryCode = "FR", Price = 400 },
                            ["FCO"] = new DestinationPrice { DestinationCityCode = "FCO", DestinationCountryCode = "IT", Price = 300 }
                        }
                    },
                    ["LAX"] = new OriginDestinationPrices
                    {
                        OriginCode = "LAX",
                        Currency = "USD",
                        Destinations = new Dictionary<string, DestinationPrice>
                        {
                            ["CDG"] = new DestinationPrice { DestinationCityCode = "CDG", DestinationCountryCode = "FR", Price = 600 },
                            ["FCO"] = new DestinationPrice { DestinationCityCode = "FCO", DestinationCountryCode = "IT", Price = 600 }
                        }
                    }
                }
            };

            var result = aggregation.GetOptimalCommonDestination();

            Assert.NotNull(result);
            Assert.Equal("FCO", result.DestinationCityCode);
            Assert.Equal("IT", result.DestinationCountryCode);
            Assert.Equal(1200m, result.TotalPrice);
            Assert.Equal(300m, result.MedianPrice);
        }
    }
}
