# Historical Prices API

A .NET Core Web API service providing historical and real‑time market data via Fintacharts.

---

## 1. Overview

- **REST** endpoints for:
  * Listing supported market assets  
  * Fetching historical price bars  
  * Getting the latest price  
- **WebSocket** endpoint for real‑time updates  
- **PostgreSQL** cache for historical data  
- **Dockerized** for easy deployment  

---

## 2. Tech Stack

- **Framework:** .NET 8, ASP.NET Core Web API  
- **ORM:** Entity Framework Core + Npgsql  
- **Container:** Docker  

---

## 3. Quick Start

### Local

1. Clone repository  
2. Edit `appsettings.json` (Fintacharts creds & DB connection string)  
3. Run database migrations:  
   ```bash
   dotnet ef database update
4. Start API:
   ```bash
   dotnet run --project historical_prices
5. Browse Swagger: https://localhost:7298/swagger


### Docker
1. Build image:
   ```bash
   docker build -t historical-prices .
2. Run container (map port 80):
   ``` bash
   docker run -p 8080:80 \
    -e ConnectionStrings__DefaultConnection="Host=...;Database=...;Username=...;Password=..." \
    -e Fintacharts__Username="..." \
    -e Fintacharts__Password="..." \
    historical-prices
3. Browse Swagger: http://localhost:8080/swagger

## 4. REST API
GET /api/assets

    Description: List all supported instruments.

    Query: refresh=true (optional) to re-sync from Fintacharts.

GET /api/prices/range

    Description: Historical bars for one instrument.

    Query parameters:

        instrumentId (GUID)

        provider (e.g. oanda)

        interval (int, e.g. 1)

        periodicity (minute/hour/day/week/month)

        start (ISO datetime)

        end (ISO datetime, optional)

GET /api/prices/current

    Description: Latest price (uses cache if <5 min old).

    Query parameters:

        instrumentId (GUID)

        provider (string)

## 5. WebSocket API
URL: wss://<host>/ws
    Subscription request:
```json
{
  "instrumentId": "<GUID>",
  "provider": "<provider>",
  "subscribe": true,
  "kinds": ["ask","bid","last"]
}
```
Server push: price updates as JSON messages
