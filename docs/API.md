# QuantLab API — Starter Endpoints

Base path: `/api`

## Assets

### GET `/api/assets`

Returns all assets.

### GET `/api/assets/{id}`

Returns one asset or `404 Not Found`.

## Prices

### GET `/api/assets/{id}/prices`

Query parameters:

```text
from
to
limit
```

Example:

```text
GET /api/assets/1/prices?from=2025-01-01&to=2025-12-31&limit=1000
```

## Backtests

### POST `/api/backtests`

```json
{
  "assetId": 1,
  "strategy": "moving-average-crossover",
  "shortWindow": 20,
  "longWindow": 50,
  "initialCapital": 10000
}
```

### GET `/api/backtests/{id}`

Return configuration, performance metrics, trades, and the equity curve.

## Rules

- Use DTOs rather than exposing database entities.
- Validate inputs.
- Return appropriate HTTP status codes.
- Keep quant calculations out of controllers.
- Document endpoints with Swagger/OpenAPI.
