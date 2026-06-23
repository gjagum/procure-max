# Authentication & Authorization

## Overview

ProcureMax uses **JWT Bearer authentication** (HS256) with short-lived access tokens and rotating refresh tokens. Authorization is **permission-based RBAC**: users are assigned to roles, roles grant permissions, and permissions control access to individual endpoints.

```
User → Role(s) → Permission(s) → Endpoint
```

---

## Authentication Flow

### Login

```
┌─────────┐         POST /api/auth/login        ┌──────────┐
│ Client  │ ─────── { email, password } ───────→ │   API    │
│         │                                      │          │
│         │ ←── { accessToken, refreshToken } ─── │          │
└─────────┘                                      └──────────┘
```

1. Client sends `email` + `password`
2. Server looks up user by email, validates `is_active` flag
3. BCrypt verifies password hash
4. On success:
   - `JwtTokenService` issues an **access token** (15 min TTL, HS256-signed)
   - `RefreshTokenStore` creates a **refresh token** (7 day TTL, SHA-256 hash stored)
5. Returns `AuthResponse` with both tokens + user profile
6. On failure: `401 Unauthorized`

### Token Refresh (Rotation)

```
┌─────────┐       POST /api/auth/refresh        ┌──────────┐
│ Client  │ ─────── { refreshToken } ──────────→ │   API    │
│         │                                      │          │
│         │ ←── { accessToken, refreshToken } ─── │          │
└─────────┘                                      └──────────┘
```

- Old refresh token is **revoked** (replaced_by_id set)
- New refresh token is issued (replay-safe rotation)
- If a revoked token is reused, the entire chain is invalidated (compromised token protection)

### Logout

```
┌─────────┐       POST /api/auth/logout         ┌──────────┐
│ Client  │ ─────── { refreshToken } ──────────→ │   API    │
│         │ (Authenticated)                      │          │
└─────────┘                                      └──────────┘
```

- Sets `revoked_at` on the matching refresh token
- Client should discard both tokens

---

## JWT Token Structure

### Access Token (15 min)

```json
{
  "sub": "user:a1b2c3d4...",
  "email": "user@example.com",
  "name": "John Doe",
  "role": "requestor",
  "perm": ["pr.create", "pr.view", "item.view", "supplier.view"],
  "jti": "unique-id",
  "iss": "ProcureMax",
  "aud": "procuremax-api",
  "iat": 1700000000,
  "exp": 1700000900
}
```

Key claims:
- `sub` — User ID (the `user:<guid>` string)
- `role` — Primary role name (single string; for RBAC display)
- `perm` — Array of permission claim values (used by the authorization handler)
- `jti` — Unique token identifier (for logging/audit)

### Refresh Token (7 days)

- Opaque string (GUID), not a JWT
- Hashed (SHA-256) before storage
- One-time use via rotation

---

## Authorization System

Three layers work together to enforce permissions:

### 1. `HasPermission()` Extension (Endpoints)

Used on Minimal API route groups to gate individual routes:

```csharp
group.MapGet("/", ListAsync).HasPermission(Permissions.ItemView);
group.MapPost("/", CreateAsync).HasPermission(Permissions.ItemManage);
```

This sets the route's policy name to `perm:<claim_value>` (e.g., `perm:item.view`).

### 2. `PermissionPolicyProvider`

Custom `IAuthorizationPolicyProvider` that intercepts policy names starting with `perm:`:

```csharp
// Framework calls GetPolicyAsync("perm:item.view")
// → PermissionPolicyProvider builds an AuthorizationPolicy
//   with a PermissionRequirement("item.view")
```

For policy names not starting with `perm:`, it falls back to the default provider.

### 3. `PermissionHandler`

Custom `AuthorizationHandler<PermissionRequirement>` that inspects the user's JWT `perm` claims:

```csharp
// Does the user have a claim of type "perm" with value "item.view"?
// If yes → allow; if no → 403 Forbidden
```

---

## Permission Catalog (24 Permissions)

### Dashboard

| Permission | Granted To |
|---|---|
| `dashboard.view` | admin, requestor, approver, buyer, apclerk |

### User & Role Management

| Permission | Granted To |
|---|---|
| `users.manage` | admin |
| `roles.manage` | admin |

### Master Data Management

| Permission | Granted To |
|---|---|
| `costcenter.manage` | admin |
| `glaccount.manage` | admin |
| `unit.manage` | admin |
| `item.view` | admin, requestor, approver, buyer, apclerk |
| `item.manage` | admin |
| `supplier.view` | admin, requestor, approver, buyer, apclerk |
| `supplier.manage` | admin |

### Procurement (PR)

| Permission | Granted To |
|---|---|
| `pr.create` | admin, requestor |
| `pr.view` | admin, requestor, approver, buyer, apclerk |
| `pr.approve.own_cost_center` | admin, approver |
| `pr.approve.all` | admin, approver |

### Procurement (PO)

| Permission | Granted To |
|---|---|
| `po.create` | admin, buyer |
| `po.view` | admin, approver, buyer, apclerk |
| `po.issue` | admin, buyer |
| `po.approve` | admin, approver |

### Goods Receipt

| Permission | Granted To |
|---|---|
| `gr.create` | admin, buyer, apclerk |

### Invoice

| Permission | Granted To |
|---|---|
| `invoice.manage` | admin, apclerk |
| `invoice.approve` | admin, apclerk |

### Approval Rules

| Permission | Granted To |
|---|---|
| `approval.rules.manage` | admin |

### Two-Tier Permission Model

Most master data follows a read/write split:
- **GET** endpoints: gated to `*.view`
- **POST/PUT/DELETE** endpoints: gated to `*.manage`

**Exceptions:** Cost Centers, GL Accounts, and Units gate both reads and writes to `*.manage` (these are sensitive for approval routing configuration).

---

## System Roles (Seeded)

| Role | Description | Permissions |
|---|---|---|
| **Admin** | Full system access | All 24 |
| **Requestor** | Creates purchase requisitions | `dashboard.view`, `pr.create`, `pr.view`, `item.view`, `supplier.view` |
| **Approver** | Approves requisitions & POs | `dashboard.view`, `pr.view`, `pr.approve.own_cost_center`, `pr.approve.all`, `po.view`, `po.approve` |
| **Buyer** | Creates and issues POs | `dashboard.view`, `po.create`, `po.view`, `po.issue`, `pr.view`, `item.view`, `supplier.view` |
| **AP Clerk** | Manages invoices & goods receipt | `dashboard.view`, `invoice.manage`, `invoice.approve`, `gr.view`, `po.view` |

> System roles (seeded with `is_system = 1`) cannot be deleted and have restricted update capabilities.

---

## Security Notes

- **Access tokens** are short-lived (15 minutes) to limit exposure
- **Refresh tokens** are one-time use via rotation — replay detection invalidates the entire chain
- **Passwords** are hashed with BCrypt (not stored in plaintext)
- **JWT signing secret** must be ≥ 32 characters in production (falls back to dev-only secret in Development)
- **SQLite FK enforcement** is active — the last line of defense for referential integrity
- **Problem Details** errors never expose stack traces in production
