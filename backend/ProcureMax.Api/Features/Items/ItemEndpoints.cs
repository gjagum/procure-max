using Microsoft.AspNetCore.Http.HttpResults;
using ProcureMax.Core.Authorization;
using ProcureMax.Core.Common;
using ProcureMax.Features.Auth;

namespace ProcureMax.Features.Items;

public static class ItemEndpoints
{
    public static IEndpointRouteBuilder MapItemEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/items").WithTags("Items");

        // Same two-tier permission model as Suppliers: every operating role needs to read the catalog
        // (for PR line lookup) but only catalog admins write to it.
        grp.MapGet("/", ListAsync).HasPermission(Permissions.ItemView);
        grp.MapGet("/{id}", GetAsync).HasPermission(Permissions.ItemView);
        grp.MapPost("/", CreateAsync).AddRequestValidation<CreateItemRequest>().HasPermission(Permissions.ItemManage);
        grp.MapPut("/{id}", UpdateAsync).AddRequestValidation<UpdateItemRequest>().HasPermission(Permissions.ItemManage);
        grp.MapDelete("/{id}", DeleteAsync).HasPermission(Permissions.ItemManage);

        return app;
    }

    private static async Task<Ok<Paged<ItemSummary>>> ListAsync(
        [AsParameters] PageQuery q, IItemService svc, CancellationToken ct)
        => TypedResults.Ok(await svc.ListAsync(q, ct));

    private static async Task<Ok<ItemDetail>> GetAsync(
        string id, IItemService svc, CancellationToken ct)
        => TypedResults.Ok(await svc.GetAsync(id, ct));

    private static async Task<Created<IdResponse>> CreateAsync(
        CreateItemRequest req, IItemService svc, ICurrentUser user, CancellationToken ct)
    {
        var id = await svc.CreateAsync(req, user.UserId!, ct);
        return TypedResults.Created($"/api/items/{id}", new IdResponse(id));
    }

    private static async Task<NoContent> UpdateAsync(
        string id, UpdateItemRequest req, IItemService svc, ICurrentUser user, CancellationToken ct)
    {
        await svc.UpdateAsync(id, req, user.UserId!, ct);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> DeleteAsync(
        string id, [AsParameters] DeleteRequest req, IItemService svc, ICurrentUser user, CancellationToken ct)
    {
        await svc.DeleteAsync(id, user.UserId!, req.RowVersion, ct);
        return TypedResults.NoContent();
    }

    public sealed class DeleteRequest
    {
        public string RowVersion { get; set; } = "";
    }
}
