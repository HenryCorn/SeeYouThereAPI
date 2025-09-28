using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using System.Threading.Tasks;
using Web.Api.Configuration;
using Web.Api.Middleware;
using Xunit;

namespace Web.Api.Tests.Middleware
{
    public class RateLimitMiddlewareTests
    {
        private readonly Mock<ILogger<RateLimitMiddleware>> _mockLogger;
        private readonly RateLimitOptions _options;
        private readonly RateLimitMiddleware _middleware;
        private readonly RequestDelegate _nextMiddleware;

        public RateLimitMiddlewareTests()
        {
            _mockLogger = new Mock<ILogger<RateLimitMiddleware>>();

            // Configure test options with a very restrictive limit for testing
            _options = new RateLimitOptions
            {
                TokenLimit = 2,
                TokensPerPeriod = 1,
                ReplenishmentPeriodSeconds = 5,
                QueueLimit = 1,
                RetryAfterSeconds = 10
            };

            var mockOptions = new Mock<IOptions<RateLimitOptions>>();
            mockOptions.Setup(o => o.Value).Returns(_options);

            // Simple pass-through middleware for testing
            _nextMiddleware = (HttpContext ctx) => Task.CompletedTask;

            _middleware = new RateLimitMiddleware(_nextMiddleware, _mockLogger.Object, mockOptions.Object);
        }

        [Fact]
        public async Task AllowsRequestsWithinRateLimit()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

            // Act - make first request (should be allowed)
            await _middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Fact]
        public async Task RejectsRequestsExceedingRateLimit()
        {
            // Arrange
            var context1 = new DefaultHttpContext();
            context1.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.2");

            var context2 = new DefaultHttpContext();
            context2.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.2");

            var context3 = new DefaultHttpContext();
            context3.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.2");

            // Act - exhaust the token bucket
            await _middleware.InvokeAsync(context1); // First request - allowed
            await _middleware.InvokeAsync(context2); // Second request - allowed (token limit is 2)
            await _middleware.InvokeAsync(context3); // Third request - should be rejected

            // Assert
            context1.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
            context2.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
            context3.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);

            // Verify the Retry-After header is set
            context3.Response.Headers.Should().ContainKey("Retry-After");
            context3.Response.Headers["Retry-After"].ToString().Should().Be(_options.RetryAfterSeconds.ToString());
        }

        [Fact]
        public async Task DifferentIpAddressesHaveSeparateRateLimits()
        {
            // Arrange
            var context1 = new DefaultHttpContext();
            context1.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.3");

            var context2 = new DefaultHttpContext();
            context2.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.4");

            // Act - make requests from different IP addresses
            await _middleware.InvokeAsync(context1);
            await _middleware.InvokeAsync(context2);

            // Assert - both should be allowed
            context1.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
            context2.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Fact]
        public async Task HandlesXForwardedForHeader()
        {
            // Arrange
            var context1 = new DefaultHttpContext();
            context1.Request.Headers["X-Forwarded-For"] = "192.168.1.1";

            var context2 = new DefaultHttpContext();
            context2.Request.Headers["X-Forwarded-For"] = "192.168.1.1";

            var context3 = new DefaultHttpContext();
            context3.Request.Headers["X-Forwarded-For"] = "192.168.1.1";

            // Act - exhaust the token bucket
            await _middleware.InvokeAsync(context1); // First request - allowed
            await _middleware.InvokeAsync(context2); // Second request - allowed (token limit is 2)
            await _middleware.InvokeAsync(context3); // Third request - should be rejected

            // Assert
            context3.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        }
    }
}
