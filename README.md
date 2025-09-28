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

## Getting Started

### Prerequisites

- .NET 6.0 SDK or later
- API keys for flight data providers (Amadeus, Kiwi, etc.)

### Configuration

Configure your flight data provider API keys in `appsettings.json`:

```json
{
  "Amadeus": {
    "ApiKey": "your-amadeus-api-key",
    "ApiSecret": "your-amadeus-api-secret"
  },
  "Kiwi": {
    "ApiKey": "your-kiwi-api-key"
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

## Development

### Running Tests

```bash
dotnet test
```

### Adding a New Flight Provider

1. Create a new implementation of `IFlightSearchClient` interface
2. Register the implementation in the DI container in `Startup.cs`

## License

[License information to be added]

## Contributors

[Contributor information to be added]
