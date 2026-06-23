# Architecture

## Tech Stack

| Layer | Technology | Version |
|---|---|---|
| **Runtime** | .NET | 10 (SDK 10.0.102) |
| **Web Framework** | ASP.NET Core Minimal APIs | 10.0.9 |
| **ORM** | Dapper | 2.1.79 |
| **Database** | SQLite (via Microsoft.Data.Sqlite) | 10.0.9 |
| **Authentication** | JWT Bearer (HS256) | 10.0.9 |
| **Password Hashing** | BCrypt.Net-Next | 4.2.0 |
| **Validation** | FluentValidation.AspNetCore | 11.3.1 |
| **API Docs** | Scalar.AspNetCore | 2.16.4 |
| **Testing** | xUnit + FluentAssertions | 2.9.2 / 6.12.2 |

### Planned Frontend

- React 19 + Vite + TanStack Query v5
- React Hook Form + Zod
- shadcn/ui + Tailwind v4 + lucide-react

---

## Project Structure — Vertical Slice Architecture

```
backend/ProcureMax.Api/
├── Program.cs                      ← Composition root, middleware pipeline, DI registration
├── GlobalUsings.cs                 ← Project-wide implicit usings
├── Core/                           ← Shared infrastructure (NOT business logic)
│   ├── Auth/                       ← JWT token service, refresh token store, password hasher
│   ├── Authorization/              ← Permission handler, policy provider, constants
│   ├── Common/                     ← Money value object, Result<T>, exceptions, enums
│   ├── Middleware/                 ← Global exception handler, request logging
│   ├── Migrations/                 ← Numbered SQL migration files (embedded resources)
│   ├── AuditLog.cs                 ← Append-only audit trail writer
│   ├── CurrentUser.cs              ← HttpContext-based current user resolver
│   ├── Database.cs                 ← IDbConnectionFactory + DatabaseInitializer
│   ├── ProcurementOptions.cs       ← Configuration POCO
│   └── Seeder.cs                   ← Seeds permissions, roles, admin user, reference data
├── Features/                       ← Each subfolder = one vertical slice
│   ├── Auth/                       ← Login, refresh, logout, "me"
│   ├── Users/                      ← User CRUD + role assignment
│   ├── Roles/                      ← Role CRUD + permission assignment
│   ├── CostCenters/                ← Cost center CRUD
│   ├── GlAccounts/                 ← GL account CRUD
│   ├── Units/                      ← Unit of measure CRUD
│   ├── Suppliers/                  ← Supplier CRUD
│   ├── Items/                      ← Item CRUD (FK validation)
│   └── Procurement/                ← Domain aggregate (PurchaseRequisition)
└── procuremax.db                   ← SQLite database (gitignored)
```

### Per-Slice File Pattern

Each slice (Phase 2+) follows a consistent 4-file template:

| File | Purpose |
|---|---|
| `Dtos.cs` | Request/response DTOs + FluentValidation `AbstractValidator` classes |
| `*Repository.cs` | Internal `Row` record class + `I*Repository` interface + Dapper implementation |
| `*Service.cs` | `I*Service` interface + implementation (business logic, audit, FK validation) |
| `*Endpoints.cs` | `static Map*Endpoints(this IEndpointRouteBuilder)` extension method |

**Phase 1 services** (`AuthService`, `UserService`, `RoleService`) skip the Repository layer and hold `IDbConnectionFactory` directly.

---

## Dependency Flow

```
Endpoints → Service → Repository → IDbConnectionFactory → SQLite
```

- **Endpoints**: define routes, permission gates, model binding, validation, and HTTP response mapping
- **Services**: contain business logic, audit logging, FK validation, and orchestration
- **Repositories**: purely data access with Dapper SQL — return internal `Row` types mapped to DTOs
- **No MediatR**, **No FastEndpoints**, **No separate Application/Domain projects** — single .csproj

---

## Middleware Pipeline (order matters)

```
RequestLoggingMiddleware
    → ExceptionHandler (GlobalExceptionHandler)
    → CORS
    → OpenAPI / Scalar (Development only)
    → Authentication (JWT Bearer)
    → Authorization (PermissionPolicyProvider + PermissionHandler)
    → Endpoint
```

---

## Conventions

| Convention | Detail |
|---|---|
| **Money** | Stored as `long` minor units (cents). `Money` readonly record struct. Never `decimal`/`float`. |
| **Optimistic Concurrency** | Every writeable table has `row_version TEXT` (a `N`-format GUID). UPDATE/DELETE includes `WHERE row_version = @OldVersion` and rotates the version. Mismatch → `ConflictException` → HTTP 409. |
| **Soft Delete** | Every table has `is_deleted INTEGER`. Queries filter `WHERE is_deleted = 0`. |
| **Audit Trail** | All mutations write to `audit_logs` in their own short transaction. |
| **ID Format** | `entity:<identifier>` — see [Domain Model](./domain-model.md#id-conventions). |
| **DTO Materialization** | All DTOs use `record class` with `init` properties and parameterless default ctor. Never positional records with Dapper. |
| **SQL Aliasing** | Dapper doesn't map snake_case → PascalCase. Every SELECT uses explicit `AS` aliases. Boolean columns are also `CAST(is_active AS INTEGER) AS IsActive`. |
