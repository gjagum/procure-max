# API Reference

All endpoints are prefixed with `/api`. Authentication uses Bearer JWT tokens.

---

## Meta

### `GET /api/health`

Public health check.

**Response** `200 OK`
```json
{ "status": "ok", "time": "2026-06-23T12:00:00Z" }
```

---

## Authentication

### `POST /api/auth/login`

Authenticate with email and password.

**Request**
```json
{
  "email": "admin@procuremax.local",
  "password": "Admin#123"
}
```

**Response** `200 OK`
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "a1b2c3d4...",
  "expiresIn": 900,
  "user": {
    "id": "user:admin",
    "email": "admin@procuremax.local",
    "fullName": "Admin User",
    "roles": ["admin"],
    "permissions": ["dashboard.view", "users.manage", "..."]
  }
}
```

**Errors**: `401 Unauthorized` on invalid credentials or inactive account.

---

### `POST /api/auth/refresh`

Rotate a refresh token and issue new tokens.

**Request**
```json
{ "refreshToken": "a1b2c3d4..." }
```

**Response** `200 OK` — Same shape as login response.

---

### `POST /api/auth/logout`

Revoke the current refresh token. Requires authentication.

**Request**
```json
{ "refreshToken": "a1b2c3d4..." }
```

**Response** `204 No Content`

---

### `GET /api/auth/me`

Get the current user's profile. Requires authentication.

**Response** `200 OK`
```json
{
  "id": "user:admin",
  "email": "admin@procuremax.local",
  "fullName": "Admin User",
  "roles": ["admin"],
  "permissions": ["dashboard.view", "users.manage", "..."]
}
```

---

## Users

All endpoints require `users.manage` permission.

### `GET /api/users`

List users (paged).

| Query Param | Type | Default | Description |
|---|---|---|---|
| `Page` | int | 1 | Page number |
| `PageSize` | int | 20 | Items per page |
| `Search` | string | — | Filter by email or name |

**Response** `200 OK`
```json
{
  "items": [{ "id": "user:admin", "email": "...", "fullName": "...", "isActive": true, "roles": ["admin"], "createdAt": "...", "rowVersion": "..." }],
  "total": 1,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

---

### `GET /api/users/{id}`

Get user detail.

**Response** `200 OK`
```json
{
  "id": "user:admin",
  "email": "admin@procuremax.local",
  "fullName": "Admin User",
  "isActive": true,
  "roles": ["admin"],
  "permissions": ["dashboard.view", "users.manage"],
  "createdAt": "...",
  "updatedAt": null,
  "rowVersion": "..."
}
```

---

### `POST /api/users`

Create a new user.

**Request**
```json
{
  "email": "jane@example.com",
  "fullName": "Jane Doe",
  "password": "SecurePass123!",
  "roleIds": ["role:requestor"]
}
```

**Response** `201 Created`
```json
{ "id": "user:a1b2c3d4..." }
```

---

### `PUT /api/users/{id}`

Update user details (name, active status).

**Request**
```json
{
  "fullName": "Jane Updated",
  "isActive": true,
  "rowVersion": "..."
}
```

**Response** `204 No Content`

---

### `PUT /api/users/{id}/roles`

Replace all role assignments.

**Request**
```json
{
  "roleIds": ["role:requestor", "role:buyer"]
}
```

**Response** `204 No Content`

---

### `DELETE /api/users/{id}`

Soft-delete a user. Self-deletion is blocked.

**Response** `204 No Content`

---

## Roles

All endpoints require `roles.manage` permission.

### `GET /api/roles`

List roles (paged).

| Query Param | Type | Default | Description |
|---|---|---|---|
| `Page` | int | 1 | |
| `PageSize` | int | 20 | |
| `Search` | string | — | Filter by name |

**Response** `200 OK`
```json
{
  "items": [{ "id": "role:admin", "name": "Admin", "description": "...", "isSystem": true, "permissionCount": 24 }],
  "total": 6,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

---

### `GET /api/roles/{id}`

Get role detail with all permission assignments.

**Response** `200 OK`
```json
{
  "id": "role:admin",
  "name": "Admin",
  "description": "Full system access",
  "isSystem": true,
  "permissionIds": ["perm:dashboard:view", "perm:users:manage", "..."],
  "allPermissions": [{ "id": "perm:dashboard:view", "area": "dashboard", "action": "view" }]
}
```

---

### `POST /api/roles`

Create a custom role.

**Request**
```json
{
  "name": "Supervisor",
  "description": "Department supervisor",
  "permissionIds": ["perm:dashboard:view", "perm:pr:view"]
}
```

**Response** `201 Created`
```json
{ "id": "role:a1b2c3d4..." }
```

---

### `PUT /api/roles/{id}`

Update role description and permission assignments. System roles are restricted.

**Request**
```json
{
  "description": "Updated description",
  "permissionIds": ["perm:dashboard:view", "perm:pr:view", "perm:pr:approve.own_cost_center"],
  "rowVersion": "..."
}
```

**Response** `204 No Content`

---

### `DELETE /api/roles/{id}`

Delete a custom role. System roles and admin role are protected.

**Response** `204 No Content`

---

## Cost Centers

All endpoints require `costcenter.manage` permission.

### `GET /api/cost-centers`

### `GET /api/cost-centers/{id}`

### `POST /api/cost-centers`

**Request**
```json
{ "code": "CC-1000", "name": "Engineering" }
```

**Response** `201 Created`
```json
{ "id": "cc:CC-1000" }
```

---

### `PUT /api/cost-centers/{id}`

**Request**
```json
{ "name": "Engineering Dept", "isActive": true, "rowVersion": "..." }
```

**Response** `204 No Content`

---

### `DELETE /api/cost-centers/{id}?rowVersion=...`

**Response** `204 No Content`

> **Warning**: Deleting a cost center soft-deletes it. The `rowVersion` query parameter is **required**.

---

## GL Accounts

All endpoints require `glaccount.manage` permission.

### `GET /api/gl-accounts`

### `GET /api/gl-accounts/{id}`

### `POST /api/gl-accounts`

**Request**
```json
{ "code": "5000-OFFICE", "name": "Office Supplies" }
```

### `PUT /api/gl-accounts/{id}`

### `DELETE /api/gl-accounts/{id}?rowVersion=...`

Same CRUD pattern as cost centers.

---

## Units of Measure

All endpoints require `unit.manage` permission.

### `GET /api/units`

### `GET /api/units/{id}`

### `POST /api/units`

**Request**
```json
{ "code": "KG", "name": "Kilogram" }
```

### `PUT /api/units/{id}`

### `DELETE /api/units/{id}?rowVersion=...`

Same CRUD pattern.

---

## Suppliers

Read endpoints require `supplier.view`, mutations require `supplier.manage`.

### `GET /api/suppliers`

| Query Param | Type | Default | Description |
|---|---|---|---|
| `Page` | int | 1 | |
| `PageSize` | int | 20 | |
| `Search` | string | — | Filter by code, name, legal name |

**Response** `200 OK`
```json
{
  "items": [{ "id": "supplier:abc...", "code": "SUP-0001", "name": "Acme Corp", "legalName": "...", "isActive": true, "isBlocked": false, "currency": "USD" }]
}
```

---

### `GET /api/suppliers/{id}`

Full detail with address, tax ID, contact info, payment terms, etc.

---

### `POST /api/suppliers`

**Request**
```json
{
  "code": "SUP-0001",
  "name": "Acme Corporation",
  "legalName": "Acme Corp LLC",
  "taxId": "12-3456789",
  "contactName": "John Smith",
  "email": "john@acme.com",
  "phone": "+1-555-0100",
  "addressLine1": "123 Main St",
  "city": "New York",
  "state": "NY",
  "postalCode": "10001",
  "country": "US",
  "paymentTerms": "NET30",
  "currency": "USD"
}
```

**Response** `201 Created`
```json
{ "id": "supplier:abc..." }
```

---

### `PUT /api/suppliers/{id}`

### `DELETE /api/suppliers/{id}?rowVersion=...`

---

## Items

Read endpoints require `item.view`, mutations require `item.manage`.

### `GET /api/items`

| Query Param | Type | Default | Description |
|---|---|---|---|
| `Page` | int | 1 | |
| `PageSize` | int | 20 | |
| `Search` | string | — | Filter by SKU, name, category |

**Response** `200 OK`
```json
{
  "items": [{ "id": "item:def...", "sku": "LAP-001", "name": "Laptop", "category": "Electronics", "defaultPriceMinor": 120000, "defaultCurrency": "USD", "isActive": true }]
}
```

---

### `GET /api/items/{id}`

Full detail including resolved unit code, GL account code, and default supplier code.

---

### `POST /api/items`

**Request**
```json
{
  "sku": "LAP-001",
  "name": "Laptop Computer",
  "description": "14-inch business laptop",
  "category": "Electronics",
  "unitId": "unit:EA",
  "glAccountId": "gl:5000-OFFICE",
  "defaultSupplierId": "supplier:abc...",
  "defaultPriceMinor": 120000,
  "defaultCurrency": "USD"
}
```

**Response** `201 Created`
```json
{ "id": "item:def..." }
```

FK references (`unitId`, `glAccountId`, `defaultSupplierId`) are validated against their tables before insert.

---

### `PUT /api/items/{id}`

### `DELETE /api/items/{id}?rowVersion=...`

---

## Common Response Formats

### Success

- `200 OK` — List or detail
- `201 Created` — Resource created (`Location` header + `IdResponse` body)
- `204 No Content` — Mutation succeeded (update, delete)

### Error (RFC 9457 Problem Details)

All errors follow the Problem Details RFC:

```json
{
  "type": "https://httpstatuses.io/400",
  "title": "Validation Error",
  "status": 400,
  "detail": "Code 'CC-1000' already exists.",
  "traceId": "abc123def456",
  "errors": {
    "Code": ["'Code' must not be empty."]
  }
}
```

| HTTP Status | When |
|---|---|
| `400 Bad Request` | Validation errors |
| `401 Unauthorized` | Missing/invalid JWT |
| `403 Forbidden` | Missing required permission |
| `404 Not Found` | Resource not found |
| `409 Conflict` | Stale `rowVersion` or duplicate code |
| `500 Internal Server Error` | Unexpected error (detail hidden in production) |
