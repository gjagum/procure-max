using System.Data;
using Dapper;
using ProcureMax.Core;

namespace ProcureMax.Features.Roles;

public interface IRoleService
{
    Task<Paged<RoleSummary>> ListAsync(PageQuery q, CancellationToken ct);
    Task<RoleDetail> GetAsync(string id, CancellationToken ct);
    Task<string> CreateAsync(CreateRoleRequest req, string actorId, CancellationToken ct);
    Task UpdateAsync(string id, UpdateRoleRequest req, string actorId, CancellationToken ct);
    Task DeleteAsync(string id, string actorId, CancellationToken ct);
}

public class RoleService(IDbConnectionFactory factory) : IRoleService
{
    private readonly IDbConnectionFactory _f = factory;

    public async Task<Paged<RoleSummary>> ListAsync(PageQuery q, CancellationToken ct)
    {
        using var conn = _f.Create();
        const string where = "WHERE (@Search IS NULL OR r.name LIKE @Like OR r.description LIKE @Like)";
        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM roles r {where};", new { Search = q.Search is null ? (string?)null : q.Search, Like = $"%{q.Search ?? ""}%" });

        var sql = $@"
            SELECT r.id AS Id, r.name AS Name, r.description AS Description,
                   CAST(r.is_system AS INTEGER) AS IsSystem,
                   CAST((SELECT COUNT(*) FROM role_permissions rp WHERE rp.role_id = r.id) AS INTEGER) AS PermissionCount
              FROM roles r {where}
             ORDER BY r.name
             LIMIT @Limit OFFSET @Offset;";
        var items = (await conn.QueryAsync<RoleSummary>(sql, new { Search = q.Search is null ? (string?)null : q.Search, Like = $"%{q.Search ?? ""}%",
            Limit = q.PageSizeValue, Offset = q.Offset })).ToList();
        return new Paged<RoleSummary>(items, total, Math.Max(q.PageValue, 1), q.PageSizeValue,
            (int)Math.Ceiling((double)total / q.PageSizeValue));
    }

    public async Task<RoleDetail> GetAsync(string id, CancellationToken ct)
    {
        using var conn = _f.Create();
        var role = await conn.QuerySingleOrDefaultAsync<RoleSummary>(
            "SELECT r.id AS Id, r.name AS Name, r.description AS Description, " +
            "CAST(r.is_system AS INTEGER) AS IsSystem, " +
            "CAST((SELECT COUNT(*) FROM role_permissions rp WHERE rp.role_id = r.id) AS INTEGER) AS PermissionCount " +
            "FROM roles r WHERE r.id = @Id;",
            new { Id = id }) ?? throw new NotFoundException("Role", id);
        var permIds = (await conn.QueryAsync<string>(
            "SELECT permission_id FROM role_permissions WHERE role_id = @Id;", new { Id = id })).ToList();
        var all = (await conn.QueryAsync<PermissionInfo>("SELECT id AS Id, area AS Area, action AS Action FROM permissions ORDER BY area, action;")).ToList();
        return new RoleDetail(role.Id, role.Name, role.Description, role.IsSystem, permIds, all);
    }

    public async Task<string> CreateAsync(CreateRoleRequest req, string actorId, CancellationToken ct)
    {
        using var conn = _f.Create();
        using var tx = conn.BeginTransaction();
        var id = $"role:{Guid.NewGuid():N}";
        var duplicate = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM roles WHERE lower(name) = lower(@Name);", new { req.Name }, tx);
        if (duplicate > 0) throw new ConflictException($"Role '{req.Name}' already exists.");
        await conn.ExecuteAsync("INSERT INTO roles (id, name, description, created_at, created_by, is_system) VALUES (@Id, @Name, @Desc, @Now, @Actor, 0);",
            new { Id = id, Name = req.Name, Desc = req.Description, Now = DateTime.UtcNow.ToString("O"), Actor = actorId }, tx);
        await BindPermissionsAsync(conn, tx, id, req.PermissionIds);
        await AuditLog.WriteAsync(conn, tx, actorId, "role.create", "roles", id, after: new { req.Name, req.Description, Permissions = req.PermissionIds });
        tx.Commit();
        return id;
    }

    public async Task UpdateAsync(string id, UpdateRoleRequest req, string actorId, CancellationToken ct)
    {
        using var conn = _f.Create();
        using var tx = conn.BeginTransaction();
        var role = await conn.QuerySingleOrDefaultAsync<(string Name, string? Description, bool IsSystem, string RowVersion)?>(
            "SELECT name AS Name, description AS Description, is_system AS IsSystem, '' AS RowVersion FROM roles WHERE id = @Id;", new { Id = id }, tx)
            ?? throw new NotFoundException("Role", id);
        if (req.RowVersion != id) throw new ConflictException("Role row version mismatch.");
        if (req.PermissionIds is not null)
        {
            if (role.IsSystem) throw new DomainException("Cannot change permissions on a system role.");
            await BindPermissionsAsync(conn, tx, id, req.PermissionIds);
        }
        if (req.Description is not null)
        {
            await conn.ExecuteAsync("UPDATE roles SET description = @Desc WHERE id = @Id;", new { Desc = req.Description, Id = id }, tx);
        }
        await AuditLog.WriteAsync(conn, tx, actorId, "role.update", "roles", id, after: new { req.Description, Permissions = req.PermissionIds });
        tx.Commit();
    }

    public async Task DeleteAsync(string id, string actorId, CancellationToken ct)
    {
        using var conn = _f.Create();
        using var tx = conn.BeginTransaction();
        var isSystem = await conn.ExecuteScalarAsync<int>("SELECT is_system FROM roles WHERE id = @Id;", new { Id = id }, tx);
        if (isSystem == 1) throw new DomainException("System roles cannot be deleted.");
        if (id == "role:admin") throw new DomainException("The Admin role cannot be deleted.");
        var inUse = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM user_roles WHERE role_id = @Id;", new { Id = id }, tx);
        if (inUse > 0) throw new ConflictException($"Role is assigned to {inUse} user(s); remove before deleting.");
        await conn.ExecuteAsync("DELETE FROM role_permissions WHERE role_id = @Id;", new { Id = id }, tx);
        await conn.ExecuteAsync("DELETE FROM roles WHERE id = @Id;", new { Id = id }, tx);
        await AuditLog.WriteAsync(conn, tx, actorId, "role.delete", "roles", id);
        tx.Commit();
    }

    private static async Task BindPermissionsAsync(IDbConnection conn, IDbTransaction tx, string roleId, IReadOnlyCollection<string> permIds)
    {
        await conn.ExecuteAsync("DELETE FROM role_permissions WHERE role_id = @Id;", new { Id = roleId }, tx);
        foreach (var pid in permIds.Distinct())
            await conn.ExecuteAsync("INSERT OR IGNORE INTO role_permissions (role_id, permission_id) VALUES (@R, @P);", new { R = roleId, P = pid }, tx);
    }
}
