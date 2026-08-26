# InventoryWarehouseApi

InventoryWarehouseApi is a production-style ASP.NET Core inventory and warehouse backend demonstrating transactional inventory consistency, multi-warehouse workflows, JWT authorization, EF Core and Dapper persistence, SQL Server concurrency control, background processing, and relational automated testing.

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4) ![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-Web_API-512BD4) ![SQL Server](https://img.shields.io/badge/SQL_Server-relational-CC2927) ![EF Core 10](https://img.shields.io/badge/EF_Core-10-512BD4) ![Dapper](https://img.shields.io/badge/Dapper-2.1.66-2C3E50)

## Engineering highlights

- Location balances with derived, never-persisted available quantity
- Atomic, reservation-safe stock operations and multi-item transfers
- Immutable physical movement ledger and adjustment audit trail
- Serializable transactions with SQL Server writer-intent locking
- Rotating, SHA-256-hashed refresh tokens and permission authorization
- EF Core transactional persistence plus focused Dapper read models
- Persistent low-stock monitoring with a failure-resilient worker
- Sanitized Problem Details, structured logging, and relational tests

## Capabilities

Products, warehouses, locations, inventory balances, Stock In/Out, adjustments, transfers, reservations, low-stock thresholds and alerts, JWT authentication, Admin user management, and five read-heavy inventory reports.

## Technology stack

| Area | Technology |
| --- | --- |
| Runtime / API | .NET 10; ASP.NET Core Web API 10.0.11 |
| Persistence | EF Core 10.0.11; Dapper 2.1.66; SQL Server; SQLite in-memory tests |
| Security | JWT Bearer 10.0.11; `PasswordHasher`; hashed refresh tokens; role/permission policies |
| Validation / errors / logs | FluentValidation 12.1.1; Problem Details; Serilog.AspNetCore 10.0.0 |
| Documentation | ASP.NET Core OpenAPI 10.0.11; Scalar.AspNetCore 2.17.1 |
| Testing | xUnit 2.9.3; WebApplicationFactory 10.0.11; EF Core SQLite 10.0.11 |

## Architecture

```text
Api -> Application, Infrastructure
Infrastructure -> Application, Domain
Application -> Domain
Domain -> no project dependencies
```

Transactional commands use application services and EF Core repositories. Read-heavy reports use an application abstraction implemented by Dapper with explicit SQL. This is a focused persistence split, not full CQRS. See [Architecture](docs/ARCHITECTURE.md).

## Inventory consistency

`AvailableQuantity = OnHandQuantity - ReservedQuantity`

On-hand is physical stock; reserved is allocated but not issued; available is usable for issue or allocation. Availability is derived and never persisted. Domain logic and database constraints enforce `OnHandQuantity >= 0`, `ReservedQuantity >= 0`, and `ReservedQuantity <= OnHandQuantity`.

Stock Out and reservations are limited by availability. Release changes allocation only; fulfillment reduces on-hand and reserved quantities and creates one StockOut. Adjustment decreases preserve reserved stock. Transfer completion revalidates all source items and commits every balance, ledger, link, and status change atomically.

## Core workflows

- **Stock In / Out:** changes physical stock and appends an immutable ledger entry.
- **Adjustments:** require a reason, record the authenticated user, and link an audit record to a movement.
- **Transfers:** `Pending -> Completed`; creation does not reserve stock, completion revalidates and writes paired TransferOut/TransferIn movements atomically.
- **Reservations:** `Active -> Released` or `Active -> Fulfilled`; release has no movement, fulfillment creates one StockOut, and no partial lifecycle exists.
- **Low stock:** one threshold per product/warehouse/location; `AvailableQuantity <= ThresholdQuantity` is low and missing balances count as zero. Persistent alerts are separate from the read-only Dapper report.

## API overview

| Area | Method | Route | Permission / purpose |
| --- | --- | --- | --- |
| Health | GET | `/health` | Anonymous |
| Authentication | POST / GET | `/api/auth/login`, `/refresh`, `/logout`, `/me` | First three anonymous; `/me` authenticated |
| User administration | POST / GET / PUT | `/api/users`, `/{id}`, `/{id}/role`, `/{id}/status` | Admin only |
| Products | GET / POST / PUT / DELETE | `/api/products`, `/{id}` | Catalog Read / Manage |
| Warehouses | GET / POST / PUT / DELETE | `/api/warehouses`, `/{id}` | Catalog Read / Manage |
| Locations | GET / POST / PUT / DELETE | `/api/warehouses/{warehouseId}/locations`, `/{locationId}` | Catalog Read / Manage |
| Inventory | GET / POST | `/api/inventory/products/{productId}/warehouses/{warehouseId}` plus location, stock and movement suffixes | Inventory Read / Operate |
| Adjustments | POST / GET | inventory location `/adjustments/increase`, `/decrease`, `/adjustments` | Inventory Adjust / Read |
| Transfers | POST / GET | `/api/warehouse-transfers`, `/{id}`, `/{id}/complete` | Inventory Operate / Read |
| Reservations | POST / GET | `/api/inventory-reservations`, `/{id}`, `/{id}/release`, `/{id}/fulfill` | Inventory Operate / Read |
| Low stock | PUT / GET | `/api/low-stock-thresholds...`, `/api/low-stock`, `/api/low-stock-alerts` | Low Stock Manage / Inventory Read |
| Reports | GET | `/api/reports/inventory-summary`, `/stock-movements`, `/warehouses/{warehouseId}/inventory`, `/low-stock`, `/products/{productId}/stock-history` | Inventory Read |

See [API examples](docs/API_EXAMPLES.md) and Development Scalar for the complete generated operation list.

## Authorization matrix

| Capability | Admin | InventoryManager | WarehouseOperator | Viewer |
| --- | :---: | :---: | :---: | :---: |
| Catalog Read | ✓ | ✓ | ✓ | ✓ |
| Catalog Manage | ✓ | ✓ | — | — |
| Inventory Read | ✓ | ✓ | ✓ | ✓ |
| Inventory Operate | ✓ | ✓ | ✓ | — |
| Inventory Adjust | ✓ | ✓ | — | — |
| Low Stock Manage | ✓ | ✓ | — | — |
| User Manage | ✓ | — | — | — |

## Authentication and security

Login returns a short-lived JWT (15 minutes by default) and rotating refresh token (seven days). Only SHA-256 refresh-token hashes are persisted. Logout revokes its token. Status or role changes revoke active refresh tokens, while access tokens remain valid to normal expiry. The last active Admin cannot be demoted or deactivated.

Production secrets must stay outside source. The checked-in signing key is Development-only. Supply production connection strings, signing keys, and bootstrap passwords with environment configuration or a secret provider.

## Quick start

Prerequisites: a compatible .NET 10 SDK and SQL Server LocalDB or SQL Server.

```powershell
git clone <repository-url>
cd InventoryWarehouseApi
dotnet restore
dotnet tool restore
dotnet ef database update --project src/InventoryWarehouseApi.Infrastructure --startup-project src/InventoryWarehouseApi.Api --context InventoryWarehouseDbContext
dotnet run --project src/InventoryWarehouseApi.Api
```

Example external configuration:

```powershell
$env:ConnectionStrings__DefaultConnection = '<sql-server-connection-string>'
$env:Authentication__Jwt__Issuer = 'InventoryWarehouseApi'
$env:Authentication__Jwt__Audience = 'InventoryWarehouseApi.Client'
$env:Authentication__Jwt__SigningKey = '<at-least-32-byte-secret-signing-key>'
$env:Authentication__DevelopmentAdmin__Enabled = 'true'
$env:Authentication__DevelopmentAdmin__Email = 'admin@example.local'
$env:Authentication__DevelopmentAdmin__DisplayName = 'Development Admin'
$env:Authentication__DevelopmentAdmin__Password = '<choose-a-strong-development-password>'
```

The Admin bootstrap is Development-only, disabled by default, idempotent, and does not reset an existing password. The application does not automatically apply migrations. The HTTPS development profile uses `https://localhost:7140` (also HTTP `5065`); Scalar is `/scalar/v1`, OpenAPI `/openapi/v1.json`, and health `/health`.

## Database and concurrency

SQL Server is the development/production provider. Nine EF Core migrations exist through `AddReportingIndexes`; reporting indexes support movement timelines and warehouse inventory. Database constraints reinforce balance and movement invariants.

Critical mutations use Serializable transactions. SQL Server mutation reads use `WITH (UPDLOCK, HOLDLOCK)`, preventing shared-to-write conversion deadlocks in the tested inventory pattern: competitors wait and re-evaluate committed state, and normal business conflicts become 409. Transfer balances use deterministic key ordering and the transfer is locked before status evaluation. This is not universal deadlock elimination, distributed locking, a global retry framework, or blind deadlock-to-409 mapping.

## Errors and observability

Problem Details cover validation (400), authentication (401), authorization (403), missing resources (404), business conflicts (409), and sanitized unexpected failures (500). Serilog provides request and structured application logs. The low-stock worker logs scans/failures and recovers on its next iteration; no external logging infrastructure is implied.

The generated OpenAPI document supplies concise operation summaries, business-focused descriptions, typed success responses, and applicable `application/problem+json` error contracts for Scalar.

## Testing

```powershell
dotnet build InventoryWarehouseApi.slnx --configuration Release
dotnet test tests/InventoryWarehouseApi.UnitTests --configuration Release
dotnet test tests/InventoryWarehouseApi.IntegrationTests --configuration Release
dotnet test InventoryWarehouseApi.slnx --configuration Release
```

Verified baseline: **95 unit + 76 integration = 171 total; 0 failed; 0 skipped**. Integration tests use `WebApplicationFactory` and an isolated shared in-memory SQLite database per host, exercising relational constraints and EF Core/Dapper against the same database where needed.

SQLite does not prove SQL Server locks. Real SQL Server acceptance separately covered competing Stock Out, reservations, same-transfer completion, refresh rotation, and last-active-Admin mutation.

## Project structure

```text
src/
  InventoryWarehouseApi.Api
  InventoryWarehouseApi.Application
  InventoryWarehouseApi.Domain
  InventoryWarehouseApi.Infrastructure
tests/
  InventoryWarehouseApi.UnitTests
  InventoryWarehouseApi.IntegrationTests
docs/
  ARCHITECTURE.md
  API_EXAMPLES.md
```

API owns HTTP composition; Application owns use cases and boundaries; Domain owns entities and invariants; Infrastructure owns EF Core, Dapper, SQL Server, and security persistence.

## Scope boundary

This backend intentionally excludes full ERP, accounting/invoicing, purchasing, sales orders, shipping, forecasting/ML, microservices, Kubernetes, production cloud infrastructure, and a frontend. The historical roadmap remains in [PROJECT_PLAN.md](PROJECT_PLAN.md).
