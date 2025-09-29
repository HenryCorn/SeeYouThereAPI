# SeeYouThereAPI

API for the SeeYouThere service. The idea of the project is to have a way of finding cheap flights from different origins to the same destination, so friends/family etc from all around the world can meet conveniently.

## Project Overview

SeeYouThereAPI (as of September 2025) is a flight aggregation service designed to solve the problem of coordinating travel for groups of people coming from different locations. The API finds optimal meeting destinations by analyzing flight prices from multiple origins, helping users identify the most cost-effective places to meet.

## Architecture

The project follows a clean architecture pattern with the following components:

### Core Components

- **Core Library**: Contains domain models, business logic, and interfaces
  - **Models**: Data structures representing flight search requests and results
  - **Interfaces**: Service interfaces like `IFlightSearchClient`
  - **External**: Implementation of external flight API integrations
  - **Validation**: Regional validation logic for restricting searches

### API Components

- **Web.API**: ASP.NET Core web API exposing endpoints for client applications
  - **Controllers**: REST API endpoints for flight searches
  - **Middleware**: Cross-cutting concerns like correlation ID tracking
  - **Filters**: Request validation filters

### Key Features

#### Multi-Origin Flight Search

The API supports searching for flights from multiple origins simultaneously, finding common destinations where all travelers can meet.

#### Destination Aggregation

The system implements several aggregation strategies:

1. **Common Destination Finding**: Identifies destinations available from all specified origins
2. **Total Cost Minimization**: Calculates the total group cost for each potential meeting place
3. **Optimal Destination Selection**: Uses a multi-factor selection algorithm:
   - Primary: Lowest total price across all travelers
   - Tiebreaker 1: Lowest median individual price
   - Tiebreaker 2: Lexicographic ordering by destination city code

#### External Flight Data Providers

The system is designed to work with multiple flight data providers:
- Amadeus API
- Kiwi API
- More providers can be integrated via the `IFlightSearchClient` interface

#### API Protection and Resiliency

The API includes several mechanisms to ensure stability and protect against abuse:

- **Rate Limiting**: Limits requests per client IP address using a fixed window algorithm
  - Configurable token limits and replenishment rates
  - Returns 429 Too Many Requests with Retry-After headers when limits are exceeded
  - Uses RFC7807 problem details format for error responses

- **Request Validation**: Validates incoming requests using FluentValidation
  - Returns detailed 400 Bad Request responses with field-specific error messages
  - Documents validation constraints in Swagger/OpenAPI specifications

- **Resilient HTTP Clients**: Implements resilience patterns for external API calls
  - Circuit breakers to prevent cascading failures
  - Retry policies for transient errors
  - Timeouts for unresponsive external services

#### Caching System

The API includes an in-memory caching system to improve performance and reduce calls to flight data providers:

- **Cache Keys**: Results are cached based on normalized request parameters (origins + region + date + currency)
- **Cache TTL**: Default time-to-live for cached results is 10 minutes (configurable)
- **Cache Bypass**: Clients can bypass the cache by sending the `Cache-Control: no-cache` header
- **Configuration**: Caching can be enabled/disabled and TTL adjusted via application settings

#### Observability

The API includes OpenTelemetry for comprehensive observability:

- **Distributed Tracing**: Tracks request flow through the system and external dependencies
  - Correlation IDs included in traces for request correlation
  - HTTP client instrumentation for tracking external API calls
  - Custom activity sources for internal operations

- **Custom Metrics**: Tracks key performance indicators
  - HTTP request counts with path and method dimensions
  - HTTP error counts with status code information
  - Cache hit/miss ratios for performance optimization
  - Runtime metrics for application health monitoring

- **Export Options**: Flexible telemetry data exporting
  - OpenTelemetry Protocol (OTLP) exporter for integration with observability backends
  - Console exporter for local development and debugging
  - Configurable endpoint settings in application configuration

## Getting Started

### Prerequisites

- .NET 6.0 SDK or later
- API keys for flight data providers (Amadeus, Kiwi, etc.)
- Docker (optional, for containerized deployment)

### Configuration

Configure your flight data provider API keys, cache settings, and OpenTelemetry in `appsettings.json`:

```json
{
  "Amadeus": {
    "ApiKey": "your-amadeus-api-key",
    "ApiSecret": "your-amadeus-api-secret"
  },
  "Kiwi": {
    "ApiKey": "your-kiwi-api-key"
  },
  "Cache": {
    "Enabled": true,
    "FlightSearchCacheTtlMinutes": 10
  },
  "OpenTelemetry": {
    "Enabled": true,
    "OtlpEndpoint": "http://localhost:4317",
    "EnableConsoleExporter": true,
    "ServiceName": "SeeYouThereApi"
  }
}
```

### Building and Running

```bash
# Build the project
dotnet build

# Run the API
cd src/Web.Api
dotnet run
```

### Docker Deployment

The application can be deployed as a containerized service using Docker:

```bash
# Build the Docker image
docker build -t seeyouthere-api .

# Run the container exposing port 8080
docker run -p 8080:8080 seeyouthere-api

# You can override the port if needed
docker run -e PORT=5000 -p 5000:5000 seeyouthere-api
```

The Docker container includes:
- ASP.NET 8 runtime (Alpine-based for minimal image size)
- Non-root user execution for enhanced security
- Health check endpoint at `/health`
- Support for custom port configuration via the `PORT` environment variable

#### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| PORT | Port the API will listen on | 8080 |

### Deployment Options

#### Local Development

#### Google Cloud Platform (GCP)

SeeYouThereAPI can be deployed to Google Cloud Platform using Cloud Run, Cloud Build, and Artifact Registry.

For complete instructions, see the [GCP Deployment Guide](docs/gcp-deployment.md).

Quick start:
```bash
# Configure and run the GCP bootstrap script
chmod +x tools/gcp/setup.sh
./tools/gcp/setup.sh

# Deploy using Cloud Build
gcloud builds submit --config=cloudbuild.yaml
```

## API Usage

### Multi-Origin Flight Search

```http
POST /api/destinations/search
Content-Type: application/json

{
  "origins": ["JFK", "LHR", "SIN"],
  "departureDate": "2025-12-01",
  "returnDate": "2025-12-10",
  "currency": "USD",
  "continentFilter": "EU"
}
```

Response:

```json
{
  "bestDestination": {
    "destinationCityCode": "CDG",
    "destinationCountryCode": "FR",
    "totalPrice": 1200.00,
    "medianPrice": 400.00,
    "currency": "USD",
    "perOriginPrices": {
      "JFK": 500.00,
      "LHR": 300.00,
      "SIN": 400.00
    }
  },
  "allCommonDestinations": ["CDG", "AMS", "FCO", "MAD"]
}
```

### Bypassing Cache

To bypass the cache and force a fresh search from flight providers:

```http
POST /api/destinations/search
Content-Type: application/json
Cache-Control: no-cache

{
  "origins": ["JFK", "LHR", "SIN"],
  "departureDate": "2025-12-01",
  "returnDate": "2025-12-10",
  "currency": "USD",
  "continentFilter": "EU"
}
```

## Development

### Running Tests

```bash
dotnet test
```

### Adding a New Flight Provider

1. Create a new implementation of `IFlightSearchClient` interface
2. Register the implementation in the DI container in `Startup.cs`

### Monitoring and Observability

To visualize the telemetry data:

1. Set up a local OpenTelemetry Collector or use cloud-based observability platforms
2. Configure the OTLP endpoint in `appsettings.json`
3. Run the application and observe traces and metrics

For local development, metrics and traces are also logged to the console when `EnableConsoleExporter` is true.

Example collector configuration:
```yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317

processors:
  batch:

exporters:
  prometheus:
    endpoint: 0.0.0.0:8889
  jaeger:
    endpoint: jaeger:14250
    tls:
      insecure: true

service:
  pipelines:
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [prometheus]
    traces:
      receivers: [otlp]
      processors: [batch]
      exporters: [jaeger]
```

## License

[License information to be added]

## Contributors

[Contributor information to be added]
