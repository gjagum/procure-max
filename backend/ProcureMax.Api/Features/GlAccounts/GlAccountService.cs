using Dapper;
using ProcureMax.Core;

namespace ProcureMax.Features.GlAccounts;

public interface IGlAccountService
{
    Task<Paged<GlAccountSummary>> ListAsync(PageQuery q, CancellationToken ct);
    Task<GlAccountDetail> GetAsync(string id, CancellationToken ct);
    Task<string> CreateAsync(CreateGlAccountRequest req, string actorId, CancellationToken ct);
    Task<string> UpdateAsync(string id, UpdateGlAccountRequest req, string actorId, CancellationToken ct);
    Task DeleteAsync(string id, string actorId, string expectedVersion, CancellationToken ct);
}

internal sealed class GlAccountService(
    IGlAccountRepository repo,
    IDbConnectionFactory factory) : IGlAccountService
{
    private readonly IGlAccountRepository _repo = repo;
    private readonly IDbConnectionFactory _factory = factory;

    public async Task<Paged<GlAccountSummary>> ListAsync(PageQuery q, CancellationToken ct)
    {
        var items = (await _repo.ListAsync(q.Search, q.PageSizeValue, q.Offset, ct))
            .Select(r => new GlAccountSummary { Id = r.Id, Code = r.Code, Name = r.Name, IsActive = r.IsActive })
            .ToList();
        var total = await _repo.CountAsync(q.Search, ct);
        return new Paged<GlAccountSummary>(items, total, q.PageValue, q.PageSizeValue, (int)Math.Ceiling((double)total / q.PageSizeValue));
    }

    public async Task<GlAccountDetail> GetAsync(string id, CancellationToken ct)
    {
        var row = await _repo.FindByIdAsync(id, ct) ?? throw new NotFoundException("GL account", id);
        return ToDetail(row);
    }

    public async Task<string> CreateAsync(CreateGlAccountRequest req, string actorId, CancellationToken ct)
    {
        if (await _repo.FindByCodeAsync(req.Code, ct) is not null)
            throw new ConflictException($"GL account code '{req.Code}' is already in use.");
        var id = await _repo.InsertAsync(req.Code, req.Name, actorId, ct);

        using var conn = _factory.Create();
        using var tx = conn.BeginTransaction();
        await AuditLog.WriteAsync(conn, tx, actorId, "glaccount.create", "gl_accounts", id,
            after: new { Code = req.Code, Name = req.Name });
        tx.Commit();
        return id;
    }

    public async Task<string> UpdateAsync(string id, UpdateGlAccountRequest req, string actorId, CancellationToken ct)
    {
        var row = await _repo.FindByIdAsync(id, ct) ?? throw new NotFoundException("GL account", id);
        if (row.RowVersion != req.RowVersion)
            throw new ConflictException("GL account was modified by another user. Reload and try again.");

        var newVersion = Guid.NewGuid().ToString("N");
        var affected = await _repo.UpdateAsync(row, req.Name, req.IsActive, actorId, newVersion, ct);
        if (affected == 0)
            throw new ConflictException("GL account was modified by another user. Reload and try again.");

        using var conn = _factory.Create();
        using var tx = conn.BeginTransaction();
        await AuditLog.WriteAsync(conn, tx, actorId, "glaccount.update", "gl_accounts", id,
            after: new { req.Name, req.IsActive });
        tx.Commit();
        return newVersion;
    }

    public async Task DeleteAsync(string id, string actorId, string expectedVersion, CancellationToken ct)
    {
        var row = await _repo.FindByIdAsync(id, ct) ?? throw new NotFoundException("GL account", id);
        if (row.RowVersion != expectedVersion)
            throw new ConflictException("GL account was modified by another user. Reload and try again.");

        var newVersion = Guid.NewGuid().ToString("N");
        var affected = await _repo.SoftDeleteAsync(id, actorId, expectedVersion, newVersion, ct);
        if (affected == 0)
            throw new ConflictException("GL account was modified by another user. Reload and try again.");

        using var conn = _factory.Create();
        using var tx = conn.BeginTransaction();
        await AuditLog.WriteAsync(conn, tx, actorId, "glaccount.delete", "gl_accounts", id);
        tx.Commit();
    }

    private static GlAccountDetail ToDetail(GlAccountRow r) => new()
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
