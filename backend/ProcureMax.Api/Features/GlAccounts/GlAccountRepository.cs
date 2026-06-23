using Dapper;

namespace ProcureMax.Features.GlAccounts;

internal sealed record GlAccountRow
{
    public string Id { get; init; } = "";
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public bool IsActive { get; init; }
    public string CreatedAt { get; init; } = "";
    public string? UpdatedAt { get; init; }
    public string RowVersion { get; init; } = "";
}

internal interface IGlAccountRepository
{
    Task<IReadOnlyList<GlAccountRow>> ListAsync(string? search, int limit, int offset, CancellationToken ct);
    Task<int> CountAsync(string? search, CancellationToken ct);
    Task<GlAccountRow?> FindByIdAsync(string id, CancellationToken ct);
    Task<GlAccountRow?> FindByCodeAsync(string code, CancellationToken ct);
    Task<string> InsertAsync(string code, string name, string actorId, CancellationToken ct);
    Task<int> UpdateAsync(GlAccountRow row, string? name, bool? isActive, string actorId, string newVersion, CancellationToken ct);
    Task<int> SoftDeleteAsync(string id, string actorId, string expectedVersion, string newVersion, CancellationToken ct);
}

internal sealed class GlAccountRepository(IDbConnectionFactory factory) : IGlAccountRepository
{
    private readonly IDbConnectionFactory _factory = factory;

    private const string SelectColumns = """
        id AS Id, code AS Code, name AS Name,
        CAST(is_active AS INTEGER) AS IsActive,
        created_at AS CreatedAt, updated_at AS UpdatedAt, row_version AS RowVersion
        """;

    public async Task<IReadOnlyList<GlAccountRow>> ListAsync(string? search, int limit, int offset, CancellationToken ct)
    {
        using var conn = _factory.Create();
        var has = !string.IsNullOrWhiteSpace(search);
        var like = has ? $"%{search}%" : null;
        var sql = $@"SELECT {SelectColumns} FROM gl_accounts WHERE is_deleted = 0
            {(has ? "AND (code LIKE @Like OR name LIKE @Like)" : "")}
            ORDER BY code
            LIMIT @Limit OFFSET @Offset;";
        return (await conn.QueryAsync<GlAccountRow>(sql, new { Like = like, Limit = limit, Offset = offset })).ToList();
    }

    public async Task<int> CountAsync(string? search, CancellationToken ct)
    {
        using var conn = _factory.Create();
        var has = !string.IsNullOrWhiteSpace(search);
        var like = has ? $"%{search}%" : null;
        var sql = $"SELECT COUNT(*) FROM gl_accounts WHERE is_deleted = 0 {(has ? "AND (code LIKE @Like OR name LIKE @Like)" : "")};";
        return await conn.ExecuteScalarAsync<int>(sql, new { Like = like });
    }

    public async Task<GlAccountRow?> FindByIdAsync(string id, CancellationToken ct)
    {
        using var conn = _factory.Create();
        return await conn.QuerySingleOrDefaultAsync<GlAccountRow>(
            $"SELECT {SelectColumns} FROM gl_accounts WHERE is_deleted = 0 AND id = @Id;", new { Id = id });
    }

    public async Task<GlAccountRow?> FindByCodeAsync(string code, CancellationToken ct)
    {
        using var conn = _factory.Create();
        return await conn.QuerySingleOrDefaultAsync<GlAccountRow>(
            $"SELECT {SelectColumns} FROM gl_accounts WHERE is_deleted = 0 AND lower(code) = lower(@Code);", new { Code = code });
    }

    public async Task<string> InsertAsync(string code, string name, string actorId, CancellationToken ct)
    {
        using var conn = _factory.Create();
        var id = $"gl:{code.ToUpperInvariant()}";
        await conn.ExecuteAsync("""
            INSERT INTO gl_accounts (id, code, name, is_active, created_at, created_by, is_deleted, row_version)
            VALUES (@Id, @Code, @Name, 1, @Now, @Actor, 0, @RowVersion);
            """, new { Id = id, Code = code.ToUpperInvariant(), Name = name, Now = DateTime.UtcNow.ToString("O"), Actor = actorId, RowVersion = Guid.NewGuid().ToString("N") });
        return id;
    }

    public async Task<int> UpdateAsync(GlAccountRow row, string? name, bool? isActive, string actorId, string newVersion, CancellationToken ct)
    {
        using var conn = _factory.Create();
        var sets = new List<string>();
        if (name is not null) sets.Add("name = @Name");
        if (isActive is not null) sets.Add("is_active = @IsActive");
        sets.Add("updated_at = @Now");
        sets.Add("updated_by = @Actor");
        sets.Add("row_version = @NewVersion");
        var sql = $@"UPDATE gl_accounts SET {string.Join(", ", sets)}
                      WHERE id = @Id AND row_version = @OldVersion AND is_deleted = 0;";
        return await conn.ExecuteAsync(sql,
            new { row.Id, Name = name, IsActive = isActive, Now = DateTime.UtcNow.ToString("O"), Actor = actorId, OldVersion = row.RowVersion, NewVersion = newVersion });
    }

    public async Task<int> SoftDeleteAsync(string id, string actorId, string expectedVersion, string newVersion, CancellationToken ct)
    {
        using var conn = _factory.Create();
        return await conn.ExecuteAsync("""
            UPDATE gl_accounts
               SET is_deleted = 1, updated_at = @Now, updated_by = @Actor, row_version = @NewVersion
             WHERE id = @Id AND row_version = @OldVersion AND is_deleted = 0;
            """, new { Id = id, Now = DateTime.UtcNow.ToString("O"), Actor = actorId, OldVersion = expectedVersion, NewVersion = newVersion });
    }
}
