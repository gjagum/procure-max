using Microsoft.AspNetCore.Http.HttpResults;
using ProcureMax.Core;
using ProcureMax.Core.Authorization;
using ProcureMax.Core.Common;
using ProcureMax.Features.Auth;

namespace ProcureMax.Features.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/users").WithTags("Users");

        grp.MapGet("/", ListAsync).HasPermission(Permissions.UsersManage);
        grp.MapGet("/{id}", GetAsync).HasPermission(Permissions.UsersManage);
        grp.MapPost("/", CreateAsync).AddRequestValidation<CreateUserRequest>().HasPermission(Permissions.UsersManage);
        grp.MapPut("/{id}", UpdateAsync).AddRequestValidation<UpdateUserRequest>().HasPermission(Permissions.UsersManage);
        grp.MapPut("/{id}/roles", (string id, AssignRolesRequest req, IUserService svc, ICurrentUser user, CancellationToken ct) => svc.AssignRolesAsync(id, req, user.UserId!, ct))
            .HasPermission(Permissions.UsersManage);
        grp.MapDelete("/{id}", DeleteAsync).HasPermission(Permissions.UsersManage);

        return app;
    }

    private static async Task<Ok<Paged<UserSummary>>> ListAsync(
        [AsParameters] PageQuery q, IUserService svc, CancellationToken ct)
        => TypedResults.Ok(await svc.ListAsync(q, ct));

    private static async Task<Ok<UserDetail>> GetAsync(string id, IUserService svc, CancellationToken ct)
        => TypedResults.Ok(await svc.GetAsync(id, ct));

    private static async Task<Created<IdResponse>> CreateAsync(
        CreateUserRequest req, IUserService svc, ICurrentUser user, CancellationToken ct)
    {
        var id = await svc.CreateAsync(req, user.UserId!, ct);
        return TypedResults.Created($"/api/users/{id}", new IdResponse(id));
    }

    private static async Task<NoContent> UpdateAsync(
        string id, UpdateUserRequest req, IUserService svc, ICurrentUser user, CancellationToken ct)
    {
        await svc.UpdateAsync(id, req, user.UserId!, ct);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> DeleteAsync(string id, IUserService svc, ICurrentUser user, CancellationToken ct)
    {
        await svc.DeleteAsync(id, user.UserId!, ct);
        return TypedResults.NoContent();
    }
}
