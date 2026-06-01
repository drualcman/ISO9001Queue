namespace ISO9001Queue.Database.EF.Entities;

internal sealed class IncidentReportEntity
{
    public Guid Id { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AffectedProcess { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
}
