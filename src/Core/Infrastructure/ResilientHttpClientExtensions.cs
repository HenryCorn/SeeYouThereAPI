using System;
using System.Net;
using System.Net.Http;
using System.Linq;
using Core.Configuration;
using Core.External.Amadeus;
using Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace Core.Infrastructure
{
    /// <summary>
    /// Extension methods for configuring resilient HTTP clients.
    /// </summary>
    public static class ResilientHttpClientExtensions
    {
        /// <summary>
        /// Adds a resilient HTTP client for the Amadeus flight search API.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">The resilience options.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddResilientAmadeusClient(
            this IServiceCollection services,
            ResilienceOptions options)
        {
            services.AddHttpClient<IFlightSearchClient, AmadeusFlightSearchClient>()
                .AddPolicyHandler((serviceProvider, request) =>
                    GetRetryPolicy(serviceProvider, options))
                .AddPolicyHandler((serviceProvider, request) =>
                    GetCircuitBreakerPolicy(serviceProvider, options))
                .AddPolicyHandler((serviceProvider, request) =>
                    GetTimeoutPolicy(options));

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(
            IServiceProvider serviceProvider,
            ResilienceOptions options)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<AmadeusFlightSearchClient>>();

            return HttpPolicyExtensions
                .HandleTransientHttpError() // HttpRequestException, 5XX status codes, 408 (request timeout)
                .OrResult(response => response.StatusCode == HttpStatusCode.TooManyRequests) // 429 Too Many Requests
                .Or<TimeoutRejectedException>() // Thrown by Polly's timeout policy
                .WaitAndRetryAsync(
                    options.RetryCount,
                    retryAttempt => TimeSpan.FromSeconds(options.RetryDelaySeconds) +
                                    TimeSpan.FromMilliseconds(new Random().Next(0, 1000)), // Add jitter
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        var correlationId = GetCorrelationId(outcome?.Result?.RequestMessage);

                        logger.LogWarning(
                            "Retrying request {CorrelationId} due to {StatusCode}. Retry attempt {RetryAttempt} after {DelayMs}ms",
                            correlationId,
                            outcome?.Result?.StatusCode,
                            retryAttempt,
                            timespan.TotalMilliseconds);
                    });
        }

        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(
            IServiceProvider serviceProvider,
            ResilienceOptions options)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<AmadeusFlightSearchClient>>();

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: options.CircuitBreakerThreshold,
                    durationOfBreak: TimeSpan.FromSeconds(options.CircuitBreakerDurationSeconds),
                    onBreak: (outcome, timespan, context) =>
                    {
                        logger.LogWarning(
                            "Circuit breaker opened for {DurationSec}s due to failures. Last status code: {StatusCode}",
                            timespan.TotalSeconds,
                            outcome.Result?.StatusCode);
                    },
                    onReset: (context) =>
                    {
                        logger.LogInformation("Circuit breaker reset - provider is accepting requests again");
                    },
                    onHalfOpen: () =>
                    {
                        logger.LogInformation("Circuit breaker half-open - testing if provider is healthy");
                    });
        }

        private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(ResilienceOptions options)
        {
            return Policy.TimeoutAsync<HttpResponseMessage>(
                TimeSpan.FromSeconds(options.TimeoutSeconds));
        }

        private static string GetCorrelationId(HttpRequestMessage request)
        {
            if (request?.Headers.TryGetValues("X-Correlation-Id", out var values) == true)
            {
                return values.FirstOrDefault() ?? "unknown";
            }
            return "unknown";
        }
    }
}
