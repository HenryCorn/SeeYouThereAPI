// <copyright file="OpenTelemetryOptions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Web.Api.Configuration
{
    /// <summary>
    /// Configuration options for OpenTelemetry
    /// </summary>
    public class OpenTelemetryOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether OpenTelemetry is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the OpenTelemetry Protocol endpoint URL for traces
        /// </summary>
        public string? OtlpEndpoint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether console exporting is enabled
        /// </summary>
        public bool EnableConsoleExporter { get; set; } = true;

        /// <summary>
        /// Gets or sets the service name for telemetry
        /// </summary>
        public string ServiceName { get; set; } = "SeeYouThereApi";
    }
}
