// <copyright file="HttpCacheHeadersMiddlewareExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Web.Api.Middleware
{
    using Microsoft.AspNetCore.Builder;

    /// <summary>
    /// Extension methods for the HttpCacheHeadersMiddleware.
    /// </summary>
    public static class HttpCacheHeadersMiddlewareExtensions
    {
        /// <summary>
        /// Adds the HTTP cache headers middleware to the application pipeline.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder with the middleware added.</returns>
        public static IApplicationBuilder UseHttpCacheHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HttpCacheHeadersMiddleware>();
        }
    }
}
