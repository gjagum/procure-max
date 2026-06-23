# Development Guide

## Build & Run

```bash
# Build the API
dotnet build backend/ProcureMax.Api/ProcureMax.Api.csproj

# Run in development mode
ASPNETCORE_ENVIRONMENT=Development \
  dotnet run --project backend/ProcureMax.Api/ProcureMax.Api.csproj \
             --urls "http://localhost:5000"

# Run tests
dotnet test backend/ProcureMax.Api.Tests/ProcureMax.Api.Tests.csproj
```

---

## Adding a New Slice

Each vertical slice follows a consistent 4-file pattern. Here's the checklist:

### 1. Create the DTOs file (`Features/<Slice>/Dtos.cs`)

```csharp
using FluentValidation;

namespace ProcureMax.Features.<Slice>;

// --- Summary (for list views) ---
public record class <Slice>Summary
{
    public string Id { get; init; } = "";
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public bool IsActive { get; init; }
}

// --- Detail (for single-item view) ---
public record class <Slice>Detail
{
    public string Id { get; init; } = "";
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public bool IsActive { get; init; }
    public string CreatedAt { get; init; } = "";
    public string? UpdatedAt { get; init; }
    public string RowVersion { get; init; } = "";
}

// --- Request DTOs ---
public record class Create<Slice>Request
{
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
}

public record class Update<Slice>Request
{
    public string? Name { get; init; }
    public bool? IsActive { get; init; }
    public string RowVersion { get; init; } = "";
}

// --- Validators ---
public class Create<Slice>RequestValidator : AbstractValidator<Create<Slice>Request>
{
    public Create<Slice>RequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
```

> **Important**: All DTOs must use `record class` with `init` properties (not positional records). Dapper materializes `init` properties via setters but can't match positional record constructors with SQLite's Int64 columns. See [Dapper + SQLite Lessons](#dapper--sqlite-lessons).

### 2. Create the Repository (`Features/<Slice>/<Slice>Repository.cs`)

```csharp
using Dapper;

namespace ProcureMax.Features.<Slice>;

internal record class <Slice>Row { /* init properties matching DB columns */ }

internal interface I<Slice>Repository
{
    Task<IReadOnlyList<<Slice>Row>> ListAsync(string? search, int limit, int offset, CancellationToken ct);
    Task<int> CountAsync(string? search, CancellationToken ct);
    Task<<Slice>Row?> FindByIdAsync(string id, CancellationToken ct);
    Task<<Slice>Row?> FindByCodeAsync(string code, CancellationToken ct);
    Task InsertAsync(<Slice>Row row, CancellationToken ct);
    Task<bool> UpdateAsync(<Slice>Row row, CancellationToken ct);
    Task<bool> SoftDeleteAsync(string id, string oldVersion, string newVersion, CancellationToken ct);
}

internal class <Slice>Repository(IDbConnectionFactory db) : I<Slice>Repository
{
    public const string SelectColumns = "id AS Id, code AS Code, name AS Name, CAST(is_active AS INTEGER) AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt, row_version AS RowVersion";
    // ... Dapper implementations
}
```

**Patterns:**
- Use `public const string SelectColumns` for reusable column aliases
- Always `CAST(is_active AS INTEGER)` for boolean columns
- Use `AS` aliases for every column (Dapper doesn't do snake_case → PascalCase)
- Return `Row` types internally, let the service project to DTOs

### 3. Create the Service (`Features/<Slice>/<Slice>Service.cs`)

```csharp
namespace ProcureMax.Features.<Slice>;

public interface I<Slice>Service
{
    Task<Paged<<Slice>Summary>> ListAsync(PageQuery q, CancellationToken ct);
    Task<<Slice>Detail> GetAsync(string id, CancellationToken ct);
    Task<IdResponse> CreateAsync(Create<Slice>Request req, string actorId, CancellationToken ct);
    Task<string> UpdateAsync(string id, Update<Slice>Request req, string actorId, CancellationToken ct);
    Task DeleteAsync(string id, string rowVersion, string actorId, CancellationToken ct);
}

internal class <Slice>Service(I<Slice>Repository repo, IDbConnectionFactory db) : I<Slice>Service
{
    // Business logic, FK validation, audit logging
}
```

**Patterns:**
- Throw `NotFoundException` if entity doesn't exist
- Throw `ConflictException` on code uniqueness or stale `row_version`
- Write to `audit_logs` for all mutations
- Return the new `row_version` from updates
- FK validation: pre-check existence before INSERT and throw `NotFoundException`

### 4. Create the Endpoints (`Features/<Slice>/<Slice>Endpoints.cs`)

```csharp
namespace ProcureMax.Features.<Slice>;

public static class <Slice>Endpoints
{
    public static IEndpointRouteBuilder Map<Slice>Endpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/<slice-plural>")
            .WithTags("<Slice>");

        group.MapGet("/", async (I<Slice>Service svc, [AsParameters] PageQuery q, CancellationToken ct)
            => Results.Ok(await svc.ListAsync(q, ct)))
            .HasPermission(Permissions.<Slice>View);  // or Manage

        group.MapGet("/{id}", async (string id, I<Slice>Service svc, CancellationToken ct)
            => Results.Ok(await svc.GetAsync(id, ct)))
            .HasPermission(Permissions.<Slice>View);

        group.MapPost("/", async (Create<Slice>Request req, I<Slice>Service svc, ICurrentUser user, CancellationToken ct)
            => {
                var result = await svc.CreateAsync(req, user.Id, ct);
                return Results.Created($"/api/<slice-plural>/{result.Id}", result);
            })
            .HasPermission(Permissions.<Slice>Manage)
            .WithValidation<Create<Slice>Request>();

        group.MapPut("/{id}", async (string id, Update<Slice>Request req, I<Slice>Service svc, ICurrentUser user, CancellationToken ct)
            => Results.NoContent())
            .HasPermission(Permissions.<Slice>Manage)
            .WithValidation<Update<Slice>Request>();

        group.MapDelete("/{id}", async (string id, [AsParameters] DeleteRequest req, I<Slice>Service svc, ICurrentUser user, CancellationToken ct)
            => Results.NoContent())
            .HasPermission(Permissions.<Slice>Manage);

        return group;
    }
}
```

**Patterns:**
- Route: `/api/<kebab-case-plural>`
- Standard verbs: GET (paged), GET (single), POST (create), PUT (update), DELETE (soft delete)
- Permission gating via `.HasPermission()` per route
- `.WithValidation<T>()` for FluentValidation integration
- Use `[AsParameters]` on `PageQuery` — but use nullable backing fields to avoid 500 errors

### 5. Wire it in `Program.cs`

```csharp
// Add using
using ProcureMax.Features.<Slice>;

// Register DI
builder.Services.AddScoped<I<Slice>Service, <Slice>Service>();
builder.Services.AddScoped<I<Slice>Repository, <Slice>Repository>();

// Map endpoints (after app build)
app.Map<Slice>Endpoints();
```

### 6. Add SQL Migration

Create `Core/Migrations/003_<slice>.sql` with CREATE TABLE, indexes, and seed data.

---

## Dapper + SQLite Lessons

These are hard-won lessons from real bugs. Follow them strictly.

### 1. Dapper parameter binding is CASE-SENSITIVE

Anonymous object property names MUST match SQL `@Name` exactly.
```csharp
// BAD — lowercase 'id' won't bind to @Id
conn.QueryAsync("SELECT ... WHERE id = @Id", new { id });

// GOOD — case matches
conn.QueryAsync("SELECT ... WHERE id = @Id", new { Id });
```

### 2. Always alias columns explicitly

Dapper does NOT apply snake_case → PascalCase mapping.
```csharp
// BAD — email, full_name, is_active stay null/false
conn.QueryAsync("SELECT * FROM users");

// GOOD
public const string SelectColumns = "id AS Id, email AS Email, full_name AS FullName, CAST(is_active AS INTEGER) AS IsActive";
```

### 3. Use record class with init, not positional records

SQLite returns `long` (Int64) for all integers. Positional records require exact ctor signature match.
```csharp
// BAD — fails on Int64→int conversion for PermissionCount
public record RoleSummary(string Id, string Name, int PermissionCount);

// GOOD — Dapper applies Int64→int via setter
public record class RoleSummary
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public int PermissionCount { get; init; }
}
```

### 4. Don't map list/array columns through Dapper

Query scalar columns, then a second query with `IN @Ids` to build dicts.
```csharp
var rows = await conn.QueryAsync<UserRow>(sql);
var ids = rows.Select(r => r.Id).ToArray();
var roles = await conn.QueryAsync<(string, string)>(
    "SELECT user_id, role FROM user_roles WHERE user_id IN @Ids", new { Ids = ids });
// Then project by joining in-memory
```

### 5. `[AsParameters]` nullable backing fields

```csharp
public class PageQuery
{
    public int? Page { get; set; }     // nullable — omitted query param = null, not error
    public int PageValue => Page ?? 1; // computed default
}
```

### 6. Foreign keys are ON in Microsoft.Data.Sqlite

Always validate FK existence in the service layer before INSERT. SQLite enforcement is the last line of defense.

---

## Testing

Tests use **xUnit** with **FluentAssertions**. Tests are pure and fast — no database required for domain tests.

```bash
dotnet test backend/ProcureMax.Api.Tests/ProcureMax.Api.Tests.csproj
```

### Test Structure

```
ProcureMax.Api.Tests/
├── Money/
│   └── MoneyTests.cs              ← Money value object tests
├── Procurement/
│   └── PurchaseRequisitionTests.cs ← Domain workflow tests
```

### Writing Tests

```csharp
using FluentAssertions;
using Xunit;

public class MyTests
{
    [Fact]
    public void Method_does_what_it_should()
    {
        // Arrange
        var sut = new MyClass();

        // Act
        var result = sut.MyMethod();

        // Assert
        result.Should().Be(expected);
    }
}
```

---

## Frontend (Planned)

The frontend stack (not yet started):

- **Framework**: React 19 + Vite
- **Data Fetching**: TanStack Query v5
- **Forms**: React Hook Form + Zod
- **UI**: shadcn/ui + Tailwind v4 + lucide-react icons

Planned folder structure:

```
frontend/src/
├── lib/
│   └── http.ts                     ← Fetch wrapper with Bearer injection + 401 refresh
├── features/
│   └── <Slice>/
│       ├── routes.tsx              ← Route definitions
│       ├── api.ts                  ← API functions
│       ├── types.ts                ← TypeScript types
│       ├── schemas.ts              ← Zod schemas
│       ├── hooks.ts                ← TanStack Query hooks
│       ├── pages/
│       │   ├── ListPage.tsx
│       │   └── DetailPage.tsx
│       ├── components/
│       │   └── ...
│       └── nav.ts                  ← Sidebar nav entry with permission filter
```

---

## Deployment

### Current Architecture

```
Cloudflare Worker (edge proxy, rate limit, CORS)
    ↓
DigitalOcean App Platform (backend container)
    ↓
SQLite (DO persistent volume: /data/procuremax.db)
```

- DB path in production: `/data/procuremax.db`
- DB path in development: `./procuremax.db` (relative to project root)
- The `appsettings.json` connection string selector determines the active path

### Production Checklist

- [ ] Set `Auth:SigningSecret` to a ≥ 32 char random value via env variable
- [ ] Configure `Cors:AllowedOrigins` in app settings
- [ ] Disable Development-only OpenAPI/Scalar endpoints
- [ ] Ensure SQLite persistent volume is mounted at `/data/`
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
