using FluentValidation;

namespace ProcureMax.Features.Roles;

public record RoleSummary
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public bool IsSystem { get; init; }
    public int PermissionCount { get; init; }
}
public record RoleDetail(string Id, string Name, string? Description, bool IsSystem, IReadOnlyList<string> PermissionIds, IReadOnlyList<PermissionInfo> AllPermissions);
public record PermissionInfo
{
    public string Id { get; init; } = "";
    public string Area { get; init; } = "";
    public string Action { get; init; } = "";
}
public record CreateRoleRequest(string Name, string? Description, IReadOnlyList<string> PermissionIds);
public record UpdateRoleRequest(string? Description, string RowVersion, IReadOnlyList<string>? PermissionIds);

public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.PermissionIds).NotEmpty();
    }
}
