using System.Data;
using Dapper;
using ProcureMax.Core;

namespace ProcureMax.Features.Users;

public interface IUserService
{
    Task<Paged<UserSummary>> ListAsync(PageQuery q, CancellationToken ct);
    Task<UserDetail> GetAsync(string id, CancellationToken ct);
    Task<string> CreateAsync(CreateUserRequest req, string actorId, CancellationToken ct);
    Task UpdateAsync(string id, UpdateUserRequest req, string actorId, CancellationToken ct);
    Task AssignRolesAsync(string id, AssignRolesRequest req, string actorId, CancellationToken ct);
    Task DeleteAsync(string id, string actorId, CancellationToken ct);
}

// All writes join on the same connection/transaction; reads reuse a one-shot connection.
public class UserService(IDbConnectionFactory factory, ICurrentUser current) : IUserService
{
    private readonly IDbConnectionFactory _f = factory;
    private readonly ICurrentUser _current = current;

    public async Task<Paged<UserSummary>> ListAsync(PageQuery q, CancellationToken ct)
    {
        using var conn = _f.Create();
        const string where = "WHERE is_deleted = 0 AND (@Search IS NULL OR email LIKE @LikeSearch OR full_name LIKE @LikeSearch)";
        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM users {where};",
            new { Search = (string?)null, LikeSearch = $"%{q.Search ?? ""}%" });

        var items = (await conn.QueryAsync<UserSummaryRow>(
            $@"SELECT id AS Id, email AS Email, full_name AS FullName,
                      CAST(is_active AS INTEGER) AS IsActive,
                      created_at AS CreatedAt, row_version AS RowVersion
                 FROM users {where}
                ORDER BY created_at DESC
                LIMIT @Limit OFFSET @Offset;",
            new { Search = q.Search is null ? (string?)null : q.Search, LikeSearch = $"%{q.Search ?? ""}%",
                  Limit = q.PageSizeValue, Offset = q.Offset }))
            .Select(r => new UserSummary { Id = r.Id, Email = r.Email, FullName = r.FullName, IsActive = r.IsActive, CreatedAt = r.CreatedAt, RowVersion = r.RowVersion })
            .ToList();

        // Inject roles per user via single round-trip GROUP_CONCAT
        if (items.Count > 0)
        {
            var ids = items.Select(i => i.Id).ToArray();
            var roles = (await conn.QueryAsync<(string UserId, string Role)>(
                @"SELECT ur.user_id AS UserId, r.name AS Role
                    FROM user_roles ur JOIN roles r ON r.id = ur.role_id
                   WHERE ur.user_id IN @Ids;", new { Ids = ids })).ToList();
            items = items.Select(u =>
            {
                var r = roles.Where(x => x.UserId == u.Id).Select(x => x.Role).ToList();
                return u with { Roles = r };
            }).ToList();
        }

        return new Paged<UserSummary>(items, total, Math.Max(q.PageValue, 1), q.PageSizeValue, (int)Math.Ceiling((double)total / q.PageSizeValue));
    }

    public async Task<UserDetail> GetAsync(string id, CancellationToken ct)
    {
        using var conn = _f.Create();
        var auth = await UserRepository.GetAuthViewByIdAsync(conn, id) ?? throw new NotFoundException("User", id);
        return new UserDetail(auth.User.Id, auth.User.Email, auth.User.FullName, auth.User.IsActive,
            auth.RoleIds, auth.RoleNames, auth.User.CreatedAt, auth.User.UpdatedAt, auth.User.RowVersion);
    }

    public async Task<string> CreateAsync(CreateUserRequest req, string actorId, CancellationToken ct)
    {
        using var conn = _f.Create();
        using var tx = conn.BeginTransaction();
        var exists = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users WHERE email = @Email;", new { req.Email }, tx);
        if (exists > 0) throw new ConflictException($"Email '{req.Email}' is already taken.");
        var id = $"user:{Guid.NewGuid():N}";
        await conn.ExecuteAsync("""
            INSERT INTO users (id, email, full_name, password_hash, is_active, created_at, created_by, is_deleted, row_version)
            VALUES (@Id, @Email, @FullName, @Hash, 1, @Now, @Actor, 0, @RowVersion);
            """, new { Id = id, req.Email, req.FullName, Hash = PasswordHasher.Hash(req.Password), Now = DateTime.UtcNow.ToString("O"), Actor = actorId, RowVersion = Guid.NewGuid().ToString("N") }, tx);
        await UpsertRolesAsync(conn, tx, id, req.RoleIds);
        await AuditLog.WriteAsync(conn, tx, actorId, "user.create", "users", id, after: new { req.Email, req.FullName, Roles = req.RoleIds });
        tx.Commit();
        return id;
    }

    public async Task UpdateAsync(string id, UpdateUserRequest req, string actorId, CancellationToken ct)
    {
        using var conn = _f.Create();
        using var tx = conn.BeginTransaction();
        var row = await conn.QuerySingleOrDefaultAsync<UserRow>("SELECT " + UserRepository.SelectColumns + " FROM users WHERE id = @Id AND is_deleted = 0;", new { Id = id }, tx)
                   ?? throw new NotFoundException("User", id);
        if (row.RowVersion != req.RowVersion) throw new ConflictException("User was modified by another user. Reload and try again.");
        var newVersion = Guid.NewGuid().ToString("N");
        await conn.ExecuteAsync("""
            UPDATE users SET full_name = @FullName, is_active = @IsActive, updated_at = @Now, updated_by = @Actor, row_version = @NewVersion
             WHERE id = @Id AND row_version = @RowVersion;
            """, new { FullName = req.FullName ?? row.FullName, IsActive = req.IsActive ?? row.IsActive, Now = DateTime.UtcNow.ToString("O"), Actor = actorId, Id = id, RowVersion = req.RowVersion, NewVersion = newVersion }, tx);
        await AuditLog.WriteAsync(conn, tx, actorId, "user.update", "users", id, before: new { row.FullName, row.IsActive }, after: new { FullName = req.FullName ?? row.FullName, IsActive = req.IsActive ?? row.IsActive });
        tx.Commit();
    }

    public async Task AssignRolesAsync(string id, AssignRolesRequest req, string actorId, CancellationToken ct)
    {
        using var conn = _f.Create();
        using var tx = conn.BeginTransaction();
        _ = await UserRepository.GetAuthViewByIdAsync(conn, id) ?? throw new NotFoundException("User", id);
        await conn.ExecuteAsync("DELETE FROM user_roles WHERE user_id = @Id;", new { Id = id }, tx);
        foreach (var roleId in req.RoleIds.Distinct())
        {
            var exists = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM roles WHERE id = @Id;", new { Id = roleId }, tx);
            if (exists == 0) throw new NotFoundException("Role", roleId);
            await conn.ExecuteAsync("INSERT OR IGNORE INTO user_roles (user_id, role_id) VALUES (@U, @R);", new { U = id, R = roleId }, tx);
        }
        await AuditLog.WriteAsync(conn, tx, actorId, "user.assign_roles", "users", id, after: new { Roles = req.RoleIds });
        tx.Commit();
    }

    public async Task DeleteAsync(string id, string actorId, CancellationToken ct)
    {
        using var conn = _f.Create();
        using var tx = conn.BeginTransaction();
        var row = await conn.QuerySingleOrDefaultAsync<UserRow>("SELECT " + UserRepository.SelectColumns + " FROM users WHERE id = @Id AND is_deleted = 0;", new { Id = id }, tx)
                   ?? throw new NotFoundException("User", id);
        if (id == actorId) throw new DomainException("You cannot delete your own account.");
        await conn.ExecuteAsync("UPDATE users SET is_deleted = 1, updated_at = @Now, updated_by = @Actor WHERE id = @Id;",
            new { Now = DateTime.UtcNow.ToString("O"), Actor = actorId, Id = id }, tx);
        await AuditLog.WriteAsync(conn, tx, actorId, "user.delete", "users", id, before: new { row.Email, row.FullName });
        tx.Commit();
    }

    private static async Task UpsertRolesAsync(IDbConnection conn, IDbTransaction tx, string userId, IReadOnlyCollection<string> roleIds)
    {
        await conn.ExecuteAsync("DELETE FROM user_roles WHERE user_id = @U;", new { U = userId }, tx);
        foreach (var roleId in roleIds.Distinct())
            await conn.ExecuteAsync("INSERT OR IGNORE INTO user_roles (user_id, role_id) VALUES (@U, @R);", new { U = userId, R = roleId }, tx);
    }
}
