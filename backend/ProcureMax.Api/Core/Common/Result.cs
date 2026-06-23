namespace ProcureMax.Core.Common;

// Unified service-level result. Endpoint middleware maps to HTTP in Program.cs.
public class Result
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string? Code { get; init; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; init; }

    public static Result Ok() => new() { Success = true };
    public static Result Fail(string error, string code = "error")
        => new() { Success = false, Error = error, Code = code };
    public static Result Invalid(IReadOnlyDictionary<string, string[]> errors)
        => new() { Success = false, ValidationErrors = errors, Code = "validation" };
}

public class Result<T> : Result
{
    public T? Data { get; init; }

    public static Result<T> Ok(T data) => new() { Success = true, Data = data };
    public new static Result<T> Fail(string error, string code = "error")
        => new() { Success = false, Error = error, Code = code };
    public new static Result<T> Invalid(IReadOnlyDictionary<string, string[]> errors)
        => new() { Success = false, ValidationErrors = errors, Code = "validation" };
}

public class PageResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)Total / PageSize);
}

public class PageQuery
{
    // Nullable so [AsParameters] binding doesn't require them in the query string.
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public string? Search { get; set; }

    public int PageValue => Page ?? 1;
    public int PageSizeValue => Math.Clamp(PageSize ?? 20, 1, 200);
    public int Offset => (Math.Max(PageValue, 1) - 1) * PageSizeValue;
}
