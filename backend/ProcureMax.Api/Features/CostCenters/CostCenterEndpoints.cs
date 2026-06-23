using Microsoft.AspNetCore.Http.HttpResults;
using ProcureMax.Core.Authorization;
using ProcureMax.Core.Common;
using ProcureMax.Features.Auth;

namespace ProcureMax.Features.CostCenters;

public static class CostCenterEndpoints
{
    public static IEndpointRouteBuilder MapCostCenterEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/cost-centers").WithTags("Cost Centers");

        // Listing is open to anyone who can manage; detail/get is also gated to manage perm
        // — the master data is sensitive (cost center codes drive approval routing).
        grp.MapGet("/", ListAsync).HasPermission(Permissions.CostCenterManage);
        grp.MapGet("/{id}", GetAsync).HasPermission(Permissions.CostCenterManage);
        grp.MapPost("/", CreateAsync).AddRequestValidation<CreateCostCenterRequest>().HasPermission(Permissions.CostCenterManage);
        grp.MapPut("/{id}", UpdateAsync).AddRequestValidation<UpdateCostCenterRequest>().HasPermission(Permissions.CostCenterManage);
        grp.MapDelete("/{id}", DeleteAsync).HasPermission(Permissions.CostCenterManage);

        return app;
    }

    private static async Task<Ok<Paged<CostCenterSummary>>> ListAsync(
        [AsParameters] PageQuery q, ICostCenterService svc, CancellationToken ct)
        => TypedResults.Ok(await svc.ListAsync(q, ct));

    private static async Task<Ok<CostCenterDetail>> GetAsync(
        string id, ICostCenterService svc, CancellationToken ct)
        => TypedResults.Ok(await svc.GetAsync(id, ct));

    private static async Task<Created<IdResponse>> CreateAsync(
        CreateCostCenterRequest req, ICostCenterService svc, ICurrentUser user, CancellationToken ct)
    {
        var id = await svc.CreateAsync(req, user.UserId!, ct);
        return TypedResults.Created($"/api/cost-centers/{id}", new IdResponse(id));
    }

    private static async Task<NoContent> UpdateAsync(
        string id, UpdateCostCenterRequest req, ICostCenterService svc, ICurrentUser user, CancellationToken ct)
    {
        await svc.UpdateAsync(id, req, user.UserId!, ct);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> DeleteAsync(
        string id, [AsParameters] DeleteRequest req, ICostCenterService svc, ICurrentUser user, CancellationToken ct)
    {
        await svc.DeleteAsync(id, user.UserId!, req.RowVersion, ct);
        return TypedResults.NoContent();
    }

    // DELETE carries row_version for optimistic concurrency through query string.
    public sealed class DeleteRequest
    {
        public string RowVersion { get; set; } = "";
    }
}
