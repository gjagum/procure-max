using System.Data;
using Dapper;

namespace ProcureMax.Core.Auth;

// Dapper-backed store for refresh token rotation. SHA-256 hashed tokens only.
public interface IRefreshTokenStore
{
    Task<string> CreateAsync(string userId, string refreshToken, DateTime expiresAt, IDbTransaction? tx = null);
    Task<bool> ValidateAndRevokeAsync(string refreshToken, string replacementTokenId, DateTime expiresAt, IDbConnection conn, IDbTransaction? tx = null);
    Task RevokeAllForUserAsync(string userId, IDbConnection conn, IDbTransaction? tx = null);
    Task<bool> IsUsableAsync(string refreshToken, IDbConnection conn, IDbTransaction? tx = null);
}

public class RefreshTokenStore : IRefreshTokenStore
{
    private readonly IDbConnectionFactory _factory;
    public RefreshTokenStore(IDbConnectionFactory factory) => _factory = factory;

    public async Task<string> CreateAsync(string userId, string refreshToken, DateTime expiresAt, IDbTransaction? tx = null)
    {
        var id = Guid.NewGuid().ToString("N");
        var hash = RefreshTokenHasher.Hash(refreshToken);
        var sql = """
            INSERT INTO refresh_tokens (id, user_id, token_hash, expires_at, created_at, revoked_at, replaced_by_id)
            VALUES (@Id, @UserId, @Hash, @ExpiresAt, @Now, NULL, NULL);
            """;
        var exec = tx is null
            ? await _factory.Create().ExecuteAsync(sql, new { Id = id, UserId = userId, Hash = hash, ExpiresAt = expiresAt.ToString("O"), Now = DateTime.UtcNow.ToString("O") })
            : await tx.Connection!.ExecuteAsync(sql, new { Id = id, UserId = userId, Hash = hash, ExpiresAt = expiresAt.ToString("O"), Now = DateTime.UtcNow.ToString("O") }, tx);
        return id;
    }

    public async Task<bool> ValidateAndRevokeAsync(string refreshToken, string replacementTokenId, DateTime expiresAt, IDbConnection conn, IDbTransaction? tx = null)
    {
        var hash = RefreshTokenHasher.Hash(refreshToken);
        var now = DateTime.UtcNow.ToString("O");
        var sql = """
            UPDATE refresh_tokens
               SET revoked_at = @Now, replaced_by_id = @ReplacementId
             WHERE token_hash = @Hash
               AND revoked_at IS NULL
               AND expires_at > @Now;
            """;
        var rows = await conn.ExecuteAsync(sql, new { Now = now, ReplacementId = replacementTokenId, Hash = hash, ExpiresAt = expiresAt.ToString("O") }, tx);
        return rows > 0;
    }

    public async Task RevokeAllForUserAsync(string userId, IDbConnection conn, IDbTransaction? tx = null)
    {
        var now = DateTime.UtcNow.ToString("O");
        await conn.ExecuteAsync("""
            UPDATE refresh_tokens SET revoked_at = @Now
             WHERE user_id = @UserId AND revoked_at IS NULL;
            """, new { UserId = userId, Now = now }, tx);
    }

    public async Task<bool> IsUsableAsync(string refreshToken, IDbConnection conn, IDbTransaction? tx = null)
    {
        var hash = RefreshTokenHasher.Hash(refreshToken);
        var now = DateTime.UtcNow.ToString("O");
        var count = await conn.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM refresh_tokens
             WHERE token_hash = @Hash AND revoked_at IS NULL AND expires_at > @Now;
            """, new { Hash = hash, Now = now }, tx);
        return count > 0;
    }
}
