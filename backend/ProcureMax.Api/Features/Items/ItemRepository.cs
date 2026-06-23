using Dapper;

namespace ProcureMax.Features.Items;

internal sealed record ItemRow
{
    public string Id { get; init; } = "";
    public string? Sku { get; init; }
    public string Name { get; init; } = "";
    public string? Category { get; init; }
    public string? Description { get; init; }
    public string? UnitId { get; init; }
    public string? UnitCode { get; init; }
    public string? GlAccountId { get; init; }
    public string? GlAccountCode { get; init; }
    public string? DefaultSupplierId { get; init; }
    public string? DefaultSupplierCode { get; init; }
    public long DefaultPriceMinor { get; init; }
    public string DefaultCurrency { get; init; } = "USD";
    public bool IsActive { get; init; }
    public string CreatedAt { get; init; } = "";
    public string? UpdatedAt { get; init; }
    public string RowVersion { get; init; } = "";
}

internal interface IItemRepository
{
    Task<IReadOnlyList<ItemRow>> ListAsync(string? search, int limit, int offset, CancellationToken ct);
    Task<int> CountAsync(string? search, CancellationToken ct);
    Task<ItemRow?> FindByIdAsync(string id, CancellationToken ct);
    Task<ItemRow?> FindBySkuAsync(string sku, CancellationToken ct);
    Task<string> InsertAsync(CreateItemRequest req, string actorId, CancellationToken ct);
    Task<int> UpdateAsync(string id, UpdateItemRequest req, string actorId, string oldVersion, string newVersion, CancellationToken ct);
    Task<int> SoftDeleteAsync(string id, string actorId, string expectedVersion, string newVersion, CancellationToken ct);
}

internal sealed class ItemRepository(IDbConnectionFactory factory) : IItemRepository
{
    private readonly IDbConnectionFactory _factory = factory;

    // JOIN units/suppliers/gl_accounts to resolve display codes for the picker UI.
    private const string SelectColumns = """
        i.id AS Id, i.sku AS Sku, i.name AS Name, i.category AS Category, i.description AS Description,
        i.unit_id AS UnitId, u.code AS UnitCode,
        i.gl_account_id AS GlAccountId, g.code AS GlAccountCode,
        i.default_supplier_id AS DefaultSupplierId, s.code AS DefaultSupplierCode,
        i.default_price_minor AS DefaultPriceMinor, i.default_currency AS DefaultCurrency,
        CAST(i.is_active AS INTEGER) AS IsActive,
        i.created_at AS CreatedAt, i.updated_at AS UpdatedAt, i.row_version AS RowVersion
        """;

    private const string FromJoins = """
        FROM items i
            LEFT JOIN units u ON u.id = i.unit_id
            LEFT JOIN gl_accounts g ON g.id = i.gl_account_id
            LEFT JOIN suppliers s ON s.id = i.default_supplier_id
        """;

    public async Task<IReadOnlyList<ItemRow>> ListAsync(string? search, int limit, int offset, CancellationToken ct)
    {
        using var conn = _factory.Create();
        var has = !string.IsNullOrWhiteSpace(search);
        var like = has ? $"%{search}%" : null;
        var sql = $@"SELECT {SelectColumns} {FromJoins}
            WHERE i.is_deleted = 0
            {(has ? "AND (i.sku LIKE @Like OR i.name LIKE @Like OR i.category LIKE @Like)" : "")}
            ORDER BY i.name
            LIMIT @Limit OFFSET @Offset;";
        return (await conn.QueryAsync<ItemRow>(sql, new { Like = like, Limit = limit, Offset = offset })).ToList();
    }

    public async Task<int> CountAsync(string? search, CancellationToken ct)
    {
        using var conn = _factory.Create();
        var has = !string.IsNullOrWhiteSpace(search);
        var like = has ? $"%{search}%" : null;
        var sql = $"SELECT COUNT(*) FROM items i WHERE i.is_deleted = 0 {(has ? "AND (i.sku LIKE @Like OR i.name LIKE @Like OR i.category LIKE @Like)" : "")};";
        return await conn.ExecuteScalarAsync<int>(sql, new { Like = like });
    }

    public async Task<ItemRow?> FindByIdAsync(string id, CancellationToken ct)
    {
        using var conn = _factory.Create();
        return await conn.QuerySingleOrDefaultAsync<ItemRow>(
            $"SELECT {SelectColumns} {FromJoins} WHERE i.is_deleted = 0 AND i.id = @Id;", new { Id = id });
    }

    public async Task<ItemRow?> FindBySkuAsync(string sku, CancellationToken ct)
    {
        using var conn = _factory.Create();
        return await conn.QuerySingleOrDefaultAsync<ItemRow>(
            $"SELECT {SelectColumns} {FromJoins} WHERE i.is_deleted = 0 AND i.sku = @Sku;", new { Sku = sku });
    }

    public async Task<string> InsertAsync(CreateItemRequest req, string actorId, CancellationToken ct)
    {
        using var conn = _factory.Create();
        var id = $"item:{Guid.NewGuid():N}";
        await conn.ExecuteAsync("""
            INSERT INTO items (
                id, sku, name, category, description,
                unit_id, gl_account_id, default_supplier_id,
                default_price_minor, default_currency, is_active,
                created_at, created_by, is_deleted, row_version)
            VALUES (
                @Id, @Sku, @Name, @Category, @Description,
                @UnitId, @GlAccountId, @DefaultSupplierId,
                @DefaultPriceMinor, @DefaultCurrency, 1,
                @Now, @Actor, 0, @RowVersion);
            """, new
        {
            Id = id,
            req.Sku,
            req.Name,
            req.Category,
            req.Description,
            req.UnitId,
            req.GlAccountId,
            req.DefaultSupplierId,
            req.DefaultPriceMinor,
            req.DefaultCurrency,
            Now = DateTime.UtcNow.ToString("O"),
            Actor = actorId,
            RowVersion = Guid.NewGuid().ToString("N"),
        });
        return id;
    }

    public async Task<int> UpdateAsync(string id, UpdateItemRequest req, string actorId, string oldVersion, string newVersion, CancellationToken ct)
    {
        using var conn = _factory.Create();
        var sets = new List<string>();
        if (req.Sku is not null) sets.Add("sku = @Sku");
        if (req.Name is not null) sets.Add("name = @Name");
        if (req.Category is not null) sets.Add("category = @Category");
        if (req.Description is not null) sets.Add("description = @Description");
        if (req.UnitId is not null) sets.Add("unit_id = @UnitId");
        if (req.GlAccountId is not null) sets.Add("gl_account_id = @GlAccountId");
        if (req.DefaultSupplierId is not null) sets.Add("default_supplier_id = @DefaultSupplierId");
        if (req.DefaultPriceMinor is not null) sets.Add("default_price_minor = @DefaultPriceMinor");
        if (req.DefaultCurrency is not null) sets.Add("default_currency = @DefaultCurrency");
        if (req.IsActive is not null) sets.Add("is_active = @IsActive");
        sets.Add("updated_at = @Now");
        sets.Add("updated_by = @Actor");
        sets.Add("row_version = @NewVersion");

        var sql = $@"UPDATE items SET {string.Join(", ", sets)}
                      WHERE id = @Id AND row_version = @OldVersion AND is_deleted = 0;";
        return await conn.ExecuteAsync(sql, new
        {
            Id = id,
            req.Sku,
            req.Name,
            req.Category,
            req.Description,
            req.UnitId,
            req.GlAccountId,
            req.DefaultSupplierId,
            req.DefaultPriceMinor,
            req.DefaultCurrency,
            req.IsActive,
            Now = DateTime.UtcNow.ToString("O"),
            Actor = actorId,
            OldVersion = oldVersion,
            NewVersion = newVersion,
        });
    }

    public async Task<int> SoftDeleteAsync(string id, string actorId, string expectedVersion, string newVersion, CancellationToken ct)
    {
        using var conn = _factory.Create();
        return await conn.ExecuteAsync("""
            UPDATE items
               SET is_deleted = 1, updated_at = @Now, updated_by = @Actor, row_version = @NewVersion
             WHERE id = @Id AND row_version = @OldVersion AND is_deleted = 0;
            """, new { Id = id, Now = DateTime.UtcNow.ToString("O"), Actor = actorId, OldVersion = expectedVersion, NewVersion = newVersion });
    }
}
