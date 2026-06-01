namespace ISO9001Queue.Database.EF.Entities;

internal sealed class NonConformityEntity
{
    public Guid Id { get; set; }
    public string EntityId { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public string AffectedProcess { get; set; } = string.Empty;
    public string Cause { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<NonConformityDetailEntity> Details { get; set; } = [];
}
