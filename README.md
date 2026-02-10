# API Aggregator Service

A .NET 8 ASP.NET Core Web API that aggregates data from multiple external APIs (OpenWeatherMap, News API, GitHub) into a unified endpoint with filtering, sorting, caching, and statistics tracking.

## Features

- **Plugin Architecture**: Easily add new API sources by implementing `IApiPlugin`
- **Multi-API Aggregation**: Fetches data from OpenWeatherMap, News API, and GitHub in parallel
- **Filtering & Sorting**: Category filtering and plugin-level sorting (`ISortablePlugin`)
- **Resilience**: Polly-based retry (3x exponential backoff), circuit breaker, and timeout policies
- **Caching**: In-memory caching with configurable expiration (default: 5 min)
- **JWT Authentication**: Token-based security for all endpoints
- **Statistics**: Thread-safe request tracking with performance buckets (Fast/Average/Slow)
- **Error Handling**: Global exception middleware with standardized error responses
- **Input Validation**: Dynamic validation of categories and sort fields against registered plugins

## Quick Start (5 minutes)

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 1. Clone & Build

```bash
git clone <repository-url>
cd ApiAggregator
dotnet build
```

### 2. Get Free API Keys

| API | Sign Up | Free Tier |
|-----|---------|-----------|
| OpenWeatherMap | [openweathermap.org/api](https://openweathermap.org/api) | 1,000 calls/day |
| News API | [newsapi.org](https://newsapi.org/) | 100 requests/day |
| GitHub | No key needed | 60 requests/hour |

### 3. Configure Your Keys

Create `src/ApiAggregator.Api/appsettings.Development.json`:

```json
{
  "ApiSettings": {
    "OpenWeatherMap": {
      "ApiKey": "paste-your-openweathermap-key-here"
    },
    "NewsApi": {
      "ApiKey": "paste-your-newsapi-key-here"
    }
  }
}
```

> **Note:** This file is gitignored so your keys stay private.

### 4. Run

```bash
cd src/ApiAggregator.Api
dotnet run
```

The API will be available at `https://localhost:5001` (or `http://localhost:5000`).

### 5. Open Swagger UI

Navigate to [https://localhost:5001/swagger](https://localhost:5001/swagger) to explore the API.

### 6. Authenticate

1. Call `POST /api/auth/token` with:
   ```json
   { "username": "admin", "password": "password" }
   ```
2. Copy the token from the response
3. Click **Authorize** in Swagger and enter: `Bearer <your-token>`

## API Endpoints

### POST /api/auth/token
Generates a JWT token for authentication.

### GET /api/aggregation
Fetches and aggregates data from external APIs.

**Query Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `city` | string | One of `city` or `query` | City for weather data (e.g., "London") |
| `query` | string | One of `city` or `query` | Search query for news and GitHub |
| `category` | string | No | Filter: `weather`, `news`, `github`, or `all` (default) |
| `sortBy` | string | No | Sort field: `date`, `stars` (depends on data source) |
| `sortOrder` | string | No | Sort order: `asc` or `desc` (default: `desc`) |

**Example:**

```bash
curl -H "Authorization: Bearer <token>" \
  "https://localhost:5001/api/aggregation?city=London&query=dotnet&sortBy=date&sortOrder=desc"
```

**Response:**

```json
{
  "data": {
    "weather": {
      "city": "London",
      "country": "GB",
      "temperature": 15.5,
      "description": "partly cloudy"
    },
    "news": [
      {
        "title": "Latest .NET News",
        "source": "TechNews",
        "publishedAt": "2024-01-15T10:00:00Z"
      }
    ],
    "github": [
      {
        "name": "dotnet/runtime",
        "stars": 12500,
        "language": "C#"
      }
    ]
  },
  "errors": [],
  "timestamp": "2024-01-15T12:00:00Z"
}
```

### GET /api/statistics
Returns request statistics for all tracked APIs with performance buckets.

### GET /api/statistics/{apiName}
Returns statistics for a specific API (e.g., `OpenWeatherMap`, `NewsAPI`, `GitHub`).

## Running Tests

```bash
dotnet test
```

## Project Structure

```
ApiAggregator/
├── src/
│   └── ApiAggregator.Api/
│       ├── Controllers/        # API endpoints
│       ├── Services/
│       │   └── Interfaces/     # IApiPlugin, ISortablePlugin, etc.
│       ├── Clients/            # External API clients (plugins)
│       ├── Models/             # Data models
│       ├── Configuration/      # Settings classes
│       └── Middleware/         # Exception handling
└── tests/
    └── ApiAggregator.Tests/    # Unit tests
```

## Architecture

### Plugin System
Adding a new API source requires just two steps:
1. Create a client class implementing `IApiPlugin` (and optionally `ISortablePlugin`)
2. Register it in `Program.cs` with one line of DI configuration

### Resilience
- **Retry**: 3 attempts with exponential backoff (2s, 4s, 8s)
- **Circuit Breaker**: Opens after 5 failures, stays open for 30 seconds
- **Timeout**: 10 seconds per request

### Caching
- IMemoryCache with 5-minute default expiration
- Cache keys based on plugin category + query

## License

MIT
