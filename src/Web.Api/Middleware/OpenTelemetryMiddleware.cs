// <copyright file="OpenTelemetryMiddleware.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Diagnostics;
using Microsoft.Extensions.Options;
using Web.Api.Configuration;
using Web.Api.Infrastructure;

namespace Web.Api.Middleware
{
    /// <summary>
    /// Middleware that captures HTTP requests metrics with OpenTelemetry
    /// </summary>
    public class OpenTelemetryMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly OpenTelemetryOptions _options;
        private readonly ILogger<OpenTelemetryMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenTelemetryMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next request delegate.</param>
        /// <param name="options">The OpenTelemetry configuration options.</param>
        /// <param name="logger">The logger instance.</param>
        public OpenTelemetryMiddleware(
            RequestDelegate next,
            IOptions<OpenTelemetryOptions> options,
            ILogger<OpenTelemetryMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Invokes the middleware
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            if (!_options.Enabled)
            {
                await _next(context);
                return;
            }

            // Start an activity for the request if not already started by instrumentation
            var path = context.Request.Path.ToString();
            var method = context.Request.Method;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Increment the request counter
                OpenTelemetryExtensions.HttpRequestCounter.Add(
                    1,
                    new KeyValuePair<string, object>("path", path),
                    new KeyValuePair<string, object>("method", method));

                // Process the request
                await _next(context);
            }
            catch (Exception ex)
            {
                // Increment the error counter
                OpenTelemetryExtensions.HttpErrorCounter.Add(
                    1,
                    new KeyValuePair<string, object>("path", path),
                    new KeyValuePair<string, object>("method", method),
                    new KeyValuePair<string, object>("exception", ex.GetType().Name));

                _logger.LogError(ex, "Error processing request {Method} {Path}", method, path);
                throw; // Re-throw to let error handling middleware deal with it
            }
            finally
            {
                stopwatch.Stop();

                // Log the request duration for debugging/testing
                if (context.Response.StatusCode >= 400)
                {
                    // Record error if not caught by exception above
                    OpenTelemetryExtensions.HttpErrorCounter.Add(
                        1,
                        new KeyValuePair<string, object>("path", path),
                        new KeyValuePair<string, object>("method", method),
                        new KeyValuePair<string, object>("status_code", context.Response.StatusCode));
                }

                _logger.LogDebug("Request {Method} {Path} completed in {ElapsedMilliseconds}ms with status {StatusCode}",
                    method, path, stopwatch.ElapsedMilliseconds, context.Response.StatusCode);
            }
        }
    }

    /// <summary>
    /// Extensions for adding OpenTelemetry middleware to the application pipeline
    /// </summary>
    public static class OpenTelemetryMiddlewareExtensions
    {
        /// <summary>
        /// Adds the OpenTelemetry middleware to the application pipeline
        /// </summary>
        /// <param name="builder">The application builder</param>
        /// <returns>The application builder for chaining</returns>
        public static IApplicationBuilder UseOpenTelemetryMetrics(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<OpenTelemetryMiddleware>();
        }
    }
}
