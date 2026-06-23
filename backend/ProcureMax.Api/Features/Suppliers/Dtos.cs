using FluentValidation;

namespace ProcureMax.Features.Suppliers;

// Summary row used in paged lists — exposes only the columns the picker UI needs.
public record SupplierSummary
{
    public string Id { get; init; } = "";
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string? LegalName { get; init; }
    public bool IsActive { get; init; }
    public bool IsBlocked { get; init; }
    public string Currency { get; init; } = "USD";
}

public record SupplierDetail
{
    public string Id { get; init; } = "";
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string? LegalName { get; init; }
    public string? TaxId { get; init; }
    public string? ContactName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public string? PaymentTerms { get; init; }
    public string Currency { get; init; } = "USD";
    public bool IsActive { get; init; }
    public bool IsBlocked { get; init; }
    public string CreatedAt { get; init; } = "";
    public string? UpdatedAt { get; init; }
    public string RowVersion { get; init; } = "";
}

public record CreateSupplierRequest(
    string Code,
    string Name,
    string? LegalName,
    string? TaxId,
    string? ContactName,
    string? Email,
    string? Phone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? PostalCode,
    string? Country,
    string? PaymentTerms,
    string Currency = "USD");

public record UpdateSupplierRequest(
    string? Name,
    string? LegalName,
    string? TaxId,
    string? ContactName,
    string? Email,
    string? Phone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? PostalCode,
    string? Country,
    string? PaymentTerms,
    string? Currency,
    bool? IsActive,
    bool? IsBlocked,
    string RowVersion);

public class CreateSupplierRequestValidator : AbstractValidator<CreateSupplierRequest>
{
    public CreateSupplierRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32).Matches(@"^[A-Za-z0-9_\-\:]+$")
            .WithMessage("Code may only contain letters, digits, hyphens, underscores or colons.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Currency).NotEmpty().Length(3).Matches(@"^[A-Z]{3}$")
            .WithMessage("Currency must be an ISO-4217 code (3 uppercase letters).");
    }
}

public class UpdateSupplierRequestValidator : AbstractValidator<UpdateSupplierRequest>
{
    public UpdateSupplierRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(160).When(x => x.Name is not null);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Currency).Length(3).Matches(@"^[A-Z]{3}$").When(x => x.Currency is not null)
            .WithMessage("Currency must be an ISO-4217 code (3 uppercase letters).");
    }
}
