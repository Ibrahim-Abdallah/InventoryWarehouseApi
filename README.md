# InventoryWarehouseApi

A production-style ASP.NET Core Web API portfolio project for inventory and warehouse management. The project is under active development. Phase 01 — Project Foundation is complete, and Phase 02 — Products & Warehouses has not started.

## Technology stack

- .NET 10 and ASP.NET Core controllers
- ASP.NET Core OpenAPI and Scalar API Reference
- ASP.NET Core health checks and Problem Details
- Serilog structured console and request logging
- xUnit and `WebApplicationFactory` integration testing

## Architecture

The solution follows a layered architecture with dependencies pointing inward:

```text
Api -> Application, Infrastructure
Infrastructure -> Application, Domain
Application -> Domain
Domain -> no project dependencies
```

Source projects live under `src/`; unit and integration test projects live under `tests/`.

## Run locally

Prerequisite: .NET SDK 10.0.400 or a compatible .NET 10 SDK.

```powershell
dotnet restore
dotnet run --project src/InventoryWarehouseApi.Api
```

In Development, Scalar API Reference is available at `/scalar/v1`, and the OpenAPI document is available at `/openapi/v1.json`. The basic application health check is available at `GET /health` and returns HTTP 200 when the API is healthy.

## Build and test

```powershell
dotnet build
dotnet test
```

See [PROJECT_PLAN.md](PROJECT_PLAN.md) for the phased roadmap. Only Phase 01 is implemented at this stage.
