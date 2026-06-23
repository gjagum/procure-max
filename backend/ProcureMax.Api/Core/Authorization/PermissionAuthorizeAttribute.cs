using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ProcureMax.Core.Authorization;

// Permission-based authorization: any of the listed permissions grants access.
// Usage: [HasPermission("pr.create")] or [HasPermission("pr.create", "pr.approve")]
public class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "perm:";
    public HasPermissionAttribute(params string[] permissions)
    {
        // Comma-joined; policy handler splits and matches any.
        Policy = $"{PolicyPrefix}{string.Join("|", permissions)}";
    }
}

public static class PermissionPolicy
{
    public const string Name = "HasPermission";

    public static void Register(AuthorizationOptions options)
    {
        options.AddPolicy(Name, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new PermissionRequirement(Array.Empty<string>()));
        });
        // Dynamic per-attribute policies handled via a custom policy provider below.
    }
}

public class PermissionRequirement : IAuthorizationRequirement
{
    public string[] Permissions { get; }
    public PermissionRequirement(string[] permissions) => Permissions = permissions;
}

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (requirement.Permissions.Length == 0
            || requirement.Permissions.Any(p => context.User.HasClaim("perm", p)))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}

// Custom IAuthorizationPolicyProvider so [HasPermission("x")] policies are matched at runtime.
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            var perms = policyName[HasPermissionAttribute.PolicyPrefix.Length..]
                        .Split('|', StringSplitOptions.RemoveEmptyEntries);
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(perms))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
        return _fallback.GetPolicyAsync(policyName);
    }
}
