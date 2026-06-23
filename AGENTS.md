# AGENTS.md — ProcureMax

> Source of truth for any AI coding agent (Copilot, Cursor, Claude Code, Aider, Continue, etc.)
> working on this repo. Vanilla JSON-schema files these conventions reference live in
> `backend/ProcureMax.Api/`. Mirrored from the agent's repo-scope memory on 2026-06-22.

---

## 1. Stack

- **Backend**: .NET 10 (SDK 10.0.102) Minimal APIs + Dapper 2.1.79 + Microsoft.Data.Sqlite 10.0.9
  - Also: FluentValidation.AspNetCore 11.3.1, BCrypt.Net-Next 4.2.0, Microsoft.AspNetCore.Authentication.JwtBearer 10.0.9
- **DB**: SQLite at `backend/ProcureMax.Api/procuremax.db`
  - Dev: relative path · Prod: `/data/procuremax.db` (DO persistent volume)
- **Auth**: JWT HS256, 15-min access tokens + SHA-256 hashed rotating refresh tokens
- **RBAC**: user → role → permission; permissions emitted as JWT `perm` claims; `HasPermission(...)`
  endpoint extension; `PermissionPolicyProvider` resolves dynamic policies by permission key
- **Frontend** (planned, not yet started): React 19 + Vite + TanStack Query v5 + RHF + Zod + shadcn/ui + Tailwind v4
- **Deploy planned**: DigitalOcean App Platform (backend container) + Cloudflare Worker (edge proxy, rate limit, CORS)

---

## 2. Architectural Pattern — Vertical Slice Architecture (VSA)

- **Single project**: `ProcureMax.Api`. No MediatR, no FastEndpoints, no separate Application/Domain projects.
- **Layout**:
  ```
  backend/ProcureMax.Api/
    Program.cs                ← composes slices, registers DI
    Core/                     ← shared infra (NOT domain logic)
      Auth/                   ← JwtTokenService, RefreshTokenStore, AuthOptions, PasswordHasher
      Authorization/          ← PermissionHandler, PermissionPolicyProvider, HasPermission
      Common/                 ← Result<T>, Paged<T>, PageQuery, Money, IdResponse, exceptions
      Middleware/             ← GlobalExceptionHandler (RFC 9457 Problem Details)
      Migrations/             ← *.sql numbered files, EmbeddedResource, alphabetically applied
      DatabaseInitializer.cs  ← migrator + __migrations tracking table
      Seeder.cs               ← admin role/user + permission catalog + reference data
    Features/                 ← each folder = one vertical slice
      <Slice>/
        Dtos.cs               ← record-class DTOs with init props + AbstractValidators
        <Slice>Repository.cs  ← internal Row classes, ISliceRepository + impl
        <Slice>Service.cs     ← ISliceService + impl, audit logging, FK validation
        <Slice>Endpoints.cs   ← static MapSliceEndpoints extension
  ```
- **Per-slice dependency chain**: Endpoints → Service → Repository → IDbConnectionFactory → SQLite.
  Phase 1 services (`UserService`, `RoleService`, `AuthService`) skip the Repository layer and
  hold `IDbConnectionFactory` directly. Phase 2+ slices use a Repository for testability.
- **Money**: stored as `long` minor units (`Money` readonly record struct). Never decimal/float.
- **Optimistic concurrency**: every writeable table has `row_version TEXT` (a guid `N` string);
  each UPDATE/DELETE includes `WHERE row_version = @OldVersion` and rotates it. Mismatch throws
  `ConflictException` → HTTP 409.
- **Soft delete**: every table has `is_deleted INTEGER`; queries filter `WHERE is_deleted = 0`.
- **Record-class DTO materialization**: **all** DTOs use `public record class X { init; }`
  (parameterless default ctor + init setters). **Never** positional records (`record X(string A, int B)`)
  for anything Dapper materializes — see §6.

---

## 3. ID Conventions

Server generates GUIDs on create; client-supplied IDs in request bodies are ignored.

| Entity | Format | Example |
|---|---|---|
| Users | `user:<guid N>` (seeded admin: `user:admin`) | `user:a1b2c3...` |
| Roles | `role:<key>` for system (`role:admin`, `role:requestor`, `role:approver`, `role:buyer`, `role:apclerk`) or `role:<guid N>` for custom | |
| Permissions | `perm:<area>:<action>` | `perm:dashboard:view`, `perm:pr:approve.all` |
| Cost Centers | `cc:<UPPERCODE>` (code-based) | `cc:CC-1000` |
| GL Accounts | `gl:<CODE>` (code-based) | `gl:5000-OFFICE` |
| Units | `unit:<CODE>` (code-based, uppercase) | `unit:EA`, `unit:KG` |
| Suppliers | `supplier:<guid N>` (GUID-based, not code-based) | `supplier:abc...` |
| Items | `item:<guid N>` (GUID-based; sku is still UNIQUE) | `item:def...` |

**Note on permission keys:** the DB row `id` is `perm:<area>:<action>`; the `Permissions` static
class constant is the claim value without the `perm:` prefix and uses `.` separator:

```csharp
public static class Permissions
{
    public const string CostCenterManage = "costcenter.manage";
    public const string SupplierView     = "supplier.view";
    public const string SupplierManage   = "supplier.manage";
    // ...
}
```

In SQL the two facts look like: row `id='perm:costcenter:manage'`, claim value `'costcenter.manage'`.

---

## 4. Permission Catalog (24 permissions, seeded)

| Permission key | Granted to roles |
|---|---|
| `dashboard.view` | admin, requestor, approver, buyer, apclerk |
| `users.manage`, `roles.manage` | admin |
| `costcenter.manage`, `glaccount.manage`, `unit.manage` | admin |
| `item.view`, `supplier.view` | admin, requestor, approver, buyer, apclerk |
| `item.manage`, `supplier.manage` | admin |
| `pr.create`, `pr.view` | admin, requestor, approver, buyer, apclerk |
| `pr.approve.own_cost_center`, `pr.approve.all` | admin, approver |
| `po.create`, `po.view`, `po.issue`, `po.approve` | admin, buyer, approver |
| `gr.create` | admin, buyer, apclerk |
| `invoice.manage`, `invoice.approve` | admin, apclerk |
| `approval.rules.manage` | admin |

Two-tier read/write pattern (master data slices): GET endpoints gated to `*.view`,
POST/PUT/DELETE gated to `*.manage`. The catalog picker is read-only for downstream PR creation.

---

## 5. Build / Run / Test

```bash
# Build
dotnet build backend/ProcureMax.Api/ProcureMax.Api.csproj

# Run (dev — JWT signing secret fallback kicks in only in Development)
ASPNETCORE_ENVIRONMENT=Development \
  dotnet run --project backend/ProcureMax.Api/ProcureMax.Api.csproj \
             --urls "http://localhost:5000"

# Tear down DB (next run will replay all migrations + seeds)
rm backend/ProcureMax.Api/procuremax.db

# Tests
dotnet test backend/ProcureMax.Api.Tests/ProcureMax.Api.Tests.csproj
```

**Admin dev seed**: `admin@procuremax.local` / `Admin#123` (Admin role, all 24 perms).

**sqlite3 CLI is NOT installed** on this machine. Inspect the DB by:
- Hitting the API with curl + `python3 -m json.tool`, or
- A throwaway `dotnet run` script in `/tmp` using `Microsoft.Data.Sqlite` directly.

---

## 6. Dapper + SQLite Lessons (critical — each verified by a real bug)

### 6.1 Dapper is CASE-SENSITIVE on parameter binding
Anonymous object property names MUST match SQL `@Name` exactly. Lowercase `new { id, name }`
against `(@Id, @Name)` fails with *"Must add values for the following parameters: @Id, @Name"*.

### 6.2 Dapper does NOT do snake_case → PascalCase column mapping
A bare `SELECT *` against `email`, `full_name`, `is_active` will NOT bind to `Email`, `FullName`,
`IsActive` — they stay null/false silently. Use explicit aliases.

Pattern: a per-repo `public const string SelectColumns = "id AS Id, email AS Email, ...";` reused
on every read.

**Verified bug**: admin login returned 401 because `IsActive` defaulted to `false` under `SELECT *`.

### 6.3 SQLite INTEGER returns as Int64 (even for bool / small-int)
Dapper positional-record materialization requires **exact** ctor signature match.
`SELECT COUNT(*) AS PermissionCount` returns Int64. A positional record
`public record RoleSummary(..., int PermissionCount)` fails with:

> `InvalidOperationException: ... matching signature (String Id, String Name, String Description, Int64 IsSystem, Int64 PermissionCount) is required`

**Fix that works**: declare record **classes** with `init` properties and a parameterless
default ctor. Dapper's default mapper then applies Int64 → bool/int conversion via setters.

```csharp
// BAD — positional record, fails on Int64 columns
public record RoleSummary(string Id, string Name, bool IsSystem, int PermissionCount);

// GOOD — record class with init
public record class RoleSummary
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public bool IsSystem { get; init; }
    public int PermissionCount { get; init; }
}
```

CAST(`is_active AS INTEGER) is **defense-in-depth** (still add it) but does NOT fix positional
records — Microsoft.Data.Sqlite exposes Int64 in the reader either way.

### 6.4 Don't try to map list/array columns through Dapper
Dapper has no concept of splitting a CSV cell into `List<string>`. If a DTO has
`IReadOnlyList<string> Roles`, do NOT `SELECT '' AS Roles`.

Pattern: SELECT the scalar fields into an internal Row, then a second query with `IN @Ids` to
build a dictionary, then project:

```csharp
var rows = await conn.QueryAsync<UserSummaryRow>("SELECT id AS Id, ... FROM users WHERE is_deleted = 0");
var ids = rows.Select(r => r.Id).ToArray();
var roles = await conn.QueryAsync<(string UserId, string Role)>(
    "SELECT ur.user_id AS UserId, r.name AS Role FROM user_roles ur JOIN roles r ON r.id = ur.role_id WHERE ur.user_id IN @Ids",
    new { Ids = ids });
var items = rows.Select(r => new UserSummary { /* ... */ })
                .Select(u => u with { Roles = roles.Where(x => x.UserId == u.Id).Select(x => x.Role).ToList() })
                .ToList();
```

### 6.5 `[AsParameters]` query-string binding gotcha
`[AsParameters] PageQuery q` treats every property of `PageQuery` as a query-string parameter.
If a property is non-nullable `int Page` and the client omits `?Page=`, ASP.NET Core returns
**500** *"Required parameter int Page was not provided"* — NOT 400.

Fix: nullable backing field + derived getter.

```csharp
public class PageQuery
{
    public int? Page     { get; set; }
    public int? PageSize { get; set; }
    public string? Search { get; set; }
    public int PageValue     => Page     ?? 1;
    public int PageSizeValue => PageSize ?? 20;
    public int Offset        => (PageValue - 1) * PageSizeValue;
}
```

For body-bound DTOs do NOT use `[AsParameters]` — plain `(string id, BodyDto req, ...)` lets
ASP.NET Core bind the JSON body to the complex type automatically.

For deleting with optimistic concurrency (no body), use a small class for the rowVersion query
param:

```csharp
public sealed class DeleteRequest { public string RowVersion { get; set; } = ""; }
// DELETE handler signature uses [AsParameters] DeleteRequest req
```

### 6.6 Foreign key constraints are ON in Microsoft.Data.Sqlite
Inserting `role_permissions` with a non-existent `permission_id` raises
`SQLite Error 19: 'FOREIGN KEY constraint failed'`.

Permission IDs in the DB are formatted `perm:<area>:<action>`. Don't confuse the **row id**
(`perm:dashboard:view`) with the claim **value** (`dashboard.view`).

For Phase 2+ slices that reference lookup tables by FK (`items.unit_id`, `items.gl_account_id`,
`items.default_supplier_id`), the service layer pre-validates FK existence and throws
`NotFoundException` before INSERT — SQLite's FK layer is the last line of defense, not the first.

---

## 7. Endpoint Conventions

- Route prefix: `/api/<slice-plural>` (e.g. `/api/cost-centers`, `/api/suppliers`, `/api/items`)
- Standard verbs per slice:
  - `GET /` (paged, `[AsParameters] PageQuery`) → `Paged<Summary>`
  - `GET /{id}` → `<Detail>`
  - `POST /` (validated body) → `Created<IdResponse>` (Location header `/api/<slice>/{id}`)
  - `PUT /{id}` (validated body, includes `RowVersion`) → `NoContent`
  - `DELETE /{id}?rowVersion=...` → `NoContent`
- Permission gating via `.HasPermission(Permissions.XManage)` per route (not per group).
- All mutations write to `audit_logs` in their own short transaction via `AuditLog.WriteAsync(conn, tx, actorId, action, entity, id, before?, after?)`.
- Errors: `NotFoundException` → 404, `ConflictException` → 409 (code uniqueness, stale row_version),
  `ValidationException` → 400. The `GlobalExceptionHandler` renders RFC 9457 Problem Details with
  a `traceId` extension.

---

## 8. VSA Slice Template (the 4-file pattern)

When adding a new master-data slice, copy this template (`backend/ProcureMax.Api/Features/<Slice>/`). Adjust columns, code regex, ID prefix, and permission keys.

**Dtos.cs** — record-class DTOs with `init` props + AbstractValidators.
**<Slice>Repository.cs** — internal `Row` record class + `I<slice>Repository` + impl taking
`IDbConnectionFactory`. SQL uses `const string SelectColumns` with explicit PascalCase aliases and
`CAST(is_active AS INTEGER) AS IsActive` for bools.
**<Slice>Service.cs** — public `I<slice>Service` + impl taking the repo + `IDbConnectionFactory`
(for audit). Throws `NotFoundException` / `ConflictException`. Returns new `row_version` from
mutations.
**<Slice>Endpoints.cs** — static `Map<Slice>Endpoints(this IEndpointRouteBuilder)` returning
builder. `MapGroup("/api/<slice>").WithTags(...)`. GET gated to `*.view`, writes gated to `*.manage`.

**Don't forget** to wire it in `Program.cs`:

```csharp
// using
using ProcureMax.Features.<Slice>;

// DI
builder.Services.AddScoped<I<Slice>Service, <Slice>Service>();
builder.Services.AddScoped<I<Slice>Repository, <Slice>Repository>();

// routing (call anywhere after app build)
app.Map<Slice>Endpoints();
```

---

## 9. Frontend (not yet started — placeholder for when work begins)

Planned stack: React 19 + Vite + TanStack Query v5 + RHF + Zod + shadcn/ui + Tailwind v4 + lucide-react.
Folder mirror of backend slices: `frontend/src/features/<slice>/{routes,api,types,schemas,hooks,pages,components,nav}`.
`lib/http.ts` = fetch wrapper w/ Bearer injection + 401-refresh-once mutex.
Sidebar dynamically rendered from each slice's `nav.ts` filtered by user permissions.

When frontend work starts, update this section with the chosen conventions.

---

## 10. Points of Contact / Open Questions

- **Multi-tenancy**: deferred — add `tenant_id` if SaaS pivot happens.
- **SQLite on DO**: single replica only (no horizontal scale until v2 Postgres migration).
- **Edge JWT validation**: planned for Cloudflare Worker v1.1 (current Worker is reverse-proxy only).
- **Parallel approvers / committees**: deferred to v2.

---

*Last updated 2026-06-22 — Phase 2 (procurement master data) in progress.*
