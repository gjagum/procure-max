using Microsoft.AspNetCore.Http.HttpResults;
using ProcureMax.Core.Authorization;
using ProcureMax.Core.Common;
using ProcureMax.Features.Auth;

namespace ProcureMax.Features.Units;

public static class UnitEndpoints
{
    public static IEndpointRouteBuilder MapUnitEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/units").WithTags("Units");

        grp.MapGet("/", ListAsync).HasPermission(Permissions.UnitManage);
        grp.MapGet("/{id}", GetAsync).HasPermission(Permissions.UnitManage);
        grp.MapPost("/", CreateAsync).AddRequestValidation<CreateUnitRequest>().HasPermission(Permissions.UnitManage);
        grp.MapPut("/{id}", UpdateAsync).AddRequestValidation<UpdateUnitRequest>().HasPermission(Permissions.UnitManage);
        grp.MapDelete("/{id}", DeleteAsync).HasPermission(Permissions.UnitManage);

        return app;
    }

    private static async Task<Ok<Paged<UnitSummary>>> ListAsync(
        [AsParameters] PageQuery q, IUnitService svc, CancellationToken ct)
        => TypedResults.Ok(await svc.ListAsync(q, ct));

    private static async Task<Ok<UnitDetail>> GetAsync(
        string id, IUnitService svc, CancellationToken ct)
        => TypedResults.Ok(await svc.GetAsync(id, ct));

    private static async Task<Created<IdResponse>> CreateAsync(
        CreateUnitRequest req, IUnitService svc, ICurrentUser user, CancellationToken ct)
    {
        var id = await svc.CreateAsync(req, user.UserId!, ct);
        return TypedResults.Created($"/api/units/{id}", new IdResponse(id));
    }

    private static async Task<NoContent> UpdateAsync(
        string id, UpdateUnitRequest req, IUnitService svc, ICurrentUser user, CancellationToken ct)
    {
        await svc.UpdateAsync(id, req, user.UserId!, ct);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> DeleteAsync(
        string id, [AsParameters] DeleteRequest req, IUnitService svc, ICurrentUser user, CancellationToken ct)
    {
        await svc.DeleteAsync(id, user.UserId!, req.RowVersion, ct);
        return TypedResults.NoContent();
    }

    public sealed class DeleteRequest
    {
        public string RowVersion { get; set; } = "";
    }
}
