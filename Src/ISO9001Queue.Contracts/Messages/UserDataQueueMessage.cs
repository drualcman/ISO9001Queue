namespace ISO9001Queue.Contracts.Messages;

/// <summary>
/// Request to collect all data the ISO9001 system holds about a subject (matched by any of
/// the given identifiers, e.g. user id, email, customer id) and email it to the receiver.
/// CompanyName is the display name shown in the email subject and signature;
/// Language is the receiver's preferred email language (en, es, fil).
/// </summary>
public record UserDataQueueMessage(
    string CompanyId,
    string CompanyName,
    IReadOnlyList<string> Identifiers,
    string ReceiverName,
    string ReceiverEmail,
    string ReceiverAntiPhishing,
    string Language);
