// <copyright file="ValidationProblemDetailsFactory.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Web.Api.Infrastructure
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using Microsoft.Extensions.Options;
    using System.Diagnostics;

    /// <summary>
    /// Factory for creating ProblemDetails instances from ModelStateDictionary.
    /// </summary>
    public class ValidationProblemDetailsFactory : ProblemDetailsFactory
    {
        private readonly ApiBehaviorOptions _options;
        private readonly Action<ProblemDetailsContext>? _configure;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationProblemDetailsFactory"/> class.
        /// </summary>
        /// <param name="options">Problem details options.</param>
        /// <param name="problemDetailsOptions">Application problem details options.</param>
        public ValidationProblemDetailsFactory(
            IOptions<ApiBehaviorOptions> options,
            IOptions<ProblemDetailsOptions>? problemDetailsOptions = null)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _configure = problemDetailsOptions?.Value?.CustomizeProblemDetails;
        }

        /// <inheritdoc/>
        public override ProblemDetails CreateProblemDetails(
            HttpContext httpContext,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null)
        {
            statusCode ??= 500;

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance,
            };

            ApplyProblemDetailsDefaults(httpContext, problemDetails, statusCode.Value);

            return problemDetails;
        }

        /// <inheritdoc/>
        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext httpContext,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null)
        {
            if (modelStateDictionary == null)
            {
                throw new ArgumentNullException(nameof(modelStateDictionary));
            }

            statusCode ??= 400;

            var problemDetails = new ValidationProblemDetails(modelStateDictionary)
            {
                Status = statusCode,
                Type = type ?? "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = title ?? "One or more validation errors occurred",
                Detail = detail,
                Instance = instance,
            };

            ApplyProblemDetailsDefaults(httpContext, problemDetails, statusCode.Value);

            return problemDetails;
        }

        private void ApplyProblemDetailsDefaults(HttpContext httpContext, ProblemDetails problemDetails, int statusCode)
        {
            problemDetails.Status ??= statusCode;

            if (_configure != null)
            {
                var context = new ProblemDetailsContext
                {
                    HttpContext = httpContext,
                    ProblemDetails = problemDetails
                };

                _configure(context);
            }

            // Add trace ID for better debugging
            problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        }
    }
}
