using FluentValidation;

namespace ProcureMax.Features.CostCenters;

// Dapper-friendly: positional records fail when SQLite INTEGER arrives as Int64 —
// use record classes with init properties + a parameterless default ctor so the
// default mapper can apply its type conversions. (See repo memory: dapper-lessons.md.)
public record CostCenterSummary
{
    public string Id { get; init; } = "";
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public bool IsActive { get; init; }
}

public record CostCenterDetail
{
    public string Id { get; init; } = "";
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public bool IsActive { get; init; }
    public string CreatedAt { get; init; } = "";
    public string? UpdatedAt { get; init; }
    public string RowVersion { get; init; } = "";
}

public record CreateCostCenterRequest(string Code, string Name);
public record UpdateCostCenterRequest(string? Name, bool? IsActive, string RowVersion);

public class CreateCostCenterRequestValidator : AbstractValidator<CreateCostCenterRequest>
{
    public CreateCostCenterRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32).Matches(@"^[A-Za-z0-9_\-\:]+$")
            .WithMessage("Code may only contain letters, digits, hyphens, underscores or colons.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
    }
}

public class UpdateCostCenterRequestValidator : AbstractValidator<UpdateCostCenterRequest>
{
    public UpdateCostCenterRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(120).When(x => x.Name is not null);
    }
}
