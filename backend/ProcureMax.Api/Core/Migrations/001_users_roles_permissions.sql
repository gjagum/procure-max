-- 001_users_roles_permissions.sql
-- Static schema for Users, Roles, Permissions, their junction tables, refresh tokens & audit logs.

CREATE TABLE users (
    id            TEXT PRIMARY KEY,
    email         TEXT NOT NULL UNIQUE,
    full_name     TEXT NOT NULL,
    password_hash TEXT NOT NULL,
    is_active     INTEGER NOT NULL DEFAULT 1,
    created_at    TEXT NOT NULL,
    created_by    TEXT,
    updated_at    TEXT,
    updated_by    TEXT,
    is_deleted    INTEGER NOT NULL DEFAULT 0,
    row_version   TEXT NOT NULL
);

CREATE TABLE roles (
    id          TEXT PRIMARY KEY,
    name        TEXT NOT NULL UNIQUE,
    description TEXT,
    created_at  TEXT NOT NULL,
    created_by  TEXT,
    is_system   INTEGER NOT NULL DEFAULT 0  -- system roles cannot be deleted
);

CREATE TABLE permissions (
    id     TEXT PRIMARY KEY,
    area   TEXT NOT NULL,
    action TEXT NOT NULL,
    UNIQUE(area, action)
);

CREATE TABLE role_permissions (
    role_id       TEXT NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    permission_id TEXT NOT NULL REFERENCES permissions(id) ON DELETE CASCADE,
    PRIMARY KEY (role_id, permission_id)
);

CREATE TABLE user_roles (
    user_id TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id TEXT NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    PRIMARY KEY (user_id, role_id)
);

CREATE TABLE refresh_tokens (
    id              TEXT PRIMARY KEY,
    user_id         TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash      TEXT NOT NULL UNIQUE,
    expires_at      TEXT NOT NULL,
    created_at      TEXT NOT NULL,
    revoked_at      TEXT,
    replaced_by_id  TEXT
);

CREATE INDEX ix_refresh_tokens_user ON refresh_tokens(user_id);
CREATE INDEX ix_refresh_tokens_expires ON refresh_tokens(expires_at);

CREATE TABLE audit_logs (
    id          TEXT PRIMARY KEY,
    user_id     TEXT,
    action      TEXT NOT NULL,
    entity      TEXT NOT NULL,
    entity_id   TEXT NOT NULL,
    before_json TEXT,
    after_json  TEXT,
    at          TEXT NOT NULL
);

CREATE INDEX ix_audit_entity ON audit_logs(entity, entity_id);
CREATE INDEX ix_audit_user ON audit_logs(user_id);
