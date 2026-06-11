using System.Globalization;
using System.Resources;

namespace ISO9001Queue.Infrastructure.Email;

internal sealed class UserDataEmailService(IEmailSender emailSender) : IUserDataEmailService
{
    private static readonly ResourceManager Resources = new(
        "ISO9001Queue.Infrastructure.Email.Resources.UserDataEmailResource",
        typeof(UserDataEmailService).Assembly);

    public async Task SendUserDataAsync(UserDataQueueMessage message, byte[] jsonData, CancellationToken cancellationToken = default)
    {
        (string language, CultureInfo culture) = MailTemplates.ResolveLanguage(message.Language);
        string Text(string key) => Resources.GetString(key, culture) ?? Resources.GetString(key, CultureInfo.InvariantCulture) ?? key;

        string companyName = string.IsNullOrWhiteSpace(message.CompanyName) ? message.CompanyId : message.CompanyName;
        string receiverName = string.IsNullOrWhiteSpace(message.ReceiverName) ? Text("DefaultReceiverName") : message.ReceiverName;
        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        string filename = $"quality-data-{timestamp}.json";
        string subject = $"[{companyName}] {Text("Subject")}";

        string bodyFragment = $"""
            <p style="margin:0 0 16px;">{string.Format(Text("Greeting"), receiverName)}</p>
            <p style="margin:0 0 16px;">{Text("Intro")}</p>
            <p style="margin:0 0 8px;">{Text("MayInclude")}</p>
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0"
                   style="border-collapse:collapse;background-color:#f8f9fb;border-left:4px solid #4a6584;border-radius:6px;margin:0 0 20px;">
                <tr>
                    <td bgcolor="#f8f9fb" style="padding:16px 20px;background-color:#f8f9fb;">
                        <p style="color:#2d3436;font-size:14px;line-height:2;margin:0;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
                            &#128203;&nbsp; {Text("ItemLogs")}<br/>
                            &#9888;&#65039;&nbsp; {Text("ItemIncidents")}<br/>
                            &#11088;&nbsp; {Text("ItemFeedback")}
                        </p>
                    </td>
                </tr>
            </table>
            <p style="margin:0 0 24px;">{Text("Outro")}</p>
            <hr style="border:none;border-top:1px solid #e1e4e8;margin:0 0 20px;"/>
            <p style="margin:0;">
                {Text("Regards")}<br/>
                <strong>{string.Format(Text("Signature"), companyName)}</strong>
            </p>
            """;

        string body = MailTemplates.GetEmailTemplate(bodyFragment, companyName, Text("Subject"),
            language, message.ReceiverAntiPhishing, Text("Footer"));

        // EmailSender throws on failure so the queue retries: a data export must reach the user.
        await emailSender.SendAsync(message.EmailCompanyId, subject, receiverName, message.ReceiverEmail, message.ReceiverAntiPhishing,
            language, body, [new EmailAttachment(filename, jsonData)], cancellationToken);
    }
}
