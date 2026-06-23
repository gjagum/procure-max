using FluentValidation;

namespace ProcureMax.Features.Users;

public record UserSummary
{
    public string Id { get; init; } = "";
    public string Email { get; init; } = "";
    public string FullName { get; init; } = "";
    public bool IsActive { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public string CreatedAt { get; init; } = "";
    public string RowVersion { get; init; } = "";
}

/// <summary>Internal flat row for Dapper materialization (no list-type columns).</summary>
internal sealed record UserSummaryRow
{
    public string Id { get; init; } = "";
    public string Email { get; init; } = "";
    public string FullName { get; init; } = "";
    public bool IsActive { get; init; }
    public string CreatedAt { get; init; } = "";
    public string RowVersion { get; init; } = "";
}
public record UserDetail(string Id, string Email, string FullName, bool IsActive, IReadOnlyList<string> RoleIds, IReadOnlyList<string> RoleNames, string CreatedAt, string? UpdatedAt, string RowVersion);
public record CreateUserRequest(string Email, string FullName, string Password, IReadOnlyList<string> RoleIds);
public record UpdateUserRequest(string? FullName, bool? IsActive, string RowVersion);
public record AssignRolesRequest(IReadOnlyList<string> RoleIds);

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.RoleIds).NotEmpty().Must(r => r.All(id => id.StartsWith("role:")));
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.FullName).MaximumLength(120).When(x => x.FullName is not null);
    }
}
