namespace Core.Configuration
{
    /// <summary>
    /// Configuration settings for HTTP client resilience policies.
    /// </summary>
    public class ResilienceOptions
    {
        /// <summary>
        /// Gets or sets the number of retry attempts for transient failures.
        /// </summary>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// Gets or sets the base delay between retries in seconds.
        /// </summary>
        public double RetryDelaySeconds { get; set; } = 2;

        /// <summary>
        /// Gets or sets the request timeout in seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the number of exceptions or failures before breaking the circuit.
        /// </summary>
        public int CircuitBreakerThreshold { get; set; } = 5;

        /// <summary>
        /// Gets or sets the duration of the circuit break in seconds.
        /// </summary>
        public int CircuitBreakerDurationSeconds { get; set; } = 30;
    }
}
