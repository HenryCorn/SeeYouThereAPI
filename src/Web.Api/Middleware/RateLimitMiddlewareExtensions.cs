// <copyright file="RateLimitMiddlewareExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Web.Api.Middleware
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Web.Api.Configuration;

    /// <summary>
    /// Extension methods for adding rate limiting to the application.
    /// </summary>
    public static class RateLimitMiddlewareExtensions
    {
        /// <summary>
        /// Adds rate limiting services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Action to configure rate limit options.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddApiRateLimiting(
            this IServiceCollection services,
            Action<RateLimitOptions> configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<RateLimitOptions>(options => { });
            }

            return services;
        }

        /// <summary>
        /// Adds rate limiting middleware to the application pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The updated application builder.</returns>
        public static IApplicationBuilder UseApiRateLimiting(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RateLimitMiddleware>();
        }
    }
}
