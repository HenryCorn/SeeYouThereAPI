// <copyright file="SerilogSensitiveDataFilter.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Web.Api.Infrastructure
{
    using Serilog.Core;
    using Serilog.Events;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Serilog filter to redact sensitive information in log events.
    /// </summary>
    public class SerilogSensitiveDataFilter : ILogEventFilter
    {
        private readonly HashSet<string> _sensitiveProperties = new(new[]
        {
            "password",
            "apiKey",
            "token",
            "secret",
            "credential",
            "connectionString",
            "authorization",
            "access_token",
            "refresh_token",
            "client_secret",
        });

        /// <summary>
        /// Determines whether the specified log event should be emitted and redacts sensitive properties.
        /// </summary>
        /// <param name="logEvent">The log event to filter.</param>
        /// <returns>True if the event should be emitted, otherwise false.</returns>
        public bool IsEnabled(LogEvent logEvent)
        {
            foreach (var property in logEvent.Properties
                .Where(p => _sensitiveProperties.Any(s =>
                    p.Key.Contains(s, System.StringComparison.OrdinalIgnoreCase))))
            {
                logEvent.RemovePropertyIfPresent(property.Key);
                logEvent.AddPropertyIfAbsent(new LogEventProperty(
                    property.Key,
                    new ScalarValue("[REDACTED]")));
            }

            // Also check exception data for sensitive info
            if (logEvent.Exception != null)
            {
                // Handle sensitive info in exception messages
                RedactSensitiveExceptionData(logEvent.Exception);
            }

            return true;
        }

        private void RedactSensitiveExceptionData(System.Exception exception)
        {
            // This is a simplified implementation
            // In a real-world scenario, you might want to create a deep copy of the exception
            // and redact sensitive data from the message, stack trace, etc.

            // Recursively process inner exceptions
            if (exception.InnerException != null)
            {
                RedactSensitiveExceptionData(exception.InnerException);
            }
        }
    }
}
