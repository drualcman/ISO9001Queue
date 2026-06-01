namespace ISO9001Queue.Database.EF.Entities;

internal sealed class NonConformityDetailEntity
{
    public int Id { get; set; }
    public Guid NonConformityId { get; set; }
    public string ReportedBy { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public NonConformityEntity NonConformity { get; set; } = null!;
}
