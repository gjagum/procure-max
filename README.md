# ProcureMax

Procurement management platform — .NET 10 API with JWT-based RBAC, SQLite, and a React 19 + TanStack Router frontend.

---

## Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 10.0.102+ |
| Node.js | 22+ (managed via nvm) |
| npm | 10+ |

---

## Quick Start

### 1. Backend (.NET 10 API)

```bash
# Build
dotnet build backend/ProcureMax.Api/ProcureMax.Api.csproj

# Run (dev server on http://localhost:5000)
ASPNETCORE_ENVIRONMENT=Development \
  dotnet run --project backend/ProcureMax.Api/ProcureMax.Api.csproj \
             --urls "http://localhost:5000"
```

On first run, the database is auto-created at `backend/ProcureMax.Api/procuremax.db` with all migrations and seed data applied.

**Admin dev seed:**

```
Email:    admin@procuremax.local
Password: Admin#123
```

To reset the database (replays all migrations + seeds on next run):

```bash
rm backend/ProcureMax.Api/procuremax.db
```

### 2. Frontend (React + Vite)

```bash
cd frontend
npm install
npm run dev
```

The dev server starts on `http://localhost:5173` and proxies `/api/*` to the backend on port 5000.

**Login** at `http://localhost:5173/login` with the admin seed credentials above.

### 3. Run both together

Open two terminals:

```bash
# Terminal 1 — Backend
ASPNETCORE_ENVIRONMENT=Development \
  dotnet run --project backend/ProcureMax.Api/ProcureMax.Api.csproj \
             --urls "http://localhost:5000"

# Terminal 2 — Frontend
cd frontend && npm run dev
```

---

## Project Structure

```
procure-max/
├── backend/
│   ├── ProcureMax.Api/           # .NET 10 Minimal API + Dapper + SQLite
│   │   ├── Features/             # Vertical slices (Auth, Users, Suppliers, Items, ...)
│   │   ├── Core/                 # Auth, Authorization, Migrations, Middleware
│   │   └── Program.cs
│   └── ProcureMax.Api.Tests/
├── frontend/                     # React 19 + Vite 8 + TanStack Router
│   └── src/
│       ├── components/           # UI components (DataTable, Badge, Sidebar, TopBar)
│       ├── features/             # API clients per slice
│       ├── lib/                  # HTTP client, types, query client
│       └── routes/               # TanStack Router file-based routes
├── docs/                         # Architecture docs
└── AGENTS.md                     # AI agent conventions
```

---

## Commands

### Backend

| Command | Description |
|---|---|
| `dotnet build backend/ProcureMax.Api/ProcureMax.Api.csproj` | Build the API |
| `dotnet run --project backend/ProcureMax.Api/ProcureMax.Api.csproj` | Run the API |
| `dotnet test backend/ProcureMax.Api.Tests/ProcureMax.Api.Tests.csproj` | Run tests |

### Frontend

| Command | Description |
|---|---|
| `npm run dev` | Start Vite dev server (port 5173) |
| `npm run build` | Production build (`tsc + vite build`) |
| `npm run typecheck` | TypeScript type-check only |

---

## Tech Stack

**Backend:** .NET 10 Minimal APIs · Dapper · SQLite · JWT (HS256) · FluentValidation · BCrypt

**Frontend:** React 19 · Vite 8 · TanStack Router · TanStack Query v5 · Tailwind CSS v4 · lucide-react

---

## Further Reading

- [AGENTS.md](./AGENTS.md) — Conventions for AI coding agents
- [Architecture docs](./docs/README.md)
