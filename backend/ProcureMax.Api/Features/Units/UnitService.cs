using Dapper;
using ProcureMax.Core;

namespace ProcureMax.Features.Units;

public interface IUnitService
{
    Task<Paged<UnitSummary>> ListAsync(PageQuery q, CancellationToken ct);
    Task<UnitDetail> GetAsync(string id, CancellationToken ct);
    Task<string> CreateAsync(CreateUnitRequest req, string actorId, CancellationToken ct);
    Task<string> UpdateAsync(string id, UpdateUnitRequest req, string actorId, CancellationToken ct);
    Task DeleteAsync(string id, string actorId, string expectedVersion, CancellationToken ct);
}

internal sealed class UnitService(
    IUnitRepository repo,
    IDbConnectionFactory factory) : IUnitService
{
    private readonly IUnitRepository _repo = repo;
    private readonly IDbConnectionFactory _factory = factory;

    public async Task<Paged<UnitSummary>> ListAsync(PageQuery q, CancellationToken ct)
    {
        var items = (await _repo.ListAsync(q.Search, q.PageSizeValue, q.Offset, ct))
            .Select(r => new UnitSummary { Id = r.Id, Code = r.Code, Name = r.Name, IsActive = r.IsActive })
            .ToList();
        var total = await _repo.CountAsync(q.Search, ct);
        return new Paged<UnitSummary>(items, total, q.PageValue, q.PageSizeValue, (int)Math.Ceiling((double)total / q.PageSizeValue));
    }

    public async Task<UnitDetail> GetAsync(string id, CancellationToken ct)
    {
        var row = await _repo.FindByIdAsync(id, ct) ?? throw new NotFoundException("Unit", id);
        return ToDetail(row);
    }

    public async Task<string> CreateAsync(CreateUnitRequest req, string actorId, CancellationToken ct)
    {
        if (await _repo.FindByCodeAsync(req.Code, ct) is not null)
            throw new ConflictException($"Unit code '{req.Code}' is already in use.");
        var id = await _repo.InsertAsync(req.Code, req.Name, actorId, ct);

        using var conn = _factory.Create();
        using var tx = conn.BeginTransaction();
        await AuditLog.WriteAsync(conn, tx, actorId, "unit.create", "units", id,
            after: new { Code = req.Code, Name = req.Name });
        tx.Commit();
        return id;
    }

    public async Task<string> UpdateAsync(string id, UpdateUnitRequest req, string actorId, CancellationToken ct)
    {
        var row = await _repo.FindByIdAsync(id, ct) ?? throw new NotFoundException("Unit", id);
        if (row.RowVersion != req.RowVersion)
            throw new ConflictException("Unit was modified by another user. Reload and try again.");

        var newVersion = Guid.NewGuid().ToString("N");
        var affected = await _repo.UpdateAsync(row, req.Name, req.IsActive, actorId, newVersion, ct);
        if (affected == 0)
            throw new ConflictException("Unit was modified by another user. Reload and try again.");

        using var conn = _factory.Create();
        using var tx = conn.BeginTransaction();
        await AuditLog.WriteAsync(conn, tx, actorId, "unit.update", "units", id,
            after: new { req.Name, req.IsActive });
        tx.Commit();
        return newVersion;
    }

    public async Task DeleteAsync(string id, string actorId, string expectedVersion, CancellationToken ct)
    {
        var row = await _repo.FindByIdAsync(id, ct) ?? throw new NotFoundException("Unit", id);
        if (row.RowVersion != expectedVersion)
            throw new ConflictException("Unit was modified by another user. Reload and try again.");

        var newVersion = Guid.NewGuid().ToString("N");
        var affected = await _repo.SoftDeleteAsync(id, actorId, expectedVersion, newVersion, ct);
        if (affected == 0)
            throw new ConflictException("Unit was modified by another user. Reload and try again.");

        using var conn = _factory.Create();
        using var tx = conn.BeginTransaction();
        await AuditLog.WriteAsync(conn, tx, actorId, "unit.delete", "units", id);
        tx.Commit();
    }

    private static UnitDetail ToDetail(UnitRow r) => new()
    {
        Id = r.Id,
        Code = r.Code,
        Name = r.Name,
        IsActive = r.IsActive,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
        RowVersion = r.RowVersion
    };
}
