namespace ISO9001Queue.Contracts.Messages;

public record NonConformityQueueMessage(
    string EntityId,
    string CompanyId,
    DateTime ReportedAt,
    string ReportedBy,
    string Description,
    string AffectedProcess,
    string Cause,
    string Status);
