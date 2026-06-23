using FluentValidation;

namespace ProcureMax.Features.Auth;

public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record RevokeRequest(string RefreshToken);
public record AuthResponse(string AccessToken, string RefreshToken, int ExpiresIn, UserProfile User);
public record UserProfile(string Id, string Email, string FullName, IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}
