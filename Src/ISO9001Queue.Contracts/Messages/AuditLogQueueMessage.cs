namespace ISO9001Queue.Contracts.Messages;

public record AuditLogQueueMessage(
    string Reference,
    string CompanyId,
    string Action,
    string PerformedBy,
    DateTime Timestamp,
    string Description,
    string Data);
