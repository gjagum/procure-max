using System.Data;
using Dapper;
using ProcureMax.Core.Authorization;

namespace ProcureMax.Features.Users;

public class UserRow
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsActive { get; set; }
    public string CreatedAt { get; set; } = "";
    public string? UpdatedAt { get; set; }
    public string RowVersion { get; set; } = "";
}

public class RoleRow
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
}

public class PermissionRow
{
    public string Id { get; set; } = "";
    public string Area { get; set; } = "";
    public string Action { get; set; } = "";
}

// Aggregated view used by Auth slice for token issue.
public class UserAuthView
{
    public UserRow User { get; set; } = new();
    public List<string> RoleIds { get; set; } = new();
    public List<string> RoleNames { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}

public static class UserRepository
{
    // Select with explicit PascalCase aliases so Dapper maps snake_case columns onto UserRow properties.
    public const string SelectColumns = """
        id AS Id, email AS Email, full_name AS FullName, password_hash AS PasswordHash,
        is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt,
        is_deleted AS IsDeleted, row_version AS RowVersion
        """;
    public const string SelectByEmail = "SELECT " + SelectColumns + " FROM users WHERE email = @Email AND is_deleted = 0;";
    public const string SelectById = "SELECT " + SelectColumns + " FROM users WHERE id = @Id AND is_deleted = 0;";

    public static async Task<UserAuthView?> GetAuthViewByEmailAsync(IDbConnection conn, string email)
    {
        var user = await conn.QuerySingleOrDefaultAsync<UserRow>(
            SelectByEmail, new { Email = email });
        if (user is null) return null;
        return await BuildAuthViewAsync(conn, user);
    }

    public static async Task<UserAuthView?> GetAuthViewByIdAsync(IDbConnection conn, string userId)
    {
        var user = await conn.QuerySingleOrDefaultAsync<UserRow>(
            SelectById, new { Id = userId });
        if (user is null) return null;
        return await BuildAuthViewAsync(conn, user);
    }

    private static async Task<UserAuthView> BuildAuthViewAsync(IDbConnection conn, UserRow user)
    {
        var roles = (await conn.QueryAsync<(string id, string name)>(
            @"SELECT r.id AS id, r.name AS name FROM roles r
              JOIN user_roles ur ON ur.role_id = r.id
              WHERE ur.user_id = @UserId;", new { UserId = user.Id })).ToList();
        var roleIds = roles.Select(r => r.id).ToList();
        var roleNames = roles.Select(r => r.name).ToList();

        var perms = roleIds.Count == 0
            ? new List<string>()
            : (await conn.QueryAsync<string>(
                @"SELECT DISTINCT p.area || '.' || p.action
                    FROM permissions p
                    JOIN role_permissions rp ON rp.permission_id = p.id
                   WHERE rp.role_id IN @RoleIds;", new { RoleIds = roleIds })).Distinct().ToList();

        return new UserAuthView { User = user, RoleIds = roleIds, RoleNames = roleNames, Permissions = perms };
    }
}
