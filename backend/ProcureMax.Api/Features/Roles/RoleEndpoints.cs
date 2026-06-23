using Microsoft.AspNetCore.Http.HttpResults;
using ProcureMax.Core.Authorization;
using ProcureMax.Core.Common;
using ProcureMax.Features.Auth;

namespace ProcureMax.Features.Roles;

public static class RoleEndpoints
{
    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/roles").WithTags("Roles");
        grp.MapGet("/", ListAsync).HasPermission(Permissions.RolesManage);
        grp.MapGet("/{id}", GetAsync).HasPermission(Permissions.RolesManage);
        grp.MapPost("/", CreateAsync).AddRequestValidation<CreateRoleRequest>().HasPermission(Permissions.RolesManage);
        grp.MapPut("/{id}", UpdateAsync).HasPermission(Permissions.RolesManage);
        grp.MapDelete("/{id}", DeleteAsync).HasPermission(Permissions.RolesManage);
        return app;
    }

    private static async Task<Ok<Paged<RoleSummary>>> ListAsync([AsParameters] PageQuery q, IRoleService svc, CancellationToken ct)
        => TypedResults.Ok(await svc.ListAsync(q, ct));

    private static async Task<Ok<RoleDetail>> GetAsync(string id, IRoleService svc, CancellationToken ct)
        => TypedResults.Ok(await svc.GetAsync(id, ct));

    private static async Task<Created<IdResponse>> CreateAsync(
        CreateRoleRequest req, IRoleService svc, ICurrentUser user, CancellationToken ct)
    {
        var id = await svc.CreateAsync(req, user.UserId!, ct);
        return TypedResults.Created($"/api/roles/{id}", new IdResponse(id));
    }

    private static async Task<NoContent> UpdateAsync(string id, UpdateRoleRequest req, IRoleService svc, ICurrentUser user, CancellationToken ct)
    {
        await svc.UpdateAsync(id, req, user.UserId!, ct);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> DeleteAsync(string id, IRoleService svc, ICurrentUser user, CancellationToken ct)
    {
        await svc.DeleteAsync(id, user.UserId!, ct);
        return TypedResults.NoContent();
    }
}
