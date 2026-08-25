# InventoryWarehouseApi

A production-style ASP.NET Core Web API portfolio project for inventory and warehouse management. Phase 07 — Inventory Reservations is complete.

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
- Controlled Increase and Decrease corrections with immutable adjustment audit history
- Required, normalized Reason and caller-supplied AdjustedBy audit metadata
- One linked AdjustmentIncrease or AdjustmentDecrease movement for every successful correction
- Serializable, atomic balance, adjustment, and ledger persistence
- Pending-to-Completed warehouse transfers between exact inventory positions, including same-warehouse/different-location moves
- Multi-product transfer documents with atomic completion, available-stock revalidation, stock conservation, and full rollback
- Linked TransferOut and TransferIn entries in the unified stock-movement ledger
- Active-to-Released/Fulfilled inventory reservations for one exact product position
- Serializable reservation allocation, release, and fulfillment with optional normalized external references
- Fulfillment through one linked StockOut ledger entry without expanding the movement-type catalog
- Database-backed pagination, SKU/code and name search, active-state filtering, and controlled sorting
- FluentValidation request and query validation
- Problem Details responses for validation (400), missing resources (404), conflicts (409), and unexpected errors (500)
- EF Core 10 with SQL Server and migrations through `AddInventoryReservations`
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
POST /api/inventory/products/{productId}/warehouses/{warehouseId}/locations/{locationId}/adjustments/increase
POST /api/inventory/products/{productId}/warehouses/{warehouseId}/locations/{locationId}/adjustments/decrease
GET  /api/inventory/products/{productId}/warehouses/{warehouseId}/locations/{locationId}/adjustments

POST /api/warehouse-transfers
POST /api/warehouse-transfers/{id}/complete
GET  /api/warehouse-transfers/{id}
GET  /api/warehouse-transfers

POST /api/inventory-reservations
POST /api/inventory-reservations/{id}/release
POST /api/inventory-reservations/{id}/fulfill
GET  /api/inventory-reservations/{id}
GET  /api/inventory-reservations
```

Stock command bodies contain a positive `quantity` and may include `referenceType` and `referenceId`; reference fields must be supplied together. Movement history uses `pageNumber` and `pageSize` (maximum 100), is scoped to the exact product/warehouse/location position, and returns newest records first.

Adjustment command bodies require a positive `quantity`, a `reason`, and `adjustedBy`. Increase creates a zero-reserved balance when none exists. Decrease is limited by `AvailableQuantity`, preserving reserved inventory, and returns 409 without writes when unavailable. Every successful adjustment creates one immutable audit record and one linked movement in the existing ledger. Adjustment history is exact-position scoped, newest first, and paged. Until Phase 09 adds authentication, `adjustedBy` is audit metadata supplied by the caller and is not an authenticated identity.

Warehouse transfers move up to 100 distinct products from one exact warehouse/location position to another. Same-warehouse transfers are supported when the locations differ. Creation validates current source availability and persists a `Pending` document, but it does not reserve stock, change balances, or create movements. Completion revalidates current `AvailableQuantity` inside one Serializable transaction, issues only unreserved source stock, receives it at the destination, and records paired `TransferOut`/`TransferIn` movements with one shared timestamp and transfer reference. Any failing item rolls back every balance, movement, link, and status change, preserving total on-hand stock. Transfer detail and deterministic newest-first paged history are available through the transfer endpoints. Cancellation, in-transit states, and partial completion are not part of this lifecycle.

Inventory reservations have a deliberately small lifecycle: `Active -> Released` or `Active -> Fulfilled`. Creation increases `ReservedQuantity` only, leaving `OnHandQuantity` unchanged and creating no stock movement. It is limited by current `AvailableQuantity`; optional `referenceType`/`referenceId` values are trimmed, length-limited, and must be supplied together. Release frees the full reserved quantity without a physical ledger entry. Fulfillment consumes the full quantity from both on-hand and reserved stock, so availability remains stable relative to immediately before fulfillment, and creates exactly one linked `StockOut` movement using the reservation ID as its reference. Partial release, partial fulfillment, amendment, reassignment, and expiration are not supported in Phase 07.

Reservation create, release, and fulfillment each execute atomically in one Serializable transaction. Existing Stock Out and Warehouse Transfer completion continue to use `AvailableQuantity`, so active reservations protect their allocated stock. Pending warehouse transfers still do **not** reserve inventory; inventory reservations are the explicit allocation mechanism.

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

Tests cover domain invariants, validation, relational constraints, HTTP behavior, atomic stock operations, adjustments, transfers, reservations, and low-stock monitoring.

## Low-stock monitoring

Phase 08 configures one threshold per exact product, warehouse, and warehouse-location position through `PUT /api/low-stock-thresholds/products/{productId}/warehouses/{warehouseId}/locations/{locationId}`. Threshold administration supports exact lookup and paged filtering. Enabled thresholds are operationally low when `AvailableQuantity <= ThresholdQuantity`; the comparison is inclusive, and a configured position without an inventory balance participates as zero on-hand, reserved, and available stock.

`GET /api/low-stock` returns active-master-data positions using deterministic, database-side pagination. Because availability is `OnHandQuantity - ReservedQuantity`, a reservation can make a position low even while physical on-hand stock remains. Stock Out and Stock In affect the query naturally through balances; monitoring itself never mutates inventory or creates a `StockMovement`.

Persistent alerts keep one active alert per threshold. Repeated scans update its observation rather than duplicate it; recovery, disabling a threshold, lowering a threshold below current availability, or deactivating master data resolves it. A later recurrence creates a new historical alert. `GET /api/low-stock-alerts` exposes paged active and resolved history.

The hosted worker runs immediately and sequentially, survives iteration failures, and uses a fresh dependency-injection scope per scan. `LowStockMonitoring:Enabled` controls execution; the default interval is 60 seconds and Development uses 10 seconds. Integration-test hosts disable automatic execution and invoke the monitoring service directly for deterministic coverage. Logs contain structured scan counts and failures.

Phase 08 supplies operational queries and alerts. Optimized Dapper low-stock reporting remains Phase 10.

See [PROJECT_PLAN.md](PROJECT_PLAN.md) for the phased roadmap. The next phase is Phase 09 — Authentication & Authorization (Not Started).
