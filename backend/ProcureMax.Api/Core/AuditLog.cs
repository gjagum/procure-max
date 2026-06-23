using System.Data;
using System.Text.Json;
using Dapper;

namespace ProcureMax.Core;

// Append-only audit trail. before/after snapshots are optional JSON blobs.
public static class AuditLog
{
    public static async Task WriteAsync(
        IDbConnection conn,
        IDbTransaction? tx,
        string userId,
        string action,
        string entity,
        string entityId,
        object? before = null,
        object? after = null)
    {
        var id = Guid.NewGuid().ToString("N");
        await conn.ExecuteAsync("""
            INSERT INTO audit_logs (id, user_id, action, entity, entity_id, before_json, after_json, at)
            VALUES (@Id, @UserId, @Action, @Entity, @EntityId, @Before, @After, @At);
            """, new
        {
            Id = id,
            UserId = userId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Before = before is null ? null : JsonSerializer.Serialize(before),
            After = after is null ? null : JsonSerializer.Serialize(after),
            At = DateTime.UtcNow.ToString("O"),
        }, tx);
    }
}
