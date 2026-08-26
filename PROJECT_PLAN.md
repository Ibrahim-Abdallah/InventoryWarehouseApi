# InventoryWarehouseApi — Project Plan

## 1. Project Overview

`InventoryWarehouseApi` is a production-style ASP.NET Core Web API for managing inventory across multiple warehouses and storage locations.

The project demonstrates practical backend engineering skills commonly required in commercial inventory, retail, e-commerce, and warehouse management systems.

The API will support stock tracking, warehouse transfers, inventory reservations, adjustments, low-stock monitoring, authentication, authorization, reporting, and auditability.

---

## 2. Project Goals

The main goals of this project are to demonstrate:

* Clean ASP.NET Core Web API architecture
* Real-world inventory business rules
* Multi-warehouse inventory management
* Reliable stock movement tracking
* Inventory reservations
* Warehouse-to-warehouse transfers
* Role-based authorization
* SQL Server and Entity Framework Core
* Dapper for reporting/read-heavy queries
* Validation and error handling
* Structured logging
* Background processing
* Automated unit and integration testing
* Professional API documentation
* Portfolio-ready GitHub documentation

---

## 3. Core Technology Stack

* C#
* .NET 10
* ASP.NET Core Web API
* Entity Framework Core
* SQL Server
* Dapper
* JWT Authentication
* Role-Based Authorization
* FluentValidation
* Serilog
* OpenAPI / Scalar
* xUnit
* Git / GitHub

---

## 4. Architecture

The solution will use a layered architecture with clear separation of concerns.

```text
InventoryWarehouseApi
│
├── src
│   ├── InventoryWarehouseApi.Api
│   ├── InventoryWarehouseApi.Application
│   ├── InventoryWarehouseApi.Domain
│   └── InventoryWarehouseApi.Infrastructure
│
└── tests
    ├── InventoryWarehouseApi.UnitTests
    └── InventoryWarehouseApi.IntegrationTests
```

### Domain

Contains:

* Entities
* Enums
* Domain rules
* Domain exceptions
* Value objects when appropriate

The Domain project must not depend on infrastructure concerns.

### Application

Contains:

* Use cases
* DTOs
* Interfaces
* Application services
* Validators
* Business orchestration

### Infrastructure

Contains:

* Entity Framework Core
* SQL Server persistence
* Dapper queries
* Authentication implementation
* Data access implementations
* External infrastructure services

### API

Contains:

* Controllers
* Middleware
* Authentication configuration
* Authorization
* Dependency injection
* OpenAPI / Scalar API Reference
* HTTP-specific concerns

---

## 5. Core Domain

The core domain will include the following concepts.

### Product

Represents an inventory-managed product or SKU.

Typical properties:

* Id
* SKU
* Name
* Description
* IsActive

### Warehouse

Represents a physical warehouse.

Typical properties:

* Id
* Code
* Name
* IsActive

### Warehouse Location

Represents a storage location or bin inside a warehouse.

Typical properties:

* Id
* WarehouseId
* Code
* Name
* IsActive

### Inventory Balance

Represents the current inventory state for a product at a warehouse/location.

Core quantities:

* OnHandQuantity
* ReservedQuantity
* AvailableQuantity

`AvailableQuantity` is derived as:

```text
AvailableQuantity = OnHandQuantity - ReservedQuantity
```

### Stock Movement

Represents an immutable historical inventory transaction.

Examples:

* Stock In
* Stock Out
* Adjustment Increase
* Adjustment Decrease
* Transfer Out
* Transfer In

### Inventory Reservation

Represents stock temporarily reserved for an external business operation such as an order.

### Warehouse Transfer

Represents movement of stock between warehouses or warehouse locations.

---

## 6. Core Business Rules

The project must enforce real inventory rules rather than behaving as a simple CRUD application.

### Inventory Availability

Stock cannot be issued when the requested quantity exceeds available stock.

```text
AvailableQuantity = OnHandQuantity - ReservedQuantity
```

### Reservations

Reservations:

* Increase `ReservedQuantity`
* Do not immediately decrease `OnHandQuantity`
* Cannot exceed available quantity
* Can be released
* Can later be fulfilled

### Stock Movements

Every physical inventory change must generate a stock movement record.

Stock movement history should act as an auditable inventory ledger.

### Adjustments

Manual inventory adjustments must:

* Include a reason
* Record the quantity change
* Record the responsible user
* Generate a stock movement

### Warehouse Transfers

A transfer must not create or destroy inventory.

A completed transfer should result in:

```text
Source Warehouse
    Transfer Out

Destination Warehouse
    Transfer In
```

The total stock across the system must remain consistent.

### Negative Stock

Negative inventory will not be allowed unless explicitly introduced as a future feature.

---

# 7. Development Phases

## Phase 01 — Project Foundation

### Objectives

Create the technical foundation for the entire solution.

### Deliverables

* Solution structure
* API project
* Application project
* Domain project
* Infrastructure project
* Unit test project
* Integration test project
* Project references
* Shared build configuration
* `.editorconfig`
* `.gitignore`
* Basic API configuration
* OpenAPI / Scalar API Reference
* Health check endpoint
* Problem Details
* Structured logging foundation
* Initial integration test
* Initial README
* Successful build and tests

### Definition of Done

* Solution builds successfully
* Tests pass
* API starts successfully
* Scalar API Reference and the OpenAPI document are accessible
* Health endpoint returns success
* Generated IDE/build files are excluded from source control

---

## Phase 02 — Products & Warehouses

### Objectives

Implement the foundational master data required by the inventory system.

### Deliverables

* Product entity
* Warehouse entity
* Product CRUD
* Warehouse CRUD
* SKU uniqueness
* Warehouse code uniqueness
* Validation
* Pagination
* Filtering
* Sorting
* Unit tests
* Integration tests
* EF Core SQL Server persistence and initial catalog migration
* FluentValidation and centralized Problem Details error mapping
* Case-insensitive normalized SKU/code uniqueness with database safeguards

### Definition of Done

Products and warehouses can be reliably managed through documented REST endpoints.

### Status

Completed. Product and Warehouse master-data CRUD, validation, uniqueness, database pagination/search/filtering/sorting, SQL Server persistence, migration, and automated unit/integration coverage are implemented.

---

## Phase 03 — Warehouse Locations & Inventory Balances

### Objectives

Introduce warehouse storage locations and inventory state.

### Deliverables

* WarehouseLocation entity
* InventoryBalance entity
* Warehouse locations CRUD
* Product inventory per warehouse
* Product inventory per location
* On-hand quantity
* Reserved quantity
* Available quantity calculation
* Inventory lookup endpoints
* Validation
* Tests

### Definition of Done

The system can represent where inventory exists and its current quantity state.

### Status

Completed. Warehouse locations, location-level inventory balances, derived availability, aggregate inventory queries, relational safeguards, dependency-safe deletion, and automated coverage are implemented.

---

## Phase 04 — Stock Movement Engine

### Objectives

Create the central inventory transaction engine.

### Deliverables

* StockMovement entity
* Stock movement types
* Stock-in operation
* Stock-out operation
* Inventory balance updates
* Movement history
* Reference information
* Transaction handling
* Protection against insufficient stock
* Unit tests
* Integration tests

### Definition of Done

Every inventory quantity change can be reliably executed and audited.

### Status

Completed. Stock In and Stock Out, immutable movement history, available-stock protection, atomic serializable balance-and-ledger persistence, optional external references, database safeguards, paged history, delete integrity, and automated unit/integration coverage are implemented.

---

## Phase 05 — Stock Adjustments

### Objectives

Support controlled manual inventory corrections.

### Deliverables

* Inventory adjustment model
* Increase adjustment
* Decrease adjustment
* Adjustment reasons
* Validation
* Stock movement generation
* Audit information
* Adjustment history
* Tests

### Definition of Done

Authorized inventory corrections are safely recorded and auditable.

### Status

Completed. Controlled Increase and Decrease corrections, immutable adjustment audit records, caller-supplied audit identity metadata, available-stock protection, linked stock movements, atomic serializable persistence, paged history, relational safeguards, and automated unit/integration coverage are implemented. Phase 09 will replace caller-supplied identity with authenticated identity integration.

---

## Phase 06 — Warehouse Transfers

### Objectives

Support safe movement of inventory between warehouses.

### Deliverables

* WarehouseTransfer entity
* Transfer items
* Transfer lifecycle
* Transfer creation
* Transfer completion
* Source stock validation
* Transfer Out movement
* Transfer In movement
* Transaction consistency
* Transfer history
* Tests

### Definition of Done

Inventory can be transferred without creating, losing, or duplicating stock.

### Status

Completed. Pending-to-Completed multi-product transfers, exact source/destination position validation, creation and completion availability checks, Serializable atomic completion, paired TransferOut/TransferIn ledger entries, stock conservation, deterministic transfer history, master-data deletion protection, relational safeguards, and automated unit/integration coverage are implemented. Pending transfers do not reserve inventory; reservations remain Phase 07 scope.

---

## Phase 07 — Inventory Reservations

### Objectives

Support temporary allocation of inventory.

### Deliverables

* InventoryReservation entity
* Reservation creation
* Reservation release
* Reservation fulfillment
* Available stock validation
* Reserved quantity updates
* Reservation status lifecycle
* External reference support
* Tests

### Definition of Done

Inventory can be safely reserved without corrupting physical stock quantities.

### Status

Completed. Exact-position Active-to-Released/Fulfilled reservations, available-stock validation, immutable quantities and external references, Serializable atomic balance updates, StockOut-backed fulfillment, deterministic history, relational lifecycle safeguards, deletion integrity, and automated unit/integration coverage are implemented. Pending transfers remain non-reserving and revalidate availability at completion.

---

## Phase 08 — Low Stock Monitoring & Background Jobs

### Objectives

Introduce proactive inventory monitoring.

### Deliverables

* Reorder / low-stock threshold
* Low-stock query
* Background monitoring service
* Low-stock alerts
* Configurable execution interval
* Logging
* Tests

### Definition of Done

The system can automatically identify products that require inventory attention.

### Status

Completed. Exact product/warehouse/location thresholds, `AvailableQuantity`-based operational low-stock evaluation, persistent alert reconciliation, a configurable hosted monitoring worker, structured scan logging, relational safeguards, and automated domain/integration coverage are implemented.

---

## Phase 09 — Authentication & Authorization

**Status: Completed.** Custom users, secure framework password hashing, JWT access tokens, rotating hashed refresh tokens, four roles, permission policies, protected endpoints, Admin user management, authenticated adjustment audit identity, and automated coverage are implemented.

### Objectives

Secure inventory operations.

### Deliverables

* User authentication
* JWT access tokens
* Refresh tokens
* Roles
* Permissions / policies
* Protected endpoints
* Administrative permissions
* Warehouse operation permissions
* Tests

### Initial Roles

* Admin
* InventoryManager
* WarehouseOperator
* Viewer

### Definition of Done

Sensitive inventory operations are available only to authorized users.

---

## Phase 10 — Inventory Queries & Dapper Reporting

**Status: Completed.** A dedicated Dapper reporting layer now supplies inventory summary, cross-product movements, warehouse inventory, low-stock reporting, and signed product stock history with UTC date filtering, database-side pagination/sorting, focused reporting indexes, and relational automated coverage.

### Objectives

Demonstrate optimized reporting and query-heavy data access.

### Deliverables

* Dapper integration
* Inventory summary report
* Stock movement report
* Warehouse inventory report
* Low-stock report
* Product stock history
* Date filtering
* Pagination
* Sorting
* Query optimization
* Tests

### Definition of Done

The API provides efficient read-heavy reporting endpoints without forcing all queries through EF Core.

---

## Phase 11 — Testing & Reliability

### Objectives

Strengthen reliability and production readiness.

### Deliverables

* Expanded unit test coverage
* Expanded integration test coverage
* Transaction tests
* Concurrency-sensitive inventory tests
* Validation tests
* Authorization tests
* Error handling tests
* Logging review
* Edge-case coverage

### Definition of Done

Critical inventory workflows have automated regression protection.

---

## Phase 12 — Portfolio Polish & Documentation

### Objectives

Prepare the repository for clients and portfolio presentation.

### Deliverables

* Professional README
* Architecture overview
* Features overview
* API examples
* Setup instructions
* Database setup instructions
* Authentication examples
* Business rule documentation
* Screenshots where useful
* OpenAPI and Scalar documentation review
* Repository cleanup
* Final build
* Final test run

### Definition of Done

The repository clearly demonstrates professional backend engineering skills to potential clients.

---

# 8. Out of Scope

The following features are intentionally excluded from the initial project scope:

* Complete ERP functionality
* Accounting
* Invoicing
* Full purchasing system
* Full supplier management
* Full sales order management
* Shipping management
* Demand forecasting
* Machine learning
* Complex microservices architecture
* Message brokers unless later justified
* Kubernetes
* Production cloud infrastructure
* Full frontend application

These features may be represented by external references or integrations but will not be implemented as complete modules.

---

# 9. Development Workflow

Each phase will use its own Git branch.

Example:

```text
phase/01-foundation
phase/02-products-warehouses
phase/03-locations-inventory-balances
```

For every phase:

1. Create or switch to the phase branch.
2. Implement only the agreed phase scope.
3. Build the entire solution.
4. Run all automated tests.
5. Review Git changes.
6. Commit using a clear commit message.
7. Push the branch.
8. Merge into `master`.
9. Delete the completed phase branch.
10. Confirm that `master` is clean and up to date.

---

# 10. Quality Rules

Throughout development:

* Avoid unnecessary overengineering.
* Keep controllers thin.
* Keep domain rules outside controllers.
* Use async APIs for I/O operations.
* Use cancellation tokens where appropriate.
* Validate external input.
* Return consistent API errors.
* Avoid exposing persistence entities directly when inappropriate.
* Protect inventory operations with transactions where required.
* Maintain clear separation of concerns.
* Add automated tests for important business rules.
* Keep the project build warning-free where practical.
* Keep commits focused and understandable.

---

# 11. Project Progress

| Phase                                         | Status      |
| --------------------------------------------- | ----------- |
| 01 — Project Foundation                       | Completed   |
| 02 — Products & Warehouses                    | Completed   |
| 03 — Warehouse Locations & Inventory Balances | Completed   |
| 04 — Stock Movement Engine                    | Completed   |
| 05 — Stock Adjustments                        | Completed   |
| 06 — Warehouse Transfers                      | Completed   |
| 07 — Inventory Reservations                   | Completed   |
| 08 — Low Stock Monitoring & Background Jobs   | Completed   |
| 09 — Authentication & Authorization           | Completed   |
| 10 — Inventory Queries & Dapper Reporting     | Completed   |
| 11 — Testing & Reliability                    | Not Started |
| 12 — Portfolio Polish & Documentation         | Not Started |

---

## Current Phase

**Phase 11 — Testing & Reliability (Not Started)**
