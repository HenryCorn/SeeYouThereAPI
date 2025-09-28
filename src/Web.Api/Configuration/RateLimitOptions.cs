// <copyright file="RateLimitOptions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Web.Api.Configuration
{
    /// <summary>
    /// Configuration options for the API's rate limiting.
    /// </summary>
    public class RateLimitOptions
    {
        /// <summary>
        /// Gets or sets the number of tokens in the bucket (maximum burst).
        /// </summary>
        public int TokenLimit { get; set; } = 100;

        /// <summary>
        /// Gets or sets the number of tokens added per replenishment period.
        /// </summary>
        public int TokensPerPeriod { get; set; } = 20;

        /// <summary>
        /// Gets or sets the token replenishment period in seconds.
        /// </summary>
        public int ReplenishmentPeriodSeconds { get; set; } = 60;

        /// <summary>
        /// Gets or sets the queue limit for pending requests.
        /// </summary>
        public int QueueLimit { get; set; } = 2;

        /// <summary>
        /// Gets or sets the time in seconds a client should wait before retrying.
        /// </summary>
        public int RetryAfterSeconds { get; set; } = 60;
    }
}
