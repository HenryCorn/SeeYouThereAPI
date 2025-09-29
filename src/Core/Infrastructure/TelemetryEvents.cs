// <copyright file="TelemetryEvents.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Core.Infrastructure
{
    using System.Diagnostics;
    using System.Diagnostics.Metrics;

    /// <summary>
    /// Provides telemetry event sources and metrics for the Core library.
    /// </summary>
    public static class TelemetryEvents
    {
        private const string ServiceName = "SeeYouThereApi";

        // Create activity source for custom tracing
        public static readonly ActivitySource CoreActivitySource = new(ServiceName + ".Core");

        // Create meter for custom metrics
        public static readonly Meter CoreMeter = new(ServiceName + ".Core");

        // Define metrics
        public static readonly Counter<long> CacheHitsCounter = CoreMeter.CreateCounter<long>("cache.hits", "Count of cache hits");
        public static readonly Counter<long> CacheMissesCounter = CoreMeter.CreateCounter<long>("cache.misses", "Count of cache misses");
    }
}
