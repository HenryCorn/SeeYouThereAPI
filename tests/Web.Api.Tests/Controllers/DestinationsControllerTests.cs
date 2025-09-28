using Core.Interfaces;
using Core.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Org.OpenAPITools.Models;
using Web.Api.Controllers;

namespace Web.Api.Tests.Controllers
{
    public class DestinationsControllerTests
    {
        private readonly Mock<IFlightSearchClient> _mockFlightSearchClient;
        private readonly DestinationsController _controller;

        public DestinationsControllerTests()
        {
            _mockFlightSearchClient = new Mock<IFlightSearchClient>();
            var mockLogger = new Mock<ILogger<DestinationsController>>();
            _controller = new DestinationsController(_mockFlightSearchClient.Object, mockLogger.Object);
        }

        [Fact]
        public void FindCheapestDestination_WithNoCommonDestinations_ReturnsNotFound()
        {
            var request = new CheapestDestinationRequest
            {
                Origins = ["JFK", "LHR"],
                Date = new DateOnly(2025, 10, 1),
                RegionType = CheapestDestinationRequest.RegionTypeEnum.ContinentEnum,
                Regions = ["EU"]
            };

            _mockFlightSearchClient.Setup(m => m.GetCheapestDestinationsAsync(It.Is<FlightSearchRequest>(r => r.Origin == "JFK")))
                .ReturnsAsync(new List<FlightSearchResult>
                {
                    new() { Origin = "JFK", DestinationCityCode = "PAR", DestinationCountryCode = "FR", Price = 300m, Currency = "USD" }
                });

            _mockFlightSearchClient.Setup(m => m.GetCheapestDestinationsAsync(It.Is<FlightSearchRequest>(r => r.Origin == "LHR")))
                .ReturnsAsync(new List<FlightSearchResult>
                {
                    new() { Origin = "LHR", DestinationCityCode = "MAD", DestinationCountryCode = "ES", Price = 150m, Currency = "USD" }
                });

            var result = _controller.FindCheapestDestination(request);

            result.Should().BeOfType<NotFoundObjectResult>();
            ((NotFoundObjectResult)result).Value.Should().Be("No common destination found for all specified origins");
        }

        [Fact]
        public void FindCheapestDestination_WithCommonDestination_ReturnsOkWithCheapestDestination()
        {
            var request = new CheapestDestinationRequest
            {
                Origins = ["JFK", "LHR"],
                Date = new DateOnly(2025, 10, 1),
                RegionType = CheapestDestinationRequest.RegionTypeEnum.ContinentEnum,
                Regions = ["EU"]
            };
            _mockFlightSearchClient.Setup(m => m.GetCheapestDestinationsAsync(It.Is<FlightSearchRequest>(r => r.Origin == "JFK")))
                .ReturnsAsync(new List<FlightSearchResult>
                {
                    new() { Origin = "JFK", DestinationCityCode = "PAR", DestinationCountryCode = "FR", Price = 300m, Currency = "USD" },
                    new() { Origin = "JFK", DestinationCityCode = "ROM", DestinationCountryCode = "IT", Price = 350m, Currency = "USD" }
                });
            _mockFlightSearchClient.Setup(m => m.GetCheapestDestinationsAsync(It.Is<FlightSearchRequest>(r => r.Origin == "LHR")))
                .ReturnsAsync(new List<FlightSearchResult>
                {
                    new() { Origin = "LHR", DestinationCityCode = "PAR", DestinationCountryCode = "FR", Price = 150m, Currency = "USD" },
                    new() { Origin = "LHR", DestinationCityCode = "BCN", DestinationCountryCode = "ES", Price = 120m, Currency = "USD" }
                });

            var result = _controller.FindCheapestDestination(request);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = (OkObjectResult)result;
            var response = okResult.Value as CheapestDestinationResponse;
            response.Should().NotBeNull();
            response.DestinationCity.Should().Be("PAR");
            response.DestinationCountry.Should().Be("FR");
            response.TotalPrice.Should().Be(450f);
            response.Currency.Should().Be("USD");
            response.PerOriginPrices.Should().ContainKeys("JFK", "LHR");
            response.PerOriginPrices["JFK"].Should().Be((decimal)300f);
            response.PerOriginPrices["LHR"].Should().Be((decimal)150f);
        }

        [Fact]
        public void FindCheapestDestination_WithMultipleCommonDestinations_ReturnsOkWithCheapest()
        {
            var request = new CheapestDestinationRequest
            {
                Origins = ["JFK", "LHR"],
                Date = new DateOnly(2025, 10, 1),
                RegionType = CheapestDestinationRequest.RegionTypeEnum.ContinentEnum,
                Regions = ["EU"]
            };
            _mockFlightSearchClient.Setup(m => m.GetCheapestDestinationsAsync(It.Is<FlightSearchRequest>(r => r.Origin == "JFK")))
                .ReturnsAsync(new List<FlightSearchResult>
                {
                    new() { Origin = "JFK", DestinationCityCode = "PAR", DestinationCountryCode = "FR", Price = 300m, Currency = "USD" },
                    new() { Origin = "JFK", DestinationCityCode = "ROM", DestinationCountryCode = "IT", Price = 250m, Currency = "USD" }
                });
            _mockFlightSearchClient.Setup(m => m.GetCheapestDestinationsAsync(It.Is<FlightSearchRequest>(r => r.Origin == "LHR")))
                .ReturnsAsync(new List<FlightSearchResult>
                {
                    new() { Origin = "LHR", DestinationCityCode = "PAR", DestinationCountryCode = "FR", Price = 150m, Currency = "USD" },
                    new() { Origin = "LHR", DestinationCityCode = "ROM", DestinationCountryCode = "IT", Price = 200m, Currency = "USD" }
                });

            var result = _controller.FindCheapestDestination(request);


            result.Should().BeOfType<OkObjectResult>();
            var okResult = (OkObjectResult)result;
            var response = okResult.Value as CheapestDestinationResponse;
            response.Should().NotBeNull();
            response.DestinationCity.Should().Be("PAR");
            response.TotalPrice.Should().Be(450f);
        }

        [Fact]
        public void FindCheapestDestination_WithFiltersByCountry_AppliesFilterCorrectly()
        {
            var request = new CheapestDestinationRequest
            {
                Origins = ["JFK"],
                Date = new DateOnly(2025, 10, 1),
                RegionType = CheapestDestinationRequest.RegionTypeEnum.CountryEnum,
                Regions = ["FR"]
            };
            _mockFlightSearchClient.Setup(m => m.GetCheapestDestinationsAsync(It.Is<FlightSearchRequest>(
                r => r.Origin == "JFK" && r.CountryFilter == "FR")))
                .ReturnsAsync(new List<FlightSearchResult>
                {
                    new() { Origin = "JFK", DestinationCityCode = "PAR", DestinationCountryCode = "FR", Price = 300m, Currency = "USD" }
                });

            var result = _controller.FindCheapestDestination(request);

            result.Should().BeOfType<OkObjectResult>();
            _mockFlightSearchClient.Verify(m => m.GetCheapestDestinationsAsync(It.Is<FlightSearchRequest>(
                r => r.CountryFilter == "FR")), Times.Once);
        }

        [Fact]
        public void FindCheapestDestination_WithFiltersByCities_AppliesFilterCorrectly()
        {
            var cities = new List<string> { "PAR", "ROM", "MAD" };
            var request = new CheapestDestinationRequest
            {
                Origins = ["JFK"],
                Date = new DateOnly(2025, 10, 1),
                RegionType = CheapestDestinationRequest.RegionTypeEnum.CitiesEnum,
                Regions = cities
            };
            _mockFlightSearchClient.Setup(m => m.GetCheapestDestinationsAsync(It.Is<FlightSearchRequest>(
                r => r.Origin == "JFK" && r.DestinationListFilter == cities)))
                .ReturnsAsync(new List<FlightSearchResult>
                {
                    new() { Origin = "JFK", DestinationCityCode = "PAR", DestinationCountryCode = "FR", Price = 300m, Currency = "USD" }
                });

            var result = _controller.FindCheapestDestination(request);

            result.Should().BeOfType<OkObjectResult>();
            _mockFlightSearchClient.Verify(m => m.GetCheapestDestinationsAsync(It.Is<FlightSearchRequest>(
                r => r.DestinationListFilter == cities)), Times.Once);
        }

        [Fact]
        public void FindCheapestDestination_WhenExceptionOccurs_ReturnsInternalServerError()
        {
            var request = new CheapestDestinationRequest
            {
                Origins = ["JFK"],
                Date = new DateOnly(2025, 10, 1),
                RegionType = CheapestDestinationRequest.RegionTypeEnum.ContinentEnum,
                Regions = ["EU"]
            };

            var innerException = new Exception("Test exception");
            var aggregateException = new AggregateException(innerException);

            _mockFlightSearchClient.Setup(m => m.GetCheapestDestinationsAsync(It.IsAny<FlightSearchRequest>()))
                .ThrowsAsync(innerException);

            var result = _controller.FindCheapestDestination(request);

            result.Should().BeOfType<NotFoundObjectResult>();
            var statusCodeResult = result as ObjectResult;
            statusCodeResult?.StatusCode.Should().Be(500);
        }
    }
}
