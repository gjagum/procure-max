# Database Schema

SQLite database with 10 tables, tracked by a `__migrations` table.

---

## Migration 001 — Users, Roles, Permissions (Identity & Access)

### `users`

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `id` | TEXT | PK | `user:<guidN>` or `user:admin` |
| `email` | TEXT | NOT NULL, UNIQUE | |
| `full_name` | TEXT | NOT NULL | |
| `password_hash` | TEXT | NOT NULL | BCrypt hash |
| `is_active` | INTEGER | NOT NULL DEFAULT 1 | |
| `created_at` | TEXT | NOT NULL | ISO 8601 |
| `created_by` | TEXT | | |
| `updated_at` | TEXT | | |
| `updated_by` | TEXT | | |
| `is_deleted` | INTEGER | NOT NULL DEFAULT 0 | Soft-delete |
| `row_version` | TEXT | NOT NULL | Optimistic concurrency |

### `roles`

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `id` | TEXT | PK | `role:<key>` or `role:<guidN>` |
| `name` | TEXT | NOT NULL, UNIQUE | |
| `description` | TEXT | | |
| `created_at` | TEXT | NOT NULL | |
| `created_by` | TEXT | | |
| `is_system` | INTEGER | NOT NULL DEFAULT 0 | System roles cannot be deleted |

### `permissions`

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `id` | TEXT | PK | `perm:<area>:<action>` |
| `area` | TEXT | NOT NULL | |
| `action` | TEXT | NOT NULL | |
| | | UNIQUE(area, action) | |

### `role_permissions`

| Column | Type | Constraints |
|---|---|---|
| `role_id` | TEXT | PK, FK → roles(id) ON DELETE CASCADE |
| `permission_id` | TEXT | PK, FK → permissions(id) ON DELETE CASCADE |

### `user_roles`

| Column | Type | Constraints |
|---|---|---|
| `user_id` | TEXT | PK, FK → users(id) ON DELETE CASCADE |
| `role_id` | TEXT | PK, FK → roles(id) ON DELETE CASCADE |

### `refresh_tokens`

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `id` | TEXT | PK | UUID |
| `user_id` | TEXT | NOT NULL, FK → users(id) ON DELETE CASCADE | |
| `token_hash` | TEXT | NOT NULL, UNIQUE | SHA-256 hex |
| `expires_at` | TEXT | NOT NULL | |
| `created_at` | TEXT | NOT NULL | |
| `revoked_at` | TEXT | | NULL until revoked |
| `replaced_by_id` | TEXT | | Rotation chain tracking |

**Indexes:**
- `ix_refresh_tokens_user` ON (user_id)
- `ix_refresh_tokens_expires` ON (expires_at)

### `audit_logs`

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `id` | TEXT | PK | UUID |
| `user_id` | TEXT | | Actor who performed the action |
| `action` | TEXT | NOT NULL | e.g. 'CREATE', 'UPDATE', 'DELETE' |
| `entity` | TEXT | NOT NULL | e.g. 'user', 'cost_center' |
| `entity_id` | TEXT | NOT NULL | The affected resource ID |
| `before_json` | TEXT | | JSON snapshot before change |
| `after_json` | TEXT | | JSON snapshot after change |
| `at` | TEXT | NOT NULL | Timestamp |

**Indexes:**
- `ix_audit_entity` ON (entity, entity_id)
- `ix_audit_user` ON (user_id)

### `__migrations`

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `id` | INTEGER | PK AUTOINCREMENT | |
| `script_name` | TEXT | NOT NULL, UNIQUE | |
| `applied_at` | TEXT | NOT NULL | |

---

## Migration 002 — Procurement Master Data

### `cost_centers`

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `id` | TEXT | PK | `cc:<UPPERCODE>` |
| `code` | TEXT | NOT NULL, UNIQUE | e.g. `CC-1000` |
| `name` | TEXT | NOT NULL | |
| `is_active` | INTEGER | NOT NULL DEFAULT 1 | |
| `created_at` | TEXT | NOT NULL | |
| `created_by` | TEXT | | |
| `updated_at` | TEXT | | |
| `updated_by` | TEXT | | |
| `is_deleted` | INTEGER | NOT NULL DEFAULT 0 | |
| `row_version` | TEXT | NOT NULL | |

### `gl_accounts`

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `id` | TEXT | PK | `gl:<CODE>` |
| `code` | TEXT | NOT NULL, UNIQUE | e.g. `5000-OFFICE` |
| `name` | TEXT | NOT NULL | |
| `is_active` | INTEGER | NOT NULL DEFAULT 1 | |
| `created_at` | TEXT | NOT NULL | |
| `created_by` | TEXT | | |
| `updated_at` | TEXT | | |
| `updated_by` | TEXT | | |
| `is_deleted` | INTEGER | NOT NULL DEFAULT 0 | |
| `row_version` | TEXT | NOT NULL | |

### `units`

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `id` | TEXT | PK | `unit:<CODE>` |
| `code` | TEXT | NOT NULL, UNIQUE | e.g. `EA`, `KG` |
| `name` | TEXT | NOT NULL | |
| `is_active` | INTEGER | NOT NULL DEFAULT 1 | |
| `created_at` | TEXT | NOT NULL | |
| `created_by` | TEXT | | |
| `updated_at` | TEXT | | |
| `updated_by` | TEXT | | |
| `is_deleted` | INTEGER | NOT NULL DEFAULT 0 | |
| `row_version` | TEXT | NOT NULL | |

### `suppliers`

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `id` | TEXT | PK | `supplier:<guidN>` |
| `code` | TEXT | NOT NULL, UNIQUE | e.g. `SUP-0001` |
| `name` | TEXT | NOT NULL | |
| `legal_name` | TEXT | | |
| `tax_id` | TEXT | | |
| `contact_name` | TEXT | | |
| `email` | TEXT | | |
| `phone` | TEXT | | |
| `address_line1` | TEXT | | |
| `address_line2` | TEXT | | |
| `city` | TEXT | | |
| `state` | TEXT | | |
| `postal_code` | TEXT | | |
| `country` | TEXT | | |
| `payment_terms` | TEXT | | |
| `currency` | TEXT | NOT NULL DEFAULT 'USD' | |
| `is_active` | INTEGER | NOT NULL DEFAULT 1 | |
| `is_blocked` | INTEGER | NOT NULL DEFAULT 0 | |
| `created_at` | TEXT | NOT NULL | |
| `created_by` | TEXT | | |
| `updated_at` | TEXT | | |
| `updated_by` | TEXT | | |
| `is_deleted` | INTEGER | NOT NULL DEFAULT 0 | |
| `row_version` | TEXT | NOT NULL | |

**Index:**
- `ix_suppliers_active` ON (is_active, is_blocked) WHERE is_deleted = 0

### `items`

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `id` | TEXT | PK | `item:<guidN>` |
| `sku` | TEXT | NOT NULL, UNIQUE | |
| `name` | TEXT | NOT NULL | |
| `description` | TEXT | | |
| `category` | TEXT | | |
| `unit_id` | TEXT | FK → units(id) | |
| `gl_account_id` | TEXT | FK → gl_accounts(id) | |
| `default_supplier_id` | TEXT | FK → suppliers(id) | |
| `default_price_minor` | INTEGER | NOT NULL DEFAULT 0 | |
| `default_currency` | TEXT | NOT NULL DEFAULT 'USD' | |
| `is_active` | INTEGER | NOT NULL DEFAULT 1 | |
| `created_at` | TEXT | NOT NULL | |
| `created_by` | TEXT | | |
| `updated_at` | TEXT | | |
| `updated_by` | TEXT | | |
| `is_deleted` | INTEGER | NOT NULL DEFAULT 0 | |
| `row_version` | TEXT | NOT NULL | |

**Index:**
- `ix_items_active` ON (category, name) WHERE is_deleted = 0

---

## FK Relationships

```
items.unit_id              → units(id)
items.gl_account_id        → gl_accounts(id)
items.default_supplier_id  → suppliers(id)
user_roles.user_id         → users(id) ON DELETE CASCADE
user_roles.role_id         → roles(id) ON DELETE CASCADE
role_permissions.role_id       → roles(id) ON DELETE CASCADE
role_permissions.permission_id → permissions(id) ON DELETE CASCADE
refresh_tokens.user_id     → users(id) ON DELETE CASCADE
```

> Foreign keys are enforced by SQLite (`PRAGMA foreign_keys = ON` is set by `Microsoft.Data.Sqlite`).

---

## Seeded Data

| Table | Rows |
|---|---|
| `permissions` | 24 permission rows |
| `roles` | 5 system roles (admin, requestor, approver, buyer, apclerk) |
| `role_permissions` | Role-to-permission bindings |
| `users` | `admin@procuremax.local` / `Admin#123` |
| `user_roles` | Admin user → admin role |
| `cost_centers` | 3 sample: CC-1000 (Engineering), CC-2000 (Marketing), CC-3000 (Operations) |
| `gl_accounts` | 4 sample: 5000-OFFICE, 5000-IT, 6000-TRAVEL, 7000-CONTRACT |
| `units` | 5 sample: EA, KG, LB, HR, SRV |
