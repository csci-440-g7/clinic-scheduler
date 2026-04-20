namespace ClinicScheduler.Core.Entities;

public enum AuditAction
{
    Created,
    Modified,
    Deleted
}

public class AuditLog
{
    public int Id { get; private set; }
    public string EntityName { get; private set; } = null!;
    public string EntityId { get; private set; } = null!;
    public AuditAction Action { get; private set; }
    public string? ChangeSummary { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string? UserId { get; private set; }

    private AuditLog() { }

    public AuditLog(string entityName, string entityId, AuditAction action, string? changeSummary = null, string? userId = null)
    {
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        ChangeSummary = changeSummary;
        Timestamp = DateTime.UtcNow;
        UserId = userId;
    }
}
