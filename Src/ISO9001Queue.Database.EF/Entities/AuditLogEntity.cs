namespace ISO9001Queue.Database.EF.Entities;

internal sealed class AuditLogEntity
{
    public Guid Id { get; set; }
    public string EntityId { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Details { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
}
