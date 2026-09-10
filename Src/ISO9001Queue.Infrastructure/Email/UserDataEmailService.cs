using ISO9001Queue.Infrastructure.Blobs;
using System.Globalization;
using System.Resources;

namespace ISO9001Queue.Infrastructure.Email;

internal sealed class UserDataEmailService(
    IEmailSender emailSender,
    IUserDataDownloadStore downloadStore,
    IOptions<EmailOptions> emailOptions,
    ILogger<UserDataEmailService> logger) : IUserDataEmailService
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
        string fileName = $"quality-data-{timestamp}.zip";
        string subject = $"[{companyName}] {Text("Subject")}";

        // El JSON viaja SIEMPRE comprimido: en crudo, el de una cuenta activa no llega a la bandeja
        // del usuario (lo rechaza el proveedor o se pasa del tiempo de espera de la API de correo).
        byte[] archive = UserDataArchiveBuilder.Compress(jsonData, $"quality-data-{timestamp}.json");
        logger.LogInformation("Quality data export packed: {RawBytes} bytes of JSON -> {ZipBytes} bytes zipped",
            jsonData.LongLength, archive.LongLength);

        // El fichero se deja SIEMPRE en el contenedor, quepa o no en el correo: el contenedor lo borra
        // solo a los 7 días, y así el enlace es la red de seguridad para el usuario cuyo servidor de
        // correo le quita los adjuntos. El tamaño sólo decide si además viaja adjunto.
        UserDataDownload download = await downloadStore.PublishAsync(fileName, archive, cancellationToken);
        bool attach = archive.LongLength <= emailOptions.Value.MaxAttachmentBytes;

        string body = BuildBody(Text, companyName, receiverName, language, message.ReceiverAntiPhishing,
            download, attach);
        EmailAttachment[] attachments = attach ? [new EmailAttachment(fileName, archive)] : [];

        // EmailSender throws on failure so the queue retries: a data export must reach the user.
        await emailSender.SendAsync(message.EmailCompanyId, subject, receiverName, message.ReceiverEmail, message.ReceiverAntiPhishing,
            language, body, attachments, cancellationToken);
    }

    private static string BuildBody(Func<string, string> Text, string companyName, string receiverName,
        string language, string antiPhishing, UserDataDownload download, bool attach)
    {
        // Adjunto o no, el enlace va siempre. Lo único que cambia es la frase: cuando el fichero viaja
        // adjunto el enlace es un extra, y cuando no cabía hay que decir por qué no está adjunto.
        string lead = attach
            ? $"{Text("Intro")} {string.Format(Text("LinkAlsoAvailable"), download.Days)}"
            : string.Format(Text("IntroLink"), download.Days);

        string intro = $"""
            <p style="margin:0 0 16px;">{lead}</p>
            <p style="margin:0 0 16px;">
                <a href="{download.Url}" style="color:#4a6584;font-weight:bold;">{Text("DownloadText")}</a>
            </p>
            """;

        string bodyFragment = $"""
            <p style="margin:0 0 16px;">{string.Format(Text("Greeting"), receiverName)}</p>
            {intro}
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

        return MailTemplates.GetEmailTemplate(bodyFragment, companyName, Text("Subject"),
            language, antiPhishing, Text("Footer"));
    }
}
