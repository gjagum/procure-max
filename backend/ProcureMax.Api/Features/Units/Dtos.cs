using FluentValidation;

namespace ProcureMax.Features.Units;

public record UnitSummary
{
    public string Id { get; init; } = "";
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public bool IsActive { get; init; }
}

public record UnitDetail
{
    public string Id { get; init; } = "";
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public bool IsActive { get; init; }
    public string CreatedAt { get; init; } = "";
    public string? UpdatedAt { get; init; }
    public string RowVersion { get; init; } = "";
}

public record CreateUnitRequest(string Code, string Name);
public record UpdateUnitRequest(string? Name, bool? IsActive, string RowVersion);

public class CreateUnitRequestValidator : AbstractValidator<CreateUnitRequest>
{
    public CreateUnitRequestValidator()
    {
        // UoM codes are short & uppercase by convention (EA, KG, M). Enforced at validation time.
        RuleFor(x => x.Code).NotEmpty().MaximumLength(16).Matches(@"^[A-Z0-9]+$")
            .WithMessage("Unit code must be uppercase alphanumeric (e.g. 'EA', 'KG', 'M').");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(60);
    }
}

public class UpdateUnitRequestValidator : AbstractValidator<UpdateUnitRequest>
{
    public UpdateUnitRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(60).When(x => x.Name is not null);
    }
}
