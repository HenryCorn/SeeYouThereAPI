// <copyright file="CacheOptions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Core.Configuration
{
    /// <summary>
    /// Configuration options for the caching system.
    /// </summary>
    public class CacheOptions
    {
        /// <summary>
        /// Gets or sets the time-to-live for cached flight search results in minutes.
        /// </summary>
        public int FlightSearchCacheTtlMinutes { get; set; } = 10;

        /// <summary>
        /// Gets or sets a value indicating whether caching is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}
