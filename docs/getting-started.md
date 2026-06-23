# Getting Started

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (SDK 10.0.102+)
- A terminal (bash, zsh, PowerShell)

---

## Quick Start

```bash
# Clone the repository
git clone <repo-url>
cd procure-max

# Build the API
dotnet build backend/ProcureMax.Api/ProcureMax.Api.csproj

# Run in development mode
ASPNETCORE_ENVIRONMENT=Development \
  dotnet run --project backend/ProcureMax.Api/ProcureMax.Api.csproj \
             --urls "http://localhost:5000"
```

The API starts at **http://localhost:5000**.

### Verify it's running

```bash
curl http://localhost:5000/api/health
# → {"status":"ok","time":"2026-06-23T12:00:00Z"}
```

---

## API Documentation (Development)

Once the server is running, open the Scalar API reference UI:

- **Scalar UI**: [http://localhost:5000/scalar/v1](http://localhost:5000/scalar/v1)
- **Raw OpenAPI JSON**: [http://localhost:5000/openapi/v1.json](http://localhost:5000/openapi/v1.json)

> The OpenAPI endpoint is only available in `Development` environment.

---

## Seeded Admin Account

The first run seeds an admin user for development:

| Field | Value |
|---|---|
| **Email** | `admin@procuremax.local` |
| **Password** | `Admin#123` |
| **Role** | Admin (all 24 permissions) |

### Logging in

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@procuremax.local","password":"Admin#123"}'
```

Save the returned `accessToken` and use it as a Bearer token for subsequent requests:

```bash
TOKEN="<accessToken from response>"

curl http://localhost:5000/api/cost-centers \
  -H "Authorization: Bearer $TOKEN"
```

---

## Database Management

The SQLite database file is `backend/ProcureMax.Api/procuremax.db`.

### Reset the database

Delete the file and restart — migrations + seeds re-run automatically:

```bash
rm backend/ProcureMax.Api/procuremax.db
```

### Inspect the database

SQLite CLI is not available on all machines. Alternatives:

```bash
# Via curl
curl http://localhost:5000/api/health | python3 -m json.tool

# Or write a quick throwaway script with Microsoft.Data.Sqlite
```

---

## Running Tests

```bash
dotnet test backend/ProcureMax.Api.Tests/ProcureMax.Api.Tests.csproj
```

The test suite covers:

- **Money value object**: arithmetic, rounding, currency checks, comparison operators
- **PurchaseRequisition domain aggregate**: construction, line management, workflow transitions (submit, approve, reject, cancel, reopen), validation rules
