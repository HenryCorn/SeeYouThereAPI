// <copyright file="HttpContextCacheHeadersService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Web.Api.Services
{
    using Core.Infrastructure;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Implementation of ICacheHeadersService that uses HttpContext to check for cache headers.
    /// </summary>
    public class HttpContextCacheHeadersService : ICacheHeadersService
    {
        private const string NoCacheHeaderValue = "no-cache";
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContextCacheHeadersService"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        public HttpContextCacheHeadersService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc/>
        public bool HasNoCacheHeader()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return false;
            }

            if (httpContext.Request.Headers.TryGetValue("Cache-Control", out var cacheControlValues))
            {
                var values = cacheControlValues.ToString().Split(',');
                foreach (var directive in values)
                {
                    if (directive.Trim().Equals(NoCacheHeaderValue, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
