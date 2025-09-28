// <copyright file="ProblemDetailsExceptionFilter.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Web.Api.Filters
{
    using System.Diagnostics;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Exception filter that converts exceptions to RFC7807 Problem Details.
    /// </summary>
    public class ProblemDetailsExceptionFilter : IExceptionFilter
    {
        private readonly IHostEnvironment _environment;
        private readonly ILogger<ProblemDetailsExceptionFilter> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProblemDetailsExceptionFilter"/> class.
        /// </summary>
        /// <param name="environment">The host environment.</param>
        /// <param name="logger">The logger.</param>
        public ProblemDetailsExceptionFilter(IHostEnvironment environment, ILogger<ProblemDetailsExceptionFilter> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        /// <inheritdoc/>
        public void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, "Unhandled exception occurred");

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Instance = context.HttpContext.Request.Path
            };

            // Include stack trace in development environments
            if (_environment.IsDevelopment())
            {
                problemDetails.Detail = context.Exception.ToString();
                problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
            }

            context.Result = new ObjectResult(problemDetails)
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };

            context.ExceptionHandled = true;
        }
    }
}
