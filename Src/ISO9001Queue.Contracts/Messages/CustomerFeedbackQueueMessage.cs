namespace ISO9001Queue.Contracts.Messages;

public record CustomerFeedbackQueueMessage(
    string EntityId,
    string CompanyId,
    string CustomerId,
    string CustomerName,
    string CustomerEmail,
    string CustomerAntiPhishing,
    int EmailCompanyId,
    int Rating,
    string Comments,
    DateTime ReportedAt,
    string Language = "en");
