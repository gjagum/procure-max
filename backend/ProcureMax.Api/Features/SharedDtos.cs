namespace ProcureMax.Features;

// Generic, slim request/response DTOs used by all slices.
// Slice-specific DTOs live alongside their endpoints in Features/<Slice>/.
public record Paged<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize, int TotalPages);

public record IdResponse(string Id);
