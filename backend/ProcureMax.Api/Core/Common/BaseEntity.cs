namespace ProcureMax.Core.Common;

// Audit & soft-delete mixin applied to all transactional tables.
// Timestamps are UTC ISO-8601 strings in SQLite. IDs are string UUIDs.
public abstract class BaseEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");
    public string? CreatedBy { get; set; }
    public string? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public string RowVersion { get; set; } = Guid.NewGuid().ToString("N"); // optimistic concurrency token
}
