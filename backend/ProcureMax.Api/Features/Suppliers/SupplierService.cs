using Dapper;
using ProcureMax.Core;

namespace ProcureMax.Features.Suppliers;

public interface ISupplierService
{
    Task<Paged<SupplierSummary>> ListAsync(PageQuery q, CancellationToken ct);
    Task<SupplierDetail> GetAsync(string id, CancellationToken ct);
    Task<string> CreateAsync(CreateSupplierRequest req, string actorId, CancellationToken ct);
    Task<string> UpdateAsync(string id, UpdateSupplierRequest req, string actorId, CancellationToken ct);
    Task DeleteAsync(string id, string actorId, string expectedVersion, CancellationToken ct);
}

internal sealed class SupplierService(
    ISupplierRepository repo,
    IDbConnectionFactory factory) : ISupplierService
{
    private readonly ISupplierRepository _repo = repo;
    private readonly IDbConnectionFactory _factory = factory;

    public async Task<Paged<SupplierSummary>> ListAsync(PageQuery q, CancellationToken ct)
    {
        var items = (await _repo.ListAsync(q.Search, q.PageSizeValue, q.Offset, ct))
            .Select(ToSummary)
            .ToList();
        var total = await _repo.CountAsync(q.Search, ct);
        return new Paged<SupplierSummary>(items, total, q.PageValue, q.PageSizeValue, (int)Math.Ceiling((double)total / q.PageSizeValue));
    }

    public async Task<SupplierDetail> GetAsync(string id, CancellationToken ct)
    {
        var row = await _repo.FindByIdAsync(id, ct) ?? throw new NotFoundException("Supplier", id);
        return ToDetail(row);
    }

    public async Task<string> CreateAsync(CreateSupplierRequest req, string actorId, CancellationToken ct)
    {
        if (await _repo.FindByCodeAsync(req.Code, ct) is not null)
            throw new ConflictException($"Supplier code '{req.Code}' is already in use.");
        var id = await _repo.InsertAsync(req, actorId, ct);

        using var conn = _factory.Create();
        using var tx = conn.BeginTransaction();
        await AuditLog.WriteAsync(conn, tx, actorId, "supplier.create", "suppliers", id, after: new { req.Code, req.Name });
        tx.Commit();
        return id;
    }

    public async Task<string> UpdateAsync(string id, UpdateSupplierRequest req, string actorId, CancellationToken ct)
    {
        var row = await _repo.FindByIdAsync(id, ct) ?? throw new NotFoundException("Supplier", id);
        if (row.RowVersion != req.RowVersion)
            throw new ConflictException("Supplier was modified by another user. Reload and try again.");

        var newVersion = Guid.NewGuid().ToString("N");
        var affected = await _repo.UpdateAsync(id, req, actorId, row.RowVersion, newVersion, ct);
        if (affected == 0)
            throw new ConflictException("Supplier was modified by another user. Reload and try again.");

        using var conn = _factory.Create();
        using var tx = conn.BeginTransaction();
        await AuditLog.WriteAsync(conn, tx, actorId, "supplier.update", "suppliers", id,
            after: new { req.Name, req.IsActive, req.IsBlocked });
        tx.Commit();
        return newVersion;
    }

    public async Task DeleteAsync(string id, string actorId, string expectedVersion, CancellationToken ct)
    {
        var row = await _repo.FindByIdAsync(id, ct) ?? throw new NotFoundException("Supplier", id);
        if (row.RowVersion != expectedVersion)
            throw new ConflictException("Supplier was modified by another user. Reload and try again.");

        var newVersion = Guid.NewGuid().ToString("N");
        var affected = await _repo.SoftDeleteAsync(id, actorId, expectedVersion, newVersion, ct);
        if (affected == 0)
            throw new ConflictException("Supplier was modified by another user. Reload and try again.");

        using var conn = _factory.Create();
        using var tx = conn.BeginTransaction();
        await AuditLog.WriteAsync(conn, tx, actorId, "supplier.delete", "suppliers", id);
        tx.Commit();
    }

    private static SupplierSummary ToSummary(SupplierRow r) => new()
    {
        Id = r.Id,
        Code = r.Code,
        Name = r.Name,
        LegalName = r.LegalName,
        IsActive = r.IsActive,
        IsBlocked = r.IsBlocked,
        Currency = r.Currency
    };

    private static SupplierDetail ToDetail(SupplierRow r) => new()
    {
        Id = r.Id,
        Code = r.Code,
        Name = r.Name,
        LegalName = r.LegalName,
        TaxId = r.TaxId,
        ContactName = r.ContactName,
        Email = r.Email,
        Phone = r.Phone,
        AddressLine1 = r.AddressLine1,
        AddressLine2 = r.AddressLine2,
        City = r.City,
        State = r.State,
        PostalCode = r.PostalCode,
        Country = r.Country,
        PaymentTerms = r.PaymentTerms,
        Currency = r.Currency,
        IsActive = r.IsActive,
        IsBlocked = r.IsBlocked,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
        RowVersion = r.RowVersion
    };
}
