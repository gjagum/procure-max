using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace ProcureMax.Core.Common;

// FluentValidation endpoint filter: runs IValidator<T> against the expected request DTO
// before the handler executes. On failure, throws a domain ValidationException so the global
// GlobalExceptionHandler emits a single RFC 9457 problem-details response from one place.
//
// Wiring on a route:
//   grp.MapPost("/", CreateAsync).AddRequestValidation<CreateUserRequest>();
//
// If no IValidator<T> is registered for T the filter is a no-op — useful when only some
// overloads of a slice have validation.
public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var req = ctx.Arguments.OfType<T>().FirstOrDefault();
        if (req is null)
            return await next(ctx);

        var validator = ctx.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null)
            return await next(ctx);

        var result = await validator.ValidateAsync(req, ctx.HttpContext.RequestAborted);
        if (result.IsValid)
            return await next(ctx);

        throw new ValidationException(ToDictionary(result.Errors));
    }

    private static IReadOnlyDictionary<string, string[]> ToDictionary(List<ValidationFailure> failures)
        => failures
            .GroupBy(f => string.IsNullOrWhiteSpace(f.PropertyName) ? "_request" : f.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray(), StringComparer.Ordinal);
}

public static class EndpointValidationExtensions
{
    // Attach one validator per request DTO. Chainable.
    public static RouteHandlerBuilder AddRequestValidation<T>(this RouteHandlerBuilder builder)
        where T : class
        => builder.AddEndpointFilter<ValidationFilter<T>>();
}
