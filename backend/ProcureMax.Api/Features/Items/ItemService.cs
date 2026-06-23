using Dapper;
using ProcureMax.Core;

namespace ProcureMax.Features.Items;

public interface IItemService
{
    Task<Paged<ItemSummary>> ListAsync(PageQuery q, CancellationToken ct);
    Task<ItemDetail> GetAsync(string id, CancellationToken ct);
    Task<string> CreateAsync(CreateItemRequest req, string actorId, CancellationToken ct);
    Task<string> UpdateAsync(string id, UpdateItemRequest req, string actorId, CancellationToken ct);
    Task DeleteAsync(string id, string actorId, string expectedVersion, CancellationToken ct);
}

internal sealed class ItemService(
    IItemRepository repo,
    IDbConnectionFactory factory) : IItemService
{
    private readonly IItemRepository _repo = repo;
    private readonly IDbConnectionFactory _factory = factory;

    public async Task<Paged<ItemSummary>> ListAsync(PageQuery q, CancellationToken ct)
    {
        var items = (await _repo.ListAsync(q.Search, q.PageSizeValue, q.Offset, ct))
            .Select(ToSummary)
            .ToList();
        var total = await _repo.CountAsync(q.Search, ct);
        return new Paged<ItemSummary>(items, total, q.PageValue, q.PageSizeValue, (int)Math.Ceiling((double)total / q.PageSizeValue));
    }

    public async Task<ItemDetail> GetAsync(string id, CancellationToken ct)
    {
        var row = await _repo.FindByIdAsync(id, ct) ?? throw new NotFoundException("Item", id);
        return ToDetail(row);
    }

    public async Task<string> CreateAsync(CreateItemRequest req, string actorId, CancellationToken ct)
    {
        // SKU is optional but when provided must be globally unique.
        if (!string.IsNullOrWhiteSpace(req.Sku) && await _repo.FindBySkuAsync(req.Sku, ct) is not null)
            throw new ConflictException($"Item SKU '{req.Sku}' is already in use.");

        // FK integrity: validate that any referenced unit / gl account / supplier actually exists
        // before INSERT so SQLite's unenforced FK layer doesn't silently accept orphan rows.
        await ValidateForeignKeysAsync(req.UnitId, req.GlAccountId, req.DefaultSupplierId, ct);

        var id = await _repo.InsertAsync(req, actorId, ct);

        using var conn = _factory.Create();
        using var tx = conn.BeginTransaction();
        await AuditLog.WriteAsync(conn, tx, actorId, "item.create", "items", id,
            after: new { req.Sku, req.Name, req.Category, req.DefaultPriceMinor, req.DefaultCurrency });
        tx.Commit();
        return id;
    }

    public async Task<string> UpdateAsync(string id, UpdateItemRequest req, string actorId, CancellationToken ct)
    {
        var row = await _repo.FindByIdAsync(id, ct) ?? throw new NotFoundException("Item", id);
        if (row.RowVersion != req.RowVersion)
            throw new ConflictException("Item was modified by another user. Reload and try again.");

        // SKU uniqueness check on update only when caller is changing it.
        if (!string.IsNullOrWhiteSpace(req.Sku) && req.Sku != row.Sku
            && await _repo.FindBySkuAsync(req.Sku, ct) is not null)
            throw new ConflictException($"Item SKU '{req.Sku}' is already in use.");

        // For partial updates the FK snapshot we validate is the merge of stored + provided values.
        await ValidateForeignKeysAsync(
            req.UnitId ?? row.UnitId,
            req.GlAccountId ?? row.GlAccountId,
            req.DefaultSupplierId ?? row.DefaultSupplierId,
            ct);

        var newVersion = Guid.NewGuid().ToString("N");
        var affected = await _repo.UpdateAsync(id, req, actorId, row.RowVersion, newVersion, ct);
        if (affected == 0)
            throw new ConflictException("Item was modified by another user. Reload and try again.");

        using var conn = _factory.Create();
        using var tx = conn.BeginTransaction();
        await AuditLog.WriteAsync(conn, tx, actorId, "item.update", "items", id,
            after: new { req.Name, req.Category, req.DefaultPriceMinor, req.IsActive });
        tx.Commit();
        return newVersion;
    }

    public async Task DeleteAsync(string id, string actorId, string expectedVersion, CancellationToken ct)
    {
        var row = await _repo.FindByIdAsync(id, ct) ?? throw new NotFoundException("Item", id);
        if (row.RowVersion != expectedVersion)
            throw new ConflictException("Item was modified by another user. Reload and try again.");

        var newVersion = Guid.NewGuid().ToString("N");
        var affected = await _repo.SoftDeleteAsync(id, actorId, expectedVersion, newVersion, ct);
        if (affected == 0)
            throw new ConflictException("Item was modified by another user. Reload and try again.");

        using var conn = _factory.Create();
        using var tx = conn.BeginTransaction();
        await AuditLog.WriteAsync(conn, tx, actorId, "item.delete", "items", id);
        tx.Commit();
    }

    // Centralizes the actual "does this FK row exist?" lookup so create + update stay consistent.
    private async Task ValidateForeignKeysAsync(string? unitId, string? glAccountId, string? supplierId, CancellationToken ct)
    {
        using var conn = _factory.Create();
        if (unitId is not null && await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM units WHERE is_deleted = 0 AND id = @Id;", new { Id = unitId }) == 0)
            throw new NotFoundException("Unit", unitId);
        if (glAccountId is not null && await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM gl_accounts WHERE is_deleted = 0 AND id = @Id;", new { Id = glAccountId }) == 0)
            throw new NotFoundException("GlAccount", glAccountId);
        if (supplierId is not null && await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM suppliers WHERE is_deleted = 0 AND id = @Id;", new { Id = supplierId }) == 0)
            throw new NotFoundException("Supplier", supplierId);
    }

    private static ItemSummary ToSummary(ItemRow r) => new()
    {
        Id = r.Id,
        Sku = r.Sku,
        Name = r.Name,
        Category = r.Category,
        DefaultPriceMinor = r.DefaultPriceMinor,
        DefaultCurrency = r.DefaultCurrency,
        IsActive = r.IsActive
    };

    private static ItemDetail ToDetail(ItemRow r) => new()
    {
        Id = r.Id,
        Sku = r.Sku,
        Name = r.Name,
        Category = r.Category,
        Description = r.Description,
        UnitId = r.UnitId,
        UnitCode = r.UnitCode,
        GlAccountId = r.GlAccountId,
        GlAccountCode = r.GlAccountCode,
        DefaultSupplierId = r.DefaultSupplierId,
        DefaultSupplierCode = r.DefaultSupplierCode,
        DefaultPriceMinor = r.DefaultPriceMinor,
        DefaultCurrency = r.DefaultCurrency,
        IsActive = r.IsActive,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
        RowVersion = r.RowVersion
    };
}
