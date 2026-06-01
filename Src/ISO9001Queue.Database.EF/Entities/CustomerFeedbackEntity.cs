namespace ISO9001Queue.Database.EF.Entities;

internal sealed class CustomerFeedbackEntity
{
    public int Id { get; set; }
    public string EntityId { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comments { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
