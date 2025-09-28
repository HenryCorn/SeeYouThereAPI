using Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using Org.OpenAPITools.Models;
using System.Net;
using System.Text.Json;
using Web.Api.Controllers;

namespace Web.Api.Tests.Controllers
{
    public class DestinationsControllerValidationTests
    {
        private readonly Mock<IFlightSearchClient> _mockFlightSearchClient;
        private readonly Mock<ILogger<DestinationsController>> _mockLogger;
        private readonly DestinationsController _controller;

        public DestinationsControllerValidationTests()
        {
            _mockFlightSearchClient = new Mock<IFlightSearchClient>();
            _mockLogger = new Mock<ILogger<DestinationsController>>();
            _controller = new DestinationsController(_mockFlightSearchClient.Object, _mockLogger.Object);
        }

        [Fact]
        public void FindCheapestDestination_WithEmptyOrigins_ReturnsBadRequest()
        {
            // Arrange
            var request = new CheapestDestinationRequest
            {
                Origins = [],
                Date = new DateOnly(2025, 10, 1),
                RegionType = CheapestDestinationRequest.RegionTypeEnum.ContinentEnum,
                Regions = ["EU"]
            };

            // Act
            var result = _controller.FindCheapestDestination(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.Value.Should().NotBeNull();
            badRequestResult.Value.ToString().Should().Contain("At least one origin must be specified");
        }

        [Fact]
        public void FindCheapestDestination_WithInvalidModelState_ShouldReturnBadRequest()
        {
            // Arrange - Create controller with invalid ModelState
            var controller = new DestinationsController(_mockFlightSearchClient.Object, _mockLogger.Object);
            controller.ModelState.AddModelError("Origins", "At least one origin must be specified");
            controller.ModelState.AddModelError("Date", "Date must be a valid future date.");

            // Create a valid request (model state errors will override)
            var request = new CheapestDestinationRequest
            {
                Origins = ["JFK"],
                Date = new DateOnly(2025, 10, 1),
                RegionType = CheapestDestinationRequest.RegionTypeEnum.ContinentEnum,
                Regions = ["EU"]
            };

            // Act
            var result = controller.FindCheapestDestination(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.Value.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public void FindCheapestDestination_WithInvalidRegionType_ShouldReturnBadRequest()
        {
            // Arrange - Simulate validator rejecting request with regions but no region type
            var controller = new DestinationsController(_mockFlightSearchClient.Object, _mockLogger.Object);
            controller.ModelState.AddModelError("RegionType", "RegionType must be specified when Regions are provided.");

            var request = new CheapestDestinationRequest
            {
                Origins = ["JFK"],
                Date = new DateOnly(2025, 10, 1),
                RegionType = CheapestDestinationRequest.RegionTypeEnum.CitiesEnum,
                Regions = ["EU"]
            };

            // Act
            var result = controller.FindCheapestDestination(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.Value.Should().NotBeNull();
        }
    }
}
