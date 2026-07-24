# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repository is

OrderHub is a company-internal order-management web app (ASP.NET Core MVC + EF Core + SQL Server) that exists as the practice project for a junior-level AI-agent coding course. The `training-repo/` directory (where this file lives) is the actual .NET solution; the course instructions live one level up in `../documents/` (`README.md`, `PROCESS.md`, `activities/activity-guideline.md`).

**The exercises rely on planted bugs and missing features. Do not proactively "fix" or refactor things unless the current task asks for it.** The guideline walks a learner through: (2) reproducing and fixing 3 specific bugs, (3) building a `/Products/LowStock` page, (4) refactoring `OrderService.CreateOrderAsync`. If you spot a bug outside the scope of the task at hand, mention it but leave it — it is likely exercise material.

Code comments, UI strings, commit messages, and seed data are in Traditional Chinese; match that when editing.

## Commands

Run from `training-repo/` (the directory with `OrderHub.sln`):

```powershell
dotnet run --project src/OrderHub.Web     # run the site (auto-migrates + seeds on first start)
dotnet build                              # build the solution
dotnet test                               # run all tests (EF Core InMemory — no SQL Server needed)

# run a single test class / method
dotnet test --filter "FullyQualifiedName~OrderServicePricingTests"
dotnet test --filter "FullyQualifiedName~OrderServicePricingTests.MethodName"

# reset the dev database back to seed data
dotnet ef database drop -f -p src/OrderHub.Infrastructure -s src/OrderHub.Web
dotnet run --project src/OrderHub.Web     # re-migrates + re-seeds

# add a migration (after changing the model / DbContext)
dotnet ef migrations add <Name> -p src/OrderHub.Infrastructure -s src/OrderHub.Web
```

Running the site requires a local SQL Server; connection string is in `src/OrderHub.Web/appsettings.Development.json` (`Server=localhost`, Windows auth). Tests do not — they use `Microsoft.EntityFrameworkCore.InMemory`.

## Architecture

Three projects, strict clean-architecture dependency direction (`Web` and `Infrastructure` → `Core`; `Core` depends on nothing):

- **OrderHub.Core** — domain models (`Domain/`), business logic in `Services/` (discount, stock, status transitions), repository *interfaces* (`Interfaces/`), and shared result types (`Common/ServiceResult<T>`, `Common/PagedResult<T>`). This is where behavior lives.
- **OrderHub.Infrastructure** — EF Core `OrderHubDbContext`, repository *implementations*, `Migrations/`, and `DbSeeder`.
- **OrderHub.Web** — MVC controllers, `ViewModels/`, Razor `Views/`. Wiring and display only.

Request flow: Controller → Core service (interface-injected) → repository (interface-injected) → `DbContext`. DI is registered in `Program.cs`; `Program.cs` also calls `db.Database.Migrate()` and `DbSeeder.SeedAsync` at startup.

Domain shape: `Customer` (with `CustomerTier`) 1—* `Order` 1—* `OrderItem` *—1 `Product`. `OrderItem.UnitPriceSnapshot` captures price at order time. `OrderStatus` is Pending → Confirmed → Shipped, or Cancelled.

## Conventions (follow when adding features)

- Keep controllers thin; put business logic in a Core service behind an interface. Never touch `DbContext` from a controller or service — go through a repository.
- Views bind to a ViewModel (mapping is hand-written in the controller), never to a domain model directly.
- Return `ServiceResult<T>` / `ServiceResult<T>.Fail(...)` from services for operations that can fail with user-facing messages; surface those via `ModelState`.
- Server-side validation uses DataAnnotations + `ModelState`. Operation outcomes use `TempData["Success"]` / `TempData["Error"]` (shared alert block is in `_Layout.cshtml`).
- Display formatting (status/tier labels, money, badge classes) goes in `Web/Helpers/DisplayHelper.cs`.
- Discount rule (single source of truth in `OrderService`): Standard 0%, Silver 5%, Gold 10%, applied once on the order total.
- Tests: use the helpers in `tests/OrderHub.Tests/TestSetup.cs` (`CreateContext`, `CreateOrderService`, `AddCustomer`, `AddProduct`) — each test gets a fresh uniquely-named InMemory DB.

C# style (`.editorconfig`): file-scoped namespaces, `var` when the type is apparent, 4-space indent, `System` usings sorted first.
