namespace ISO9001Queue.Infrastructure.Email;

public record EmailAttachment(string Name, byte[] Bytes);

/// <summary>Sends emails through the shared messaging API configured in EmailOptions. Throws on failure.</summary>
public interface IEmailSender
{
    Task SendAsync(int companyId, string subject, string receiverName, string receiverEmail, string antiPhishing,
        string language, string htmlBody, IReadOnlyList<EmailAttachment>? attachments = null,
        CancellationToken cancellationToken = default);
}
