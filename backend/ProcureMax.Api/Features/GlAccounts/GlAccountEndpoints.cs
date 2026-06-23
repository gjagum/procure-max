using Microsoft.AspNetCore.Http.HttpResults;
using ProcureMax.Core.Authorization;
using ProcureMax.Core.Common;
using ProcureMax.Features.Auth;

namespace ProcureMax.Features.GlAccounts;

public static class GlAccountEndpoints
{
    public static IEndpointRouteBuilder MapGlAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/gl-accounts").WithTags("GL Accounts");

        grp.MapGet("/", ListAsync).HasPermission(Permissions.GlAccountManage);
        grp.MapGet("/{id}", GetAsync).HasPermission(Permissions.GlAccountManage);
        grp.MapPost("/", CreateAsync).AddRequestValidation<CreateGlAccountRequest>().HasPermission(Permissions.GlAccountManage);
        grp.MapPut("/{id}", UpdateAsync).AddRequestValidation<UpdateGlAccountRequest>().HasPermission(Permissions.GlAccountManage);
        grp.MapDelete("/{id}", DeleteAsync).HasPermission(Permissions.GlAccountManage);

        return app;
    }

    private static async Task<Ok<Paged<GlAccountSummary>>> ListAsync(
        [AsParameters] PageQuery q, IGlAccountService svc, CancellationToken ct)
        => TypedResults.Ok(await svc.ListAsync(q, ct));

    private static async Task<Ok<GlAccountDetail>> GetAsync(
        string id, IGlAccountService svc, CancellationToken ct)
        => TypedResults.Ok(await svc.GetAsync(id, ct));

    private static async Task<Created<IdResponse>> CreateAsync(
        CreateGlAccountRequest req, IGlAccountService svc, ICurrentUser user, CancellationToken ct)
    {
        var id = await svc.CreateAsync(req, user.UserId!, ct);
        return TypedResults.Created($"/api/gl-accounts/{id}", new IdResponse(id));
    }

    private static async Task<NoContent> UpdateAsync(
        string id, UpdateGlAccountRequest req, IGlAccountService svc, ICurrentUser user, CancellationToken ct)
    {
        await svc.UpdateAsync(id, req, user.UserId!, ct);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> DeleteAsync(
        string id, [AsParameters] DeleteRequest req, IGlAccountService svc, ICurrentUser user, CancellationToken ct)
    {
        await svc.DeleteAsync(id, user.UserId!, req.RowVersion, ct);
        return TypedResults.NoContent();
    }

    public sealed class DeleteRequest
    {
        public string RowVersion { get; set; } = "";
    }
}
