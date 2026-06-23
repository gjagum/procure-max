using System.Security.Claims;

namespace ProcureMax.Core;

// Resolves the current authenticated user from HttpContext claims.
// Inject as Scoped; resolve via ICurrentUserAccessor from anywhere.
public interface ICurrentUser
{
    string? UserId { get; }
    string? Email { get; }
    string? FullName { get; }
    IReadOnlyList<string> Roles { get; }
    IReadOnlyList<string> Permissions { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permission);
    bool IsInRole(string role);
}

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;

    public CurrentUser(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? Principal => _http.HttpContext?.User;

    public string? UserId => Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? Principal?.FindFirst("sub")?.Value;
    public string? Email => Principal?.FindFirst(ClaimTypes.Email)?.Value
                            ?? Principal?.FindFirst("email")?.Value;
    public string? FullName => Principal?.FindFirst("name")?.Value;

    public IReadOnlyList<string> Roles =>
        Principal?.FindAll("role").Select(c => c.Value).ToList() ?? new List<string>();

    public IReadOnlyList<string> Permissions =>
        Principal?.FindAll("perm").Select(c => c.Value).ToList() ?? new List<string>();

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public bool HasPermission(string permission) => Permissions.Contains(permission);
    public bool IsInRole(string role) => Roles.Contains(role);
}
