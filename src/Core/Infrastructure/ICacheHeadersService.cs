// <copyright file="ICacheHeadersService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Core.Infrastructure
{
    /// <summary>
    /// Service interface for checking HTTP cache headers.
    /// </summary>
    public interface ICacheHeadersService
    {
        /// <summary>
        /// Checks if the current request has a no-cache header.
        /// </summary>
        /// <returns>True if the request has a no-cache header; otherwise, false.</returns>
        bool HasNoCacheHeader();
    }
}
