using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ProcureMax.Core.Middleware;

// Central mapper from domain / framework exceptions to RFC 9457 Problem Details.
// Registered via AddExceptionHandler<GlobalExceptionHandler>() + AddProblemDetails() in Program.cs.
//
// Wiring intentionally thin: we only decide the (status, title, code, errors) tuple and let
// IProblemDetailsService do the actual writing so framework-emitted 401/403/404 share the same shape.
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Client went away — let Kestrel handle the disconnect and skip our logging noise.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            return false;

        var (status, title, code, errors, logLevel) = Map(exception);

        // Stable per-request trace id so a caller can grep server logs.
        var traceId = httpContext.TraceIdentifier;

        if (logLevel == LogLevel.Error)
            _logger.LogError(exception, "Unhandled exception on {Method} {Path} (trace {TraceId})",
                httpContext.Request.Method, httpContext.Request.Path, traceId);
        else
            _logger.LogDebug(exception, "Domain exception ({Code}) on {Method} {Path} (trace {TraceId})",
                code, httpContext.Request.Method, httpContext.Request.Path, traceId);

        var problem = new ProblemDetails
        {
            Status = (int)status,
            Title = title,
            Type = TypeFor(status),
            // In Development show the real message; in production the global ProblemDetails
            // customization will mask Detail for 5xx so internal details never leak.
            Detail = logLevel == LogLevel.Error ? null : exception.Message,
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = traceId;

        if (errors is { Count: > 0 })
            problem.Extensions["errors"] = errors;

        httpContext.Response.StatusCode = (int)status;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static (HttpStatusCode status, string title, string code,
                    IReadOnlyDictionary<string, string[]>? errors, LogLevel logLevel) Map(Exception ex) => ex switch
    {
        ValidationException ve => (HttpStatusCode.BadRequest, "Validation failed", ve.Code,
            ve.Errors, LogLevel.Debug),
        AuthException => (HttpStatusCode.Unauthorized, "Unauthorized", "unauthorized",
            null, LogLevel.Debug),
        ForbiddenException => (HttpStatusCode.Forbidden, "Forbidden", "forbidden",
            null, LogLevel.Debug),
        NotFoundException => (HttpStatusCode.NotFound, "Not Found", "not_found",
            null, LogLevel.Debug),
        ConflictException => (HttpStatusCode.Conflict, "Conflict", "conflict",
            null, LogLevel.Debug),
        DomainException de => (HttpStatusCode.BadRequest, "Bad Request", de.Code,
            null, LogLevel.Debug),
        System.UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized",
            "unauthorized", null, LogLevel.Debug),
        _ => (HttpStatusCode.InternalServerError, "Server Error", "server_error",
            null, LogLevel.Error),
    };

    private static string TypeFor(HttpStatusCode status) => status switch
    {
        HttpStatusCode.BadRequest => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1",
        HttpStatusCode.Unauthorized => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.2",
        HttpStatusCode.Forbidden => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.4",
        HttpStatusCode.NotFound => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5",
        HttpStatusCode.Conflict => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.10",
        _ => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
    };
}
