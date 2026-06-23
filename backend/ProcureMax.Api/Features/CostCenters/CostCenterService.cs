using Dapper;
using ProcureMax.Core;

namespace ProcureMax.Features.CostCenters;

// Service for the CostCenters slice.
//
// Pattern followed across all master-data slices (also documented at
// /memories/repo/conventions.md):
//   * Endpoints / Dtos / Repository / Service all live in this folder
//   * Reads use a one-shot connection; writes open a transaction
//   * Optimistic concurrency: caller passes RowVersion; server rotates it on success
//   * Domain errors throw DomainException subclasses → mapped to HTTP by GlobalExceptionHandler
//   * AuditLog records every mutating action for traceability
public interface ICostCenterService
{
    Task<Paged<CostCenterSummary>> ListAsync(PageQuery q, CancellationToken ct);
    Task<CostCenterDetail> GetAsync(string id, CancellationToken ct);
    Task<string> CreateAsync(CreateCostCenterRequest req, string actorId, CancellationToken ct);
    Task<string> UpdateAsync(string id, UpdateCostCenterRequest req, string actorId, CancellationToken ct);
    Task DeleteAsync(string id, string actorId, string expectedVersion, CancellationToken ct);
}

internal sealed class CostCenterService(
    ICostCenterRepository repo,
    IDbConnectionFactory factory) : ICostCenterService
{
    private readonly ICostCenterRepository _repo = repo;
    private readonly IDbConnectionFactory _factory = factory;

    public async Task<Paged<CostCenterSummary>> ListAsync(PageQuery q, CancellationToken ct)
    {
        var items = (await _repo.ListAsync(q.Search, q.PageSizeValue, q.Offset, ct))
            .Select(r => new CostCenterSummary { Id = r.Id, Code = r.Code, Name = r.Name, IsActive = r.IsActive })
            .ToList();
        var total = await _repo.CountAsync(q.Search, ct);
        return new Paged<CostCenterSummary>(items, total, q.PageValue, q.PageSizeValue, (int)Math.Ceiling((double)total / q.PageSizeValue));
    }

    public async Task<CostCenterDetail> GetAsync(string id, CancellationToken ct)
    {
        var row = await _repo.FindByIdAsync(id, ct) ?? throw new NotFoundException("Cost center", id);
        return ToDetail(row);
    }

    public async Task<string> CreateAsync(CreateCostCenterRequest req, string actorId, CancellationToken ct)
    {
        // Case-insensitive uniqueness check (the DB UNIQUE is on the upper-cased value).
        if (await _repo.FindByCodeAsync(req.Code, ct) is not null)
            throw new ConflictException($"Cost center code '{req.Code}' is already in use.");

        var id = await _repo.InsertAsync(req.Code, req.Name, actorId, ct);

        using var conn = _factory.Create();
        using var tx = conn.BeginTransaction();
        await AuditLog.WriteAsync(conn, tx, actorId, "costcenter.create", "cost_centers", id,
            after: new { Code = req.Code, Name = req.Name });
        tx.Commit();
        return id;
    }

    public async Task<string> UpdateAsync(string id, UpdateCostCenterRequest req, string actorId, CancellationToken ct)
    {
        var row = await _repo.FindByIdAsync(id, ct) ?? throw new NotFoundException("Cost center", id);
        if (row.RowVersion != req.RowVersion)
            throw new ConflictException("Cost center was modified by another user. Reload and try again.");

        var newVersion = Guid.NewGuid().ToString("N");
        var affected = await _repo.UpdateAsync(row, req.Name, req.IsActive, actorId, newVersion, ct);
        if (affected == 0)
            throw new ConflictException("Cost center was modified by another user. Reload and try again.");

        using var conn = _factory.Create();
        using var tx = conn.BeginTransaction();
        await AuditLog.WriteAsync(conn, tx, actorId, "costcenter.update", "cost_centers", id,
            after: new { req.Name, req.IsActive });
        tx.Commit();
        return newVersion;
    }

    public async Task DeleteAsync(string id, string actorId, string expectedVersion, CancellationToken ct)
    {
        var row = await _repo.FindByIdAsync(id, ct) ?? throw new NotFoundException("Cost center", id);
        if (row.RowVersion != expectedVersion)
            throw new ConflictException("Cost center was modified by another user. Reload and try again.");

        var newVersion = Guid.NewGuid().ToString("N");
        var affected = await _repo.SoftDeleteAsync(id, actorId, expectedVersion, newVersion, ct);
        if (affected == 0)
            throw new ConflictException("Cost center was modified by another user. Reload and try again.");

        using var conn = _factory.Create();
        using var tx = conn.BeginTransaction();
        await AuditLog.WriteAsync(conn, tx, actorId, "costcenter.delete", "cost_centers", id);
        tx.Commit();
    }

    private static CostCenterDetail ToDetail(CostCenterRow r) => new()
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
