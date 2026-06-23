using Microsoft.Data.Sqlite;
using ProcureMax.Features.Users;

namespace ProcureMax.Features.Auth;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest req, CancellationToken ct);
    Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct);
    Task LogoutAsync(string refreshToken, string? currentUserId, CancellationToken ct);
}

public class AuthService : IAuthService
{
    private readonly IDbConnectionFactory _factory;
    private readonly IJwtTokenService _jwt;
    private readonly IRefreshTokenStore _refresh;
    private readonly AuthOptions _auth;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IDbConnectionFactory factory, IJwtTokenService jwt, IRefreshTokenStore refresh, AuthOptions auth, ILogger<AuthService> logger)
    {
        _factory = factory; _jwt = jwt; _refresh = refresh; _auth = auth; _logger = logger;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        using var conn = _factory.Create();
        var auth = await UserRepository.GetAuthViewByEmailAsync(conn, req.Email);
        if (auth is null || !auth.User.IsActive || !ProcureMax.Core.Auth.PasswordHasher.Verify(req.Password, auth.User.PasswordHash))
            throw new Core.AuthException("Invalid email or password.");

        var (access, expiresAt) = _jwt.IssueAccessToken(auth.User.Id, auth.User.Email, auth.User.FullName, auth.RoleNames, auth.Permissions);
        var refreshToken = _jwt.GenerateRefreshToken();
        await _refresh.CreateAsync(auth.User.Id, refreshToken, DateTime.UtcNow.AddDays(_auth.RefreshTokenDays));

        return new AuthResponse(access, refreshToken, (int)(expiresAt - DateTime.UtcNow).TotalSeconds,
            new UserProfile(auth.User.Id, auth.User.Email, auth.User.FullName, auth.RoleNames, auth.Permissions));
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        using var conn = _factory.Create();
        if (!await _refresh.IsUsableAsync(refreshToken, conn))
            throw new Core.AuthException("Invalid or expired refresh token.");

        // Find owning user via hash
        var hash = RefreshTokenHasher.Hash(refreshToken);
        var userId = await Dapper.SqlMapper.ExecuteScalarAsync<string?>(conn,
            "SELECT user_id FROM refresh_tokens WHERE token_hash = @Hash AND revoked_at IS NULL;",
            new { Hash = hash });
        if (userId is null)
            throw new Core.AuthException("Invalid or expired refresh token.");

        var auth = await UserRepository.GetAuthViewByIdAsync(conn, userId)
                   ?? throw new Core.AuthException("User not found.");

        // Revoke old + create new (rotation). Failure here means a race / replay.
        var newRefresh = _jwt.GenerateRefreshToken();
        var newExpiry = DateTime.UtcNow.AddDays(_auth.RefreshTokenDays);
        var ok = await _refresh.ValidateAndRevokeAsync(refreshToken, "rotated", newExpiry, conn);
        if (!ok)
            throw new Core.AuthException("Refresh token already used.");

        await _refresh.CreateAsync(auth.User.Id, newRefresh, newExpiry);

        var (access, expiresAt) = _jwt.IssueAccessToken(auth.User.Id, auth.User.Email, auth.User.FullName, auth.RoleNames, auth.Permissions);
        return new AuthResponse(access, newRefresh, (int)(expiresAt - DateTime.UtcNow).TotalSeconds,
            new UserProfile(auth.User.Id, auth.User.Email, auth.User.FullName, auth.RoleNames, auth.Permissions));
    }

    public async Task LogoutAsync(string refreshToken, string? currentUserId, CancellationToken ct)
    {
        using var conn = _factory.Create();
        // Revoke the chain rooted at this token; loop closure: set revoked_at only.
        await Dapper.SqlMapper.ExecuteAsync(conn,
            "UPDATE refresh_tokens SET revoked_at = @Now WHERE token_hash = @Hash AND revoked_at IS NULL;",
            new { Now = DateTime.UtcNow.ToString("O"), Hash = RefreshTokenHasher.Hash(refreshToken) });
    }
}
