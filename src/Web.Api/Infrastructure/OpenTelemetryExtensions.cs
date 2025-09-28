// <copyright file="OpenTelemetryExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Core.External;
using Core.Infrastructure;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Web.Api.Configuration;
using Web.Api.Middleware;

namespace Web.Api.Infrastructure
{
    /// <summary>
    /// Extension methods for configuring OpenTelemetry services
    /// </summary>
    public static class OpenTelemetryExtensions
    {
        // Define source name constants
        private const string ServiceName = "SeeYouThereApi";

        // Create activity sources for custom tracing
        public static readonly ActivitySource ApiActivitySource = new(ServiceName);

        // Create meters for custom metrics
        public static readonly Meter ApiMeter = new(ServiceName);

        // Define metrics
        public static readonly Counter<long> HttpRequestCounter = ApiMeter.CreateCounter<long>("http.requests.total", "Count of HTTP requests processed");
        public static readonly Counter<long> HttpErrorCounter = ApiMeter.CreateCounter<long>("http.requests.errors", "Count of HTTP requests resulting in errors");

        /// <summary>
        /// Adds OpenTelemetry services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The application configuration</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind OpenTelemetry options
            services.Configure<OpenTelemetryOptions>(configuration.GetSection("OpenTelemetry"));

            var openTelemetryOptions = configuration.GetSection("OpenTelemetry").Get<OpenTelemetryOptions>();

            if (openTelemetryOptions == null || !openTelemetryOptions.Enabled)
            {
                return services;
            }

            // Configure resource attributes
            var resourceBuilder = ResourceBuilder.CreateDefault()
                .AddService(openTelemetryOptions.ServiceName)
                .AddTelemetrySdk()
                .AddEnvironmentVariableDetector();

            // Add tracing
            services.AddOpenTelemetry()
                .WithTracing(builder =>
                {
                    builder
                        .SetResourceBuilder(resourceBuilder)
                        .AddSource(ServiceName) // Add our custom activity source
                        .AddSource(ServiceName + ".Core") // Add Core activity source
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            // Add correlation ID from middleware to spans
                            options.Filter = httpContext =>
                            {
                                // Exclude health checks and other noise if needed
                                return !httpContext.Request.Path.StartsWithSegments("/health");
                            };
                            options.RecordException = true;
                            options.EnrichWithHttpRequest = (activity, request) =>
                            {
                                var correlationId = request.HttpContext.Items["CorrelationId"] as string;
                                if (!string.IsNullOrEmpty(correlationId))
                                {
                                    activity.SetTag("correlation_id", correlationId);
                                }
                            };
                        })
                        .AddHttpClientInstrumentation(options =>
                        {
                            options.RecordException = true;
                            // Filter out health check calls or other noisy spans if needed
                            options.FilterHttpRequestMessage = (request) =>
                            {
                                return true;
                            };
                        });

                    // Configure exporters
                    if (openTelemetryOptions.EnableConsoleExporter)
                    {
                        builder.AddConsoleExporter();
                    }

                    if (!string.IsNullOrEmpty(openTelemetryOptions.OtlpEndpoint))
                    {
                        builder.AddOtlpExporter(otlpOptions =>
                        {
                            otlpOptions.Endpoint = new Uri(openTelemetryOptions.OtlpEndpoint);
                        });
                    }
                });

            // Add metrics
            services.AddOpenTelemetry()
                .WithMetrics(builder =>
                {
                    builder
                        .SetResourceBuilder(resourceBuilder)
                        .AddMeter(ServiceName) // Add our API meter
                        .AddMeter(ServiceName + ".Core") // Add Core meter
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation();

                    // Configure exporters
                    if (openTelemetryOptions.EnableConsoleExporter)
                    {
                        builder.AddConsoleExporter();
                    }

                    if (!string.IsNullOrEmpty(openTelemetryOptions.OtlpEndpoint))
                    {
                        builder.AddOtlpExporter(otlpOptions =>
                        {
                            otlpOptions.Endpoint = new Uri(openTelemetryOptions.OtlpEndpoint);
                        });
                    }
                });

            return services;
        }
    }
}
