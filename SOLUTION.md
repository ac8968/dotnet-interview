# TODO API Refactoring — Solution

This document satisfies the exercise deliverable: **problems found**, **architecture and reasoning**, **how to run**, **API usage**, **future work**, and pointers to **tests**.

## Repository layout

The exercise describes a folder named `interview-problem/`; this repo uses the same roles under the solution root (next to `TodoApi.sln`):

| Exercise (conceptual) | This repository |
|------------------------|-----------------|
| `TodoApi/` (controllers, models, services, `Program.cs`) | `TodoApi/` — plus `Data/` (schema bootstrap), `Options/` (configuration binding) |
| `TodoApi.Tests/` | `TodoApi.Tests/` — tests grouped under `Integration/` (HTTP + in-memory config) and `Services/` (persistence behavior with a temp database file) |

## Problems Identified

1. **SQL injection** — `TodoService` built SQL with string interpolation (`$"… '{todo.Title}' …"`), so malicious input could alter queries.
2. **Tight coupling and no DI** — The controller used `new TodoService()` on every request, which hid dependencies, blocked testing with substitutes, and prevented sharing configuration.
3. **Hard-coded connection strings** — The same literal appeared in `Program.cs` and `TodoService`, with no way to vary by environment.
4. **Non-RESTful API** — All operations used `POST` with paths like `createTodo` / `getTodo`; retrieval should use `GET`, updates `PUT`, deletes `DELETE`, with resources under a consistent route.
5. **Fragile data access** — `Description` was read with `GetString(2)` even when the column could be `NULL`, which can throw at runtime.
6. **Over-broad error handling** — Catching `Exception` and returning `BadRequest(ex.Message)` masked real failures and mixed client errors with server problems.
7. **Weak tests** — Tests asserted `Assert.True(true)`, depended on shared `todos.db` state, and did not cover negative cases or HTTP behavior.
8. **No input validation** — DTOs had no constraints; invalid payloads were not rejected in a consistent way.

## Architectural Decisions

- **Layering** — Kept a thin **controller** (HTTP only), **service** (`ITodoService` / `TodoService`) for use-cases, and **data** helpers (`DatabaseInitializer`) for schema creation. No full ORM: SQLite + parameterized commands keeps the exercise small and avoids migration tooling.
- **Reasoning (layering)** — A dedicated service boundary keeps SQL and mapping out of the controller, makes the API testable, and leaves room to swap SQLite for another store without changing HTTP contracts.

- **Configuration** — `TodoDatabaseOptions` bound from `appsettings.json` under `TodoDatabase:ConnectionString`, so environments can override without recompiling.
- **Reasoning (configuration)** — Hard-coded connection strings break deployments and tests; options binding is the standard ASP.NET Core pattern.

- **Dependency injection** — `ITodoService` is registered as **scoped** per request; the controller receives it via constructor injection.
- **Reasoning (DI)** — `new TodoService()` in the controller hid dependencies and forced a shared global database path; constructor injection is required for test doubles and per-environment setup.

- **REST** — `TodosController` exposes `POST/GET/PUT/DELETE` under `api/todos` with `id` in the route where appropriate. Successful delete returns **204 NoContent**; create returns **201 Created** with `CreatedAtAction` for a stable location pattern.
- **Reasoning (REST)** — Using GET for reads and DELETE for deletes matches HTTP semantics, improves cacheability, and aligns with client tooling (OpenAPI, browsers, proxies).

- **Async I/O** — Data access uses `async`/`await` end-to-end to avoid blocking thread-pool threads under load.
- **Validation** — `CreateTodoRequest` / `UpdateTodoRequest` use data annotations (`Required`, `MinLength`, `MaxLength`). `[ApiController]` returns **400** for invalid models without manual try/catch.
- **Reasoning (validation)** — Central validation avoids duplicating checks in every action and returns consistent problem details for bad input.

- **Testing** — **`TodoApi.Tests/Services`** exercises `TodoService` against a **private temporary SQLite file** per test class instance (create/read/update/delete, null description, not-found paths, ordering). **`TodoApi.Tests/Integration`** uses **`WebApplicationFactory<Program>`** with configuration overriding the DB path so runs do not touch `todos.db`. Integration tests cover **success paths** (201, 200, 204) and **negative paths** (404, 400 validation on create and update). A **non-parallel xUnit collection** avoids flaky shared state on one factory.
- **Target framework** — Projects target **.NET 9** so `dotnet test` runs on machines that have newer runtimes only; to match the original **.NET 8** template, set `TargetFramework` to `net8.0` in both `.csproj` files and align `Microsoft.AspNetCore.Mvc.Testing` to 8.x.

## How to Run

From the repository root (folder containing `TodoApi.sln`):

```bash
dotnet build
dotnet run --project TodoApi
```

With the default profile, Swagger UI is available in Development (see `launchSettings.json`, typically `https://localhost:7186/swagger` or `http://localhost:5164/swagger`).

Run tests:

```bash
dotnet test
```

## API Documentation

Base URL: `/api/todos` (ASP.NET Core trims the `Todos` controller name to `todos`).

| Method | Path | Body | Success | Notes |
|--------|------|------|---------|--------|
| `POST` | `/api/todos` | `CreateTodoRequest` JSON | **201 Created** | Response body is the created `Todo`; `Location` header points to the new resource. |
| `GET` | `/api/todos` | — | **200 OK** | Array of `Todo`. |
| `GET` | `/api/todos/{id}` | — | **200 OK** / **404** | Single `Todo` or not found. |
| `PUT` | `/api/todos/{id}` | `UpdateTodoRequest` JSON | **200 OK** / **404** | Full update of title, description, completed flag. |
| `DELETE` | `/api/todos/{id}` | — | **204 NoContent** / **404** | Idempotent in the sense that missing ids return 404. |

**CreateTodoRequest** (JSON):

- `title` (string, required, 1–500 characters)
- `description` (string, optional, max 4000)
- `isCompleted` (boolean)

**UpdateTodoRequest**: same shape as create (all fields apply to the existing todo).

**Todo** (response):

- `id`, `title`, `description`, `isCompleted`, `createdAt` (UTC, ISO-8601)

## Future Improvements

- **Migrations** — Replace `EnsureCreated` with a versioned migration tool (EF Core or FluentMigrator) for production schema changes.
- **Repository abstraction** — Split `TodoService` into `ITodoRepository` + domain/service layer if rules grow beyond CRUD.
- **Observability** — Structured logging, correlation IDs, and health checks (`/health`) including DB connectivity.
- **AuthN/Z** — Protect mutating endpoints if the API is multi-tenant or user-specific.
- **Pagination and filtering** — `GET /api/todos` could support `?completed=false&page=2`.
- **Concurrency** — ETag or row versioning on `PUT` to detect stale updates.
