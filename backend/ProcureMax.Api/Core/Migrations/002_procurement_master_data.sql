-- 002_procurement_master_data.sql
-- Static schema for the procurement master data slices that PRs/POs reference:
--
--   cost_centers : organizational budget owner; required for every PR
--   gl_accounts  : general-ledger posting account; one per item or per line
--   units        : unit-of-measure table (EA, KG, M, etc.) referenced by items/lines
--   suppliers    : vendors from whom we purchase; required by POs
--   items        : the catalog of goods/services purchased; references units, gl_accounts, suppliers
--
-- All master tables follow the same conventions as 001:
--   * TEXT primary key with '<entity>:<guid>' convention
--   * is_active / is_deleted for status and soft-delete
--   * created_at/by, updated_at/by audit columns
--   * row_version TEXT as optimistic-concurrency token (server rotates on each write)

CREATE TABLE cost_centers (
    id          TEXT PRIMARY KEY,
    code        TEXT NOT NULL UNIQUE,        -- e.g. 'CC-1000' — human-readable stable key
    name        TEXT NOT NULL,
    is_active   INTEGER NOT NULL DEFAULT 1,
    created_at  TEXT NOT NULL,
    created_by  TEXT,
    updated_at  TEXT,
    updated_by  TEXT,
    is_deleted  INTEGER NOT NULL DEFAULT 0,
    row_version TEXT NOT NULL
);

CREATE TABLE gl_accounts (
    id          TEXT PRIMARY KEY,
    code        TEXT NOT NULL UNIQUE,        -- e.g. '5000-OFFICE' — the GL account code
    name        TEXT NOT NULL,
    is_active   INTEGER NOT NULL DEFAULT 1,
    created_at  TEXT NOT NULL,
    created_by  TEXT,
    updated_at  TEXT,
    updated_by  TEXT,
    is_deleted  INTEGER NOT NULL DEFAULT 0,
    row_version TEXT NOT NULL
);

CREATE TABLE units (
    id          TEXT PRIMARY KEY,
    code        TEXT NOT NULL UNIQUE,        -- 'EA', 'KG', 'M', 'BOX', etc. (case-insensitive compare in C#)
    name        TEXT NOT NULL,               -- 'Each', 'Kilogram', etc.
    is_active   INTEGER NOT NULL DEFAULT 1,
    created_at  TEXT NOT NULL,
    created_by  TEXT,
    updated_at  TEXT,
    updated_by  TEXT,
    is_deleted  INTEGER NOT NULL DEFAULT 0,
    row_version TEXT NOT NULL
);

CREATE TABLE suppliers (
    id              TEXT PRIMARY KEY,
    code            TEXT NOT NULL UNIQUE,    -- 'SUP-0001' — internal stable id
    name            TEXT NOT NULL,
    legal_name      TEXT,                    -- legal entity name (may differ from display name)
    tax_id          TEXT,                    -- VAT/EIN/etc.
    contact_name    TEXT,
    email           TEXT,
    phone           TEXT,
    address_line1   TEXT,
    address_line2   TEXT,
    city            TEXT,
    state           TEXT,
    postal_code     TEXT,
    country         TEXT,
    payment_terms   TEXT,                    -- 'Net 30', 'Net 60', etc.
    currency        TEXT NOT NULL DEFAULT 'USD',
    is_active       INTEGER NOT NULL DEFAULT 1,
    is_blocked      INTEGER NOT NULL DEFAULT 0, -- set by AP clerk when vendor is on hold
    created_at      TEXT NOT NULL,
    created_by      TEXT,
    updated_at      TEXT,
    updated_by      TEXT,
    is_deleted      INTEGER NOT NULL DEFAULT 0,
    row_version     TEXT NOT NULL
);

CREATE INDEX ix_suppliers_active ON suppliers(is_active, is_blocked) WHERE is_deleted = 0;

CREATE TABLE items (
    id              TEXT PRIMARY KEY,
    sku             TEXT NOT NULL UNIQUE,    -- internal stock-keeping code
    name            TEXT NOT NULL,
    description     TEXT,
    category        TEXT,                    -- free-text grouping ('Office Supplies', 'IT', 'Raw Materials')
    unit_id         TEXT REFERENCES units(id),       -- primary UoM
    gl_account_id   TEXT REFERENCES gl_accounts(id), -- default GL to post to
    default_supplier_id TEXT REFERENCES suppliers(id),
    default_price_minor  INTEGER NOT NULL DEFAULT 0, -- current list price in minor units
    default_currency    TEXT NOT NULL DEFAULT 'USD',
    is_active       INTEGER NOT NULL DEFAULT 1,
    created_at      TEXT NOT NULL,
    created_by      TEXT,
    updated_at      TEXT,
    updated_by      TEXT,
    is_deleted      INTEGER NOT NULL DEFAULT 0,
    row_version     TEXT NOT NULL
);

CREATE INDEX ix_items_active ON items(category, name) WHERE is_deleted = 0;
