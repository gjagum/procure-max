using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using ProcureMax.Core.Common;
using ProcureMax.Features.Users;

namespace ProcureMax.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/auth").WithTags("Auth").AllowAnonymous();

        grp.MapPost("/login", LoginAsync).AddRequestValidation<LoginRequest>();
        grp.MapPost("/refresh", RefreshAsync);
        grp.MapPost("/logout", LogoutAsync).RequireAuthorization();
        grp.MapGet("/me", MeAsync).RequireAuthorization();
        return app;
    }

    private static async Task<Ok<AuthResponse>> LoginAsync(
        LoginRequest req, IAuthService svc, CancellationToken ct)
    {
        var result = await svc.LoginAsync(req, ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<AuthResponse>> RefreshAsync(
        RefreshRequest req, IAuthService svc, CancellationToken ct) =>
        TypedResults.Ok(await svc.RefreshAsync(req.RefreshToken, ct));

    private static async Task<Ok> LogoutAsync(
        RevokeRequest req, IAuthService svc, ICurrentUser user, CancellationToken ct)
    {
        await svc.LogoutAsync(req.RefreshToken, user.UserId, ct);
        return TypedResults.Ok();
    }

    private static Ok<UserProfile> MeAsync(ICurrentUser user, IDbConnectionFactory factory)
    {
        // Cached in token already; fetch fresh profile to reflect role changes.
        using var conn = factory.Create();
        var view = UserRepository.GetAuthViewByIdAsync(conn, user.UserId!).GetAwaiter().GetResult()
                   ?? throw new NotFoundException("User");
        return TypedResults.Ok(new UserProfile(view.User.Id, view.User.Email, view.User.FullName, view.RoleNames, view.Permissions));
    }
}
