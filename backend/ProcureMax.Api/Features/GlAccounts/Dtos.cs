using FluentValidation;

namespace ProcureMax.Features.GlAccounts;

public record GlAccountSummary
{
    public string Id { get; init; } = "";
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public bool IsActive { get; init; }
}

public record GlAccountDetail
{
    public string Id { get; init; } = "";
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public bool IsActive { get; init; }
    public string CreatedAt { get; init; } = "";
    public string? UpdatedAt { get; init; }
    public string RowVersion { get; init; } = "";
}

public record CreateGlAccountRequest(string Code, string Name);
public record UpdateGlAccountRequest(string? Name, bool? IsActive, string RowVersion);

public class CreateGlAccountRequestValidator : AbstractValidator<CreateGlAccountRequest>
{
    public CreateGlAccountRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32).Matches(@"^[A-Za-z0-9_\-\:]+$")
            .WithMessage("Code may only contain letters, digits, hyphens, underscores or colons.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
    }
}

public class UpdateGlAccountRequestValidator : AbstractValidator<UpdateGlAccountRequest>
{
    public UpdateGlAccountRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(120).When(x => x.Name is not null);
    }
}
