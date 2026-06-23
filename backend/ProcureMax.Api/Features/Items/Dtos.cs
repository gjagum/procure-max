using FluentValidation;
using ProcureMax.Core;

namespace ProcureMax.Features.Items;

// Summary used by item pickers on PR line UIs — needs to expose default price
// so the client can pre-fill line amount without a follow-up detail GET.
public record ItemSummary
{
    public string Id { get; init; } = "";
    public string? Sku { get; init; }
    public string Name { get; init; } = "";
    public string? Category { get; init; }
    public long DefaultPriceMinor { get; init; }
    public string DefaultCurrency { get; init; } = "USD";
    public bool IsActive { get; init; }
}

public record ItemDetail
{
    public string Id { get; init; } = "";
    public string? Sku { get; init; }
    public string Name { get; init; } = "";
    public string? Category { get; init; }
    public string? Description { get; init; }
    public string? UnitId { get; init; }
    public string? UnitCode { get; init; }
    public string? GlAccountId { get; init; }
    public string? GlAccountCode { get; init; }
    public string? DefaultSupplierId { get; init; }
    public string? DefaultSupplierCode { get; init; }
    public long DefaultPriceMinor { get; init; }
    public string DefaultCurrency { get; init; } = "USD";
    public bool IsActive { get; init; }
    public string CreatedAt { get; init; } = "";
    public string? UpdatedAt { get; init; }
    public string RowVersion { get; init; } = "";
}

public record CreateItemRequest(
    string? Sku,
    string Name,
    string? Category,
    string? Description,
    string? UnitId,
    string? GlAccountId,
    string? DefaultSupplierId,
    long DefaultPriceMinor,
    string DefaultCurrency = "USD");

public record UpdateItemRequest(
    string? Sku,
    string? Name,
    string? Category,
    string? Description,
    string? UnitId,
    string? GlAccountId,
    string? DefaultSupplierId,
    long? DefaultPriceMinor,
    string? DefaultCurrency,
    bool? IsActive,
    string RowVersion);

public class CreateItemRequestValidator : AbstractValidator<CreateItemRequest>
{
    public CreateItemRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Sku).MaximumLength(48).Matches(@"^[A-Za-z0-9_\-\:]*$")
            .WithMessage("SKU may only contain letters, digits, hyphens, underscores or colons.");
        RuleFor(x => x.Category).MaximumLength(60).When(x => x.Category is not null);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        RuleFor(x => x.DefaultCurrency).NotEmpty().Length(3).Matches(@"^[A-Z]{3}$")
            .WithMessage("Currency must be an ISO-4217 code (3 uppercase letters).");
        RuleFor(x => x.DefaultPriceMinor).GreaterThanOrEqualTo(0);
    }
}

public class UpdateItemRequestValidator : AbstractValidator<UpdateItemRequest>
{
    public UpdateItemRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(160).When(x => x.Name is not null);
        RuleFor(x => x.Sku).MaximumLength(48).Matches(@"^[A-Za-z0-9_\-\:]*$").When(x => x.Sku is not null)
            .WithMessage("SKU may only contain letters, digits, hyphens, underscores or colons.");
        RuleFor(x => x.Category).MaximumLength(60).When(x => x.Category is not null);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        RuleFor(x => x.DefaultCurrency).Length(3).Matches(@"^[A-Z]{3}$").When(x => x.DefaultCurrency is not null)
            .WithMessage("Currency must be an ISO-4217 code (3 uppercase letters).");
        RuleFor(x => x.DefaultPriceMinor).GreaterThanOrEqualTo(0).When(x => x.DefaultPriceMinor is not null);
    }
}
