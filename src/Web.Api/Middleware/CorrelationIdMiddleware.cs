// <copyright file="CorrelationIdMiddleware.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Web.Api.Middleware
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Serilog.Context;

    /// <summary>
    /// Middleware for handling correlation IDs in requests and responses.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private const string CorrelationIdHeaderName = "X-Correlation-ID";
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationIdMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger.</param>
        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Invokes the middleware to process the request.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            string correlationId = GetOrCreateCorrelationId(context);

            // Add the correlation ID to the response headers
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
            {
                context.Response.Headers.Append(CorrelationIdHeaderName, correlationId);
            }

            // Store correlation ID in HttpContext items for other middleware and controllers to access
            context.Items["CorrelationId"] = correlationId;

            // Push the correlation ID into Serilog's LogContext
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                _logger.LogDebug("Request {Method} {Path} has correlation ID {CorrelationId}",
                    context.Request.Method, context.Request.Path, correlationId);

                await _next(context);

                _logger.LogDebug("Response for {Method} {Path} completed with status {StatusCode}",
                    context.Request.Method, context.Request.Path, context.Response.StatusCode);
            }
        }

        private static string GetOrCreateCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var existingCorrelationId))
            {
                return existingCorrelationId;
            }

            return Guid.NewGuid().ToString();
        }
    }
}
