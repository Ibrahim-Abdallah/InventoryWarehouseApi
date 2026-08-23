# InventoryWarehouseApi

A production-style ASP.NET Core Web API portfolio project for inventory and warehouse management. Phase 04 — Stock Movement Engine is complete.

## Implemented capabilities

- Product master data CRUD with normalized, case-insensitive unique SKUs
- Warehouse master data CRUD with normalized, case-insensitive unique codes
- Warehouse-location CRUD with warehouse-scoped, normalized unique bin codes and dependency-safe deletes
- Read-only product inventory summaries by warehouse and location, including zero-balance locations
- Location balances as the single source of truth; warehouse totals are database-side aggregates
- `AvailableQuantity = OnHandQuantity - ReservedQuantity`, with domain and database safeguards
- Stock In and Stock Out commands backed by an immutable physical stock-movement ledger
- Available-stock protection: Stock Out cannot consume quantities reserved for later workflows
- Serializable, atomic InventoryBalance update and StockMovement insertion
- Optional, normalized external reference pairs and newest-first paged movement history
- Database-backed pagination, SKU/code and name search, active-state filtering, and controlled sorting
- FluentValidation request and query validation
- Problem Details responses for validation (400), missing resources (404), conflicts (409), and unexpected errors (500)
- EF Core 10 with SQL Server and `InitialCatalog`, `AddWarehouseLocationsAndInventoryBalances`, and `AddStockMovementEngine` migrations
- ASP.NET Core OpenAPI and Scalar API Reference
- Serilog request and structured console logging
- Unit tests and HTTP-pipeline integration tests using isolated SQLite in-memory databases

Physical stock changes use positive quantities; direction is represented by `StockIn` or `StockOut`. Available quantity remains derived and is never persisted. Stock Out returns 409 Conflict without changing the balance or ledger when available stock is insufficient.

## Architecture

```text
Api -> Application, Infrastructure
Infrastructure -> Application, Domain
Application -> Domain
Domain -> no project dependencies
```

Source projects live under `src/`; unit and integration tests live under `tests/`.

## API

Products and warehouses expose matching controller-based endpoints:

```text
GET    /api/products
GET    /api/products/{id}
POST   /api/products
PUT    /api/products/{id}
DELETE /api/products/{id}

GET    /api/warehouses
GET    /api/warehouses/{id}
POST   /api/warehouses
PUT    /api/warehouses/{id}
DELETE /api/warehouses/{id}

GET    /api/warehouses/{warehouseId}/locations
GET    /api/warehouses/{warehouseId}/locations/{locationId}
POST   /api/warehouses/{warehouseId}/locations
PUT    /api/warehouses/{warehouseId}/locations/{locationId}
DELETE /api/warehouses/{warehouseId}/locations/{locationId}

GET /api/inventory/products/{productId}/warehouses/{warehouseId}
GET /api/inventory/products/{productId}/warehouses/{warehouseId}/locations
GET /api/inventory/products/{productId}/warehouses/{warehouseId}/locations/{locationId}
POST /api/inventory/products/{productId}/warehouses/{warehouseId}/locations/{locationId}/stock-in
POST /api/inventory/products/{productId}/warehouses/{warehouseId}/locations/{locationId}/stock-out
GET  /api/inventory/products/{productId}/warehouses/{warehouseId}/locations/{locationId}/movements
```

Stock command bodies contain a positive `quantity` and may include `referenceType` and `referenceId`; reference fields must be supplied together. Movement history uses `pageNumber` and `pageSize` (maximum 100), is scoped to the exact product/warehouse/location position, and returns newest records first.

Collection queries accept `pageNumber` (default 1), `pageSize` (default 20, maximum 100), `search`, `isActive`, `sortBy`, and `sortDirection` (`asc` or `desc`). Products allow `sku`, `name`, `createdAtUtc`, and `updatedAtUtc` sorting; warehouses allow `code`, `name`, `createdAtUtc`, and `updatedAtUtc`. The deterministic default is newest creation time first.

## Local database setup

Prerequisites are .NET SDK 10.0.400 (or a compatible .NET 10 SDK) and SQL Server LocalDB. The development connection in `src/InventoryWarehouseApi.Api/appsettings.json` uses Windows authentication and the `InventoryWarehouseApi` LocalDB database. Override `ConnectionStrings:DefaultConnection` through user secrets or environment configuration for another SQL Server; do not store credentials in source.

Restore the repository-local EF tool and apply the migration:

```powershell
dotnet tool restore
dotnet ef database update --project src/InventoryWarehouseApi.Infrastructure --startup-project src/InventoryWarehouseApi.Api --context InventoryWarehouseDbContext
```

The application does not automatically create, recreate, or migrate the normal database at startup.

## Run locally

```powershell
dotnet restore
dotnet run --project src/InventoryWarehouseApi.Api
```

In Development, Scalar is available at `/scalar/v1`, OpenAPI at `/openapi/v1.json`, and the health endpoint at `GET /health`.

## Build and test

```powershell
dotnet restore
dotnet build
dotnet test
```

Integration tests replace SQL Server with a kept-open SQLite in-memory connection and create isolated relational schemas; they never use or modify the developer database.

Tests cover domain invariants, validation, relational constraints, HTTP behavior, atomic stock operations, movement history, aggregation, zero balances, and safe deletes.

See [PROJECT_PLAN.md](PROJECT_PLAN.md) for the phased roadmap. The next phase is Phase 05 — Stock Adjustments (Not Started).
