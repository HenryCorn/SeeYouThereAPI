using Core.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Org.OpenAPITools.Models;
using System.Net;
using Web.Api.Filters;
using Web.Api.Infrastructure;

namespace Web.Api.Tests.Filters
{
    public class ValidationAndProblemDetailsTests
    {
        private readonly Mock<IHostEnvironment> _mockEnvironment;
        private readonly Mock<ILogger<ProblemDetailsExceptionFilter>> _mockLogger;

        public ValidationAndProblemDetailsTests()
        {
            _mockEnvironment = new Mock<IHostEnvironment>();
            _mockLogger = new Mock<ILogger<ProblemDetailsExceptionFilter>>();

            // Set up properties instead of extension methods
            _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");
        }

        [Fact]
        public void ProblemDetailsExceptionFilter_ShouldReturnProblemDetails_WhenExceptionOccurs()
        {
            // Arrange
            var filter = new ProblemDetailsExceptionFilter(_mockEnvironment.Object, _mockLogger.Object);
            var context = CreateExceptionContext(new InvalidOperationException("Test exception"));

            // Act
            filter.OnException(context);

            // Assert
            var result = context.Result as ObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            result.Value.Should().BeOfType<ProblemDetails>();

            var problemDetails = result.Value as ProblemDetails;
            problemDetails!.Status.Should().Be(StatusCodes.Status500InternalServerError);
            problemDetails.Title.Should().Be("An unexpected error occurred");
            problemDetails.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.6.1");

            // In development environment, we include exception details
            problemDetails.Detail.Should().Contain("Test exception");
            problemDetails.Extensions.Should().ContainKey("traceId");
        }

        [Fact]
        public void ValidationProblemDetailsFactory_ShouldCreateProperValidationProblemDetails()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("Origins", "Origins are required");
            modelState.AddModelError("Date", "Date must be in the future");

            var options = new Mock<IOptions<ApiBehaviorOptions>>();
            options.Setup(o => o.Value).Returns(new ApiBehaviorOptions());

            var problemDetailsOptions = new Mock<IOptions<ProblemDetailsOptions>>();

            var factory = new ValidationProblemDetailsFactory(options.Object, problemDetailsOptions.Object);

            // Act
            var problemDetails = factory.CreateValidationProblemDetails(
                httpContext,
                modelState,
                StatusCodes.Status400BadRequest,
                "Validation Failed",
                "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                "There were validation errors",
                "/api/v1/destinations/cheapest"
            );

            // Assert
            problemDetails.Should().NotBeNull();
            problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
            problemDetails.Title.Should().Be("Validation Failed");
            problemDetails.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.1");
            problemDetails.Detail.Should().Be("There were validation errors");
            problemDetails.Instance.Should().Be("/api/v1/destinations/cheapest");
            problemDetails.Errors.Should().ContainKeys("Origins", "Date");
            problemDetails.Extensions.Should().ContainKey("traceId");
        }

        [Fact]
        public void CheapestDestinationRequestValidator_ShouldFailValidation_WhenInputIsInvalid()
        {
            // Arrange
            var validator = new Validators.CheapestDestinationRequestValidator();
            var request = new CheapestDestinationRequest
            {
                // Missing Origins
                Origins = null,
                // Past date
                Date = new DateOnly(2020, 1, 1),
                RegionType = CheapestDestinationRequest.RegionTypeEnum.CountryEnum,
                Regions = ["FR"]
            };

            // Act
            ValidationResult result = validator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCountGreaterThan(0);
            result.Errors.Should().Contain(e => e.PropertyName == "Origins");
            result.Errors.Should().Contain(e => e.PropertyName == "Date");
        }

        [Fact]
        public void CheapestDestinationRequestValidator_ShouldPassValidation_WhenInputIsValid()
        {
            // Arrange - Use current date which is September 28, 2025 as per context
            var validator = new Validators.CheapestDestinationRequestValidator();
            var request = new CheapestDestinationRequest
            {
                Origins = ["JFK", "LHR"],
                Date = new DateOnly(2025, 10, 15), // Future date
                RegionType = CheapestDestinationRequest.RegionTypeEnum.ContinentEnum,
                Regions = ["EU"]
            };

            // Act
            ValidationResult result = validator.Validate(request);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        private static ExceptionContext CreateExceptionContext(Exception ex)
        {
            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new ActionDescriptor(),
                new ModelStateDictionary());

            return new ExceptionContext(actionContext, new List<IFilterMetadata>())
            {
                Exception = ex
            };
        }
    }
}
