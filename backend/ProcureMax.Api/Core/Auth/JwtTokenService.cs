using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ProcureMax.Core.Auth;

// Issues short-lived access JWTs (claims include permission catalog as 'perm' claims)
// and long-lived opaque refresh tokens (managed by RefreshTokenStore with rotation).
public interface IJwtTokenService
{
    (string accessToken, DateTime expiresAt) IssueAccessToken(string userId, string email, string fullName, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions);
    string GenerateRefreshToken();
}

public class JwtTokenService : IJwtTokenService
{
    private readonly AuthOptions _opts;
    public JwtTokenService(IOptions<AuthOptions> opts) => _opts = opts.Value;

    public (string accessToken, DateTime expiresAt) IssueAccessToken(
        string userId, string email, string fullName,
        IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_opts.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Email, email),
            new("name", fullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };
        foreach (var r in roles.Distinct())
        {
            claims.Add(new Claim("role", r));
            claims.Add(new Claim(ClaimTypes.Role, r));
        }
        foreach (var p in permissions.Distinct())
            claims.Add(new Claim("perm", p));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.SigningSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}

// Refresh tokens are stored as a SHA-256 hash of the token (so a DB leak doesn't grant access).
// Rotation: any refresh marks the old record revoked_at + replaced_by_id and inserts a fresh row.
public static class RefreshTokenHasher
{
    public static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
