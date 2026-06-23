using Dapper;
using ProcureMax.Core.Authorization;
using ProcureMax.Core.Auth;

namespace ProcureMax.Core;

// Seeds the permission catalog, system roles, and a bootstrap admin user.
// Idempotent — re-running only inserts missing rows.
public class Seeder
{
    private readonly IDbConnectionFactory _factory;
    private readonly IConfiguration _config;
    private readonly ILogger<Seeder> _logger;

    public Seeder(IDbConnectionFactory factory, IConfiguration config, ILogger<Seeder> logger)
    {
        _factory = factory;
        _config = config;
        _logger = logger;
    }

    public void Seed()
    {
        using var conn = _factory.Create();
        SeedPermissions(conn);
        SeedSystemRoles(conn);
        BindRolePermissions(conn);
        SeedAdminUser(conn);
        SeedProcurementReference(conn);
    }

    private void SeedPermissions(IDbConnection conn)
    {
        var now = DateTime.UtcNow.ToString("O");
        foreach (var (area, action) in Permissions.All)
        {
            var id = $"perm:{area}:{action}";
            conn.Execute("""
                INSERT OR IGNORE INTO permissions (id, area, action) VALUES (@Id, @Area, @Action);
                """, new { Id = id, Area = area, Action = action });
        }
        _logger.LogInformation("Seeded {Count} permissions", Permissions.All.Length);
    }

    private void SeedSystemRoles(IDbConnection conn)
    {
        var roles = new (string id, string name, string desc)[]
        {
            ("role:admin",     SystemRoles.Admin,     "Full system access"),
            ("role:requestor", SystemRoles.Requestor, "Creates and tracks own requisitions"),
            ("role:approver",  SystemRoles.Approver,  "Approves requisitions and POs"),
            ("role:buyer",     SystemRoles.Buyer,     "Creates purchase orders from approved requisitions"),
            ("role:apclerk",   SystemRoles.ApClerk,   "Manages invoices and three-way match"),
        };
        foreach (var r in roles)
        {
            conn.Execute("""
                INSERT OR IGNORE INTO roles (id, name, description, created_at, is_system)
                VALUES (@Id, @Name, @Desc, @Now, 1);
                """, new { Id = r.id, Name = r.name, Desc = r.desc, Now = DateTime.UtcNow.ToString("O") });
        }
    }

    private void BindRolePermissions(IDbConnection conn)
    {
        var binding = new Dictionary<string, string[]>
        {
            ["role:admin"] = Permissions.All.Select(p => Permissions.FullName(p.area, p.action)).ToArray(),
            ["role:requestor"] = [Permissions.PrCreate, Permissions.PrView, Permissions.DashboardView, Permissions.ItemView, Permissions.SupplierView],
            ["role:approver"] = [Permissions.PrView, Permissions.PrApproveOwn, Permissions.PrApproveAll, Permissions.PoView, Permissions.PoApprove, Permissions.DashboardView],
            ["role:buyer"] = [Permissions.PoCreate, Permissions.PoView, Permissions.PoIssue, Permissions.PrView, Permissions.SupplierView, Permissions.ItemView, Permissions.DashboardView],
            ["role:apclerk"] = [Permissions.InvoiceManage, Permissions.InvoiceView, Permissions.InvoiceApprove, Permissions.GrView, Permissions.PoView, Permissions.DashboardView],
        };

        foreach (var (roleId, perms) in binding)
        {
            foreach (var full in perms)
            {
                var parts = full.Split('.', 2);
                var area = parts[0];
                var action = parts[1];
                var permId = $"perm:{area}:{action}";
                conn.Execute("""
                    INSERT OR IGNORE INTO role_permissions (role_id, permission_id)
                    VALUES (@RoleId, @PermId);
                    """, new { RoleId = roleId, PermId = permId });
            }
        }
        _logger.LogInformation("Bound permissions to system roles");
    }

    private void SeedAdminUser(IDbConnection conn)
    {
        var email = _config["SeedAdmin:Email"] ?? "admin@procuremax.local";
        var password = _config["SeedAdmin:Password"] ?? "Admin#123";
        var name = _config["SeedAdmin:Name"] ?? "System Administrator";

        var existing = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM users WHERE email = @Email;", new { Email = email });
        if (existing > 0) return;

        var id = "user:admin";
        conn.Execute("""
            INSERT INTO users (id, email, full_name, password_hash, is_active, created_at, created_by, is_deleted, row_version)
            VALUES (@Id, @Email, @Name, @Hash, 1, @Now, 'system', 0, @RowVersion);
            """, new
        {
            Id = id,
            Email = email,
            Name = name,
            Hash = PasswordHasher.Hash(password),
            Now = DateTime.UtcNow.ToString("O"),
            RowVersion = Guid.NewGuid().ToString("N"),
        });
        conn.Execute("""
            INSERT OR IGNORE INTO user_roles (user_id, role_id) VALUES (@UserId, 'role:admin');
            """, new { UserId = id });

        _logger.LogInformation("Seeded admin user {Email}", email);
    }

    // Seed a minimal set of reference rows so a fresh install can immediately create PRs.
    // Idempotent: existing rows are left intact (INSERT OR IGNORE).
    private void SeedProcurementReference(IDbConnection conn)
    {
        var now = DateTime.UtcNow.ToString("O");
        var actor = "system";

        // Units — common defaults a procurement department always needs.
        var units = new (string code, string name)[]
        {
            ("EA",   "Each"),
            ("KG",   "Kilogram"),
            ("G",    "Gram"),
            ("L",    "Liter"),
            ("M",    "Meter"),
            ("BOX",  "Box"),
            ("PK",   "Pack"),
            ("HR",   "Hour"),
            ("DAY",  "Day"),
        };
        foreach (var u in units)
        {
            conn.Execute("""
                INSERT OR IGNORE INTO units (id, code, name, is_active, created_at, created_by, row_version)
                VALUES (@Id, @Code, @Name, 1, @Now, @Actor, @RowVersion);
                """, new { Id = $"unit:{u.code}", Code = u.code, Name = u.name, Now = now, Actor = actor, RowVersion = Guid.NewGuid().ToString("N") });
        }

        // GL accounts — typical operating expense buckets.
        var glAccounts = new (string code, string name)[]
        {
            ("5000-OFFICE",  "Office Supplies"),
            ("5010-IT",      "IT Equipment & Software"),
            ("5020-FACIL",   "Facilities & Maintenance"),
            ("5030-RAW",     "Raw Materials"),
            ("5040-MKTG",    "Marketing & Advertising"),
            ("5050-PROF",    "Professional Services"),
            ("5060-TRAVEL",  "Travel & Entertainment"),
        };
        foreach (var g in glAccounts)
        {
            conn.Execute("""
                INSERT OR IGNORE INTO gl_accounts (id, code, name, is_active, created_at, created_by, row_version)
                VALUES (@Id, @Code, @Name, 1, @Now, @Actor, @RowVersion);
                """, new { Id = $"gl:{g.code}", Code = g.code, Name = g.name, Now = now, Actor = actor, RowVersion = Guid.NewGuid().ToString("N") });
        }

        // Cost centers — generic organizational units.
        var costCenters = new (string code, string name)[]
        {
            ("CC-1000", "Corporate Administration"),
            ("CC-2000", "Operations"),
            ("CC-3000", "Information Technology"),
            ("CC-4000", "Sales & Marketing"),
            ("CC-5000", "Research & Development"),
        };
        foreach (var c in costCenters)
        {
            conn.Execute("""
                INSERT OR IGNORE INTO cost_centers (id, code, name, is_active, created_at, created_by, row_version)
                VALUES (@Id, @Code, @Name, 1, @Now, @Actor, @RowVersion);
                """, new { Id = $"cc:{c.code}", Code = c.code, Name = c.name, Now = now, Actor = actor, RowVersion = Guid.NewGuid().ToString("N") });
        }

        _logger.LogInformation("Seeded procurement reference data (units, gl_accounts, cost_centers)");
    }
}
