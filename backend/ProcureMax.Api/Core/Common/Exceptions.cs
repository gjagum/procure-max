using ProcureMax.Core.Common;

namespace ProcureMax.Core;

public class DomainException(string message, string code = "domain_error") : Exception(message)
{
    public string Code { get; } = code;
}

public class NotFoundException(string what, string? id = null)
    : DomainException(id is null ? $"{what} not found" : $"{what} '{id}' not found", "not_found");

public class ConflictException(string message) : DomainException(message, "conflict");

public class ForbiddenException(string message = "Forbidden") : DomainException(message, "forbidden");

// Named to avoid collision with System.UnauthorizedAccessException.
public class AuthException(string message = "Unauthorized") : DomainException(message, "unauthorized");

public class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : DomainException("Validation failed", "validation")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
