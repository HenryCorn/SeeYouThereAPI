// <copyright file="RateLimitMiddleware.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Web.Api.Middleware
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.RateLimiting;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Web.Api.Configuration;

    /// <summary>
    /// Middleware that implements rate limiting per client IP address.
    /// </summary>
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitMiddleware> _logger;
        private readonly RateLimitOptions _options;
        private readonly ConcurrentDictionary<string, FixedWindowRateLimiter> _limiters = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="RateLimitMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="options">Rate limiting options.</param>
        public RateLimitMiddleware(
            RequestDelegate next,
            ILogger<RateLimitMiddleware> logger,
            IOptions<RateLimitOptions> options)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Processes a request and applies rate limiting.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var clientIp = GetClientIpAddress(context);

            if (string.IsNullOrEmpty(clientIp))
            {
                _logger.LogWarning("Unable to determine client IP address, skipping rate limiting");
                await _next(context);
                return;
            }

            var limiter = _limiters.GetOrAdd(clientIp, key => CreateRateLimiter());
            using RateLimitLease lease = limiter.AttemptAcquire(permitCount: 1);

            if (lease.IsAcquired)
            {
                _logger.LogDebug("Rate limit allowed for {ClientIp}", clientIp);
                await _next(context);
            }
            else
            {
                _logger.LogWarning("Rate limit exceeded for {ClientIp}", clientIp);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = _options.RetryAfterSeconds.ToString();
                context.Response.ContentType = "application/problem+json";

                var problemDetails = new
                {
                    type = "https://tools.ietf.org/html/rfc6585#section-4",
                    title = "Too Many Requests",
                    status = 429,
                    detail = "Rate limit has been exceeded. Please try again later.",
                    instance = context.Request.Path,
                    retryAfter = _options.RetryAfterSeconds,
                };

                await context.Response.WriteAsJsonAsync(problemDetails);
            }
        }

        private static string GetClientIpAddress(HttpContext context)
        {
            var ip = context.Request.Headers["X-Forwarded-For"].ToString();

            if (string.IsNullOrEmpty(ip))
            {
                ip = context.Connection.RemoteIpAddress?.ToString();
            }
            else
            {
                ip = ip.Split(',')[0].Trim();
            }

            return ip;
        }

        private FixedWindowRateLimiter CreateRateLimiter()
        {
            return new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                PermitLimit = _options.TokenLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = _options.QueueLimit,
                Window = TimeSpan.FromSeconds(_options.ReplenishmentPeriodSeconds),
                AutoReplenishment = true,
            });
        }
    }
}
