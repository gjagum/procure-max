using Dapper;

namespace ProcureMax.Features.Suppliers;

// Internal flat row for Dapper materialization.
internal sealed record SupplierRow
{
    public string Id { get; init; } = "";
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string? LegalName { get; init; }
    public string? TaxId { get; init; }
    public string? ContactName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public string? PaymentTerms { get; init; }
    public string Currency { get; init; } = "USD";
    public bool IsActive { get; init; }
    public bool IsBlocked { get; init; }
    public string CreatedAt { get; init; } = "";
    public string? UpdatedAt { get; init; }
    public string RowVersion { get; init; } = "";
}

internal interface ISupplierRepository
{
    Task<IReadOnlyList<SupplierRow>> ListAsync(string? search, int limit, int offset, CancellationToken ct);
    Task<int> CountAsync(string? search, CancellationToken ct);
    Task<SupplierRow?> FindByIdAsync(string id, CancellationToken ct);
    Task<SupplierRow?> FindByCodeAsync(string code, CancellationToken ct);
    Task<string> InsertAsync(CreateSupplierRequest req, string actorId, CancellationToken ct);
    Task<int> UpdateAsync(string id, UpdateSupplierRequest req, string actorId, string oldVersion, string newVersion, CancellationToken ct);
    Task<int> SoftDeleteAsync(string id, string actorId, string expectedVersion, string newVersion, CancellationToken ct);
}

internal sealed class SupplierRepository(IDbConnectionFactory factory) : ISupplierRepository
{
    private readonly IDbConnectionFactory _factory = factory;

    // Aliases must be PascalCase — Dapper won't auto map snake_case (repo memory §2).
    private const string SelectColumns = """
        id AS Id, code AS Code, name AS Name, legal_name AS LegalName, tax_id AS TaxId,
        contact_name AS ContactName, email AS Email, phone AS Phone,
        address_line1 AS AddressLine1, address_line2 AS AddressLine2,
        city AS City, state AS State, postal_code AS PostalCode, country AS Country,
        payment_terms AS PaymentTerms, currency AS Currency,
        CAST(is_active AS INTEGER) AS IsActive,
        CAST(is_blocked AS INTEGER) AS IsBlocked,
        created_at AS CreatedAt, updated_at AS UpdatedAt, row_version AS RowVersion
        """;

    public async Task<IReadOnlyList<SupplierRow>> ListAsync(string? search, int limit, int offset, CancellationToken ct)
    {
        using var conn = _factory.Create();
        var has = !string.IsNullOrWhiteSpace(search);
        var like = has ? $"%{search}%" : null;
        var sql = $@"SELECT {SelectColumns} FROM suppliers WHERE is_deleted = 0
            {(has ? "AND (code LIKE @Like OR name LIKE @Like OR legal_name LIKE @Like)" : "")}
            ORDER BY name
            LIMIT @Limit OFFSET @Offset;";
        return (await conn.QueryAsync<SupplierRow>(sql, new { Like = like, Limit = limit, Offset = offset })).ToList();
    }

    public async Task<int> CountAsync(string? search, CancellationToken ct)
    {
        using var conn = _factory.Create();
        var has = !string.IsNullOrWhiteSpace(search);
        var like = has ? $"%{search}%" : null;
        var sql = $"SELECT COUNT(*) FROM suppliers WHERE is_deleted = 0 {(has ? "AND (code LIKE @Like OR name LIKE @Like OR legal_name LIKE @Like)" : "")};";
        return await conn.ExecuteScalarAsync<int>(sql, new { Like = like });
    }

    public async Task<SupplierRow?> FindByIdAsync(string id, CancellationToken ct)
    {
        using var conn = _factory.Create();
        return await conn.QuerySingleOrDefaultAsync<SupplierRow>(
            $"SELECT {SelectColumns} FROM suppliers WHERE is_deleted = 0 AND id = @Id;", new { Id = id });
    }

    public async Task<SupplierRow?> FindByCodeAsync(string code, CancellationToken ct)
    {
        using var conn = _factory.Create();
        return await conn.QuerySingleOrDefaultAsync<SupplierRow>(
            $"SELECT {SelectColumns} FROM suppliers WHERE is_deleted = 0 AND lower(code) = lower(@Code);", new { Code = code });
    }

    public async Task<string> InsertAsync(CreateSupplierRequest req, string actorId, CancellationToken ct)
    {
        using var conn = _factory.Create();
        var id = $"supplier:{Guid.NewGuid():N}";
        await conn.ExecuteAsync("""
            INSERT INTO suppliers (
                id, code, name, legal_name, tax_id, contact_name, email, phone,
                address_line1, address_line2, city, state, postal_code, country,
                payment_terms, currency, is_active, is_blocked,
                created_at, created_by, is_deleted, row_version)
            VALUES (
                @Id, @Code, @Name, @LegalName, @TaxId, @ContactName, @Email, @Phone,
                @AddressLine1, @AddressLine2, @City, @State, @PostalCode, @Country,
                @PaymentTerms, @Currency, 1, 0,
                @Now, @Actor, 0, @RowVersion);
            """, new
        {
            Id = id,
            req.Code,
            req.Name,
            req.LegalName,
            req.TaxId,
            req.ContactName,
            req.Email,
            req.Phone,
            req.AddressLine1,
            req.AddressLine2,
            req.City,
            req.State,
            req.PostalCode,
            req.Country,
            req.PaymentTerms,
            req.Currency,
            Now = DateTime.UtcNow.ToString("O"),
            Actor = actorId,
            RowVersion = Guid.NewGuid().ToString("N"),
        });
        return id;
    }

    public async Task<int> UpdateAsync(string id, UpdateSupplierRequest req, string actorId, string oldVersion, string newVersion, CancellationToken ct)
    {
        using var conn = _factory.Create();
        // Build a column-by-column UPDATE only for fields the caller supplied.
        // Everything else is left untouched, preserving null-vs-absent semantics.
        var sets = new List<string>();
        if (req.Name is not null) sets.Add("name = @Name");
        if (req.LegalName is not null) sets.Add("legal_name = @LegalName");
        if (req.TaxId is not null) sets.Add("tax_id = @TaxId");
        if (req.ContactName is not null) sets.Add("contact_name = @ContactName");
        if (req.Email is not null) sets.Add("email = @Email");
        if (req.Phone is not null) sets.Add("phone = @Phone");
        if (req.AddressLine1 is not null) sets.Add("address_line1 = @AddressLine1");
        if (req.AddressLine2 is not null) sets.Add("address_line2 = @AddressLine2");
        if (req.City is not null) sets.Add("city = @City");
        if (req.State is not null) sets.Add("state = @State");
        if (req.PostalCode is not null) sets.Add("postal_code = @PostalCode");
        if (req.Country is not null) sets.Add("country = @Country");
        if (req.PaymentTerms is not null) sets.Add("payment_terms = @PaymentTerms");
        if (req.Currency is not null) sets.Add("currency = @Currency");
        if (req.IsActive is not null) sets.Add("is_active = @IsActive");
        if (req.IsBlocked is not null) sets.Add("is_blocked = @IsBlocked");
        sets.Add("updated_at = @Now");
        sets.Add("updated_by = @Actor");
        sets.Add("row_version = @NewVersion");

        var sql = $@"UPDATE suppliers SET {string.Join(", ", sets)}
                      WHERE id = @Id AND row_version = @OldVersion AND is_deleted = 0;";
        return await conn.ExecuteAsync(sql, new
        {
            Id = id,
            req.Name,
            req.LegalName,
            req.TaxId,
            req.ContactName,
            req.Email,
            req.Phone,
            req.AddressLine1,
            req.AddressLine2,
            req.City,
            req.State,
            req.PostalCode,
            req.Country,
            req.PaymentTerms,
            req.Currency,
            req.IsActive,
            req.IsBlocked,
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
            UPDATE suppliers
               SET is_deleted = 1, updated_at = @Now, updated_by = @Actor, row_version = @NewVersion
             WHERE id = @Id AND row_version = @OldVersion AND is_deleted = 0;
            """, new { Id = id, Now = DateTime.UtcNow.ToString("O"), Actor = actorId, OldVersion = expectedVersion, NewVersion = newVersion });
    }
}
