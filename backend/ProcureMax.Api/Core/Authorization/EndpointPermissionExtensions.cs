using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;

namespace ProcureMax.Core.Authorization;

// Builder extension: appends a permission requirement to a Minimal API route.
// Resolved at runtime by PermissionPolicyProvider into a PermissionRequirement.
public static class EndpointPermissionExtensions
{
    public static TBuilder HasPermission<TBuilder>(this TBuilder builder, params string[] permissions)
        where TBuilder : IEndpointConventionBuilder
    {
        // We use a policy name that PermissionPolicyProvider recognises by prefix.
        var policy = $"{HasPermissionAttribute.PolicyPrefix}{string.Join("|", permissions)}";
        builder.RequireAuthorization(policy);
        return builder;
    }
}
