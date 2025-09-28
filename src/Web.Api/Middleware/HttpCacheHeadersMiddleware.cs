// <copyright file="HttpCacheHeadersMiddleware.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Web.Api.Middleware
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Middleware to process HTTP cache headers.
    /// </summary>
    public class HttpCacheHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private const string NoCacheHeaderKey = "CacheControl";
        private const string NoCacheHeaderValue = "no-cache";

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpCacheHeadersMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next delegate in the pipeline.</param>
        public HttpCacheHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Invokes the middleware.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task that represents the completion of request processing.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            // Check for Cache-Control: no-cache header
            if (context.Request.Headers.TryGetValue("Cache-Control", out var cacheControlValues))
            {
                var values = cacheControlValues.ToString().Split(',');
                foreach (var directive in values)
                {
                    if (directive.Trim().Equals("no-cache", System.StringComparison.OrdinalIgnoreCase))
                    {
                        // Store the no-cache directive in the HttpContext.Items for later retrieval
                        context.Items[NoCacheHeaderKey] = NoCacheHeaderValue;
                        break;
                    }
                }
            }

            await _next(context);
        }

        /// <summary>
        /// Checks if the request has a Cache-Control: no-cache header.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>True if the no-cache directive is present; otherwise, false.</returns>
        public static bool HasNoCacheHeader(HttpContext context)
        {
            return context?.Items.TryGetValue(NoCacheHeaderKey, out var value) == true &&
                   NoCacheHeaderValue.Equals(value?.ToString());
        }
    }
}
