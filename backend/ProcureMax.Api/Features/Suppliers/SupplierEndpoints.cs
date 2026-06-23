using Microsoft.AspNetCore.Http.HttpResults;
using ProcureMax.Core.Authorization;
using ProcureMax.Core.Common;
using ProcureMax.Features.Auth;

namespace ProcureMax.Features.Suppliers;

public static class SupplierEndpoints
{
    public static IEndpointRouteBuilder MapSupplierEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/suppliers").WithTags("Suppliers");

        // Listing and detail GETs are gated to supplier.view — requestors & approvers need
        // read access to pick a vendor in a PR. All mutations require supplier.manage.
        grp.MapGet("/", ListAsync).HasPermission(Permissions.SupplierView);
        grp.MapGet("/{id}", GetAsync).HasPermission(Permissions.SupplierView);
        grp.MapPost("/", CreateAsync).AddRequestValidation<CreateSupplierRequest>().HasPermission(Permissions.SupplierManage);
        grp.MapPut("/{id}", UpdateAsync).AddRequestValidation<UpdateSupplierRequest>().HasPermission(Permissions.SupplierManage);
        grp.MapDelete("/{id}", DeleteAsync).HasPermission(Permissions.SupplierManage);

        return app;
    }

    private static async Task<Ok<Paged<SupplierSummary>>> ListAsync(
        [AsParameters] PageQuery q, ISupplierService svc, CancellationToken ct)
        => TypedResults.Ok(await svc.ListAsync(q, ct));

    private static async Task<Ok<SupplierDetail>> GetAsync(
        string id, ISupplierService svc, CancellationToken ct)
        => TypedResults.Ok(await svc.GetAsync(id, ct));

    private static async Task<Created<IdResponse>> CreateAsync(
        CreateSupplierRequest req, ISupplierService svc, ICurrentUser user, CancellationToken ct)
    {
        var id = await svc.CreateAsync(req, user.UserId!, ct);
        return TypedResults.Created($"/api/suppliers/{id}", new IdResponse(id));
    }

    private static async Task<NoContent> UpdateAsync(
        string id, UpdateSupplierRequest req, ISupplierService svc, ICurrentUser user, CancellationToken ct)
    {
        await svc.UpdateAsync(id, req, user.UserId!, ct);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> DeleteAsync(
        string id, [AsParameters] DeleteRequest req, ISupplierService svc, ICurrentUser user, CancellationToken ct)
    {
        await svc.DeleteAsync(id, user.UserId!, req.RowVersion, ct);
        return TypedResults.NoContent();
    }

    public sealed class DeleteRequest
    {
        public string RowVersion { get; set; } = "";
    }
}
