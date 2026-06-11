using System.Globalization;
using System.Resources;

namespace ISO9001Queue.Infrastructure.Email;

internal sealed class UserDataEmailService(
    IHttpClientFactory httpClientFactory,
    IOptions<EmailOptions> emailOptions,
    ILogger<UserDataEmailService> logger) : IUserDataEmailService
{
    private static readonly ResourceManager Resources = new(
        "ISO9001Queue.Infrastructure.Email.Resources.UserDataEmailResource",
        typeof(UserDataEmailService).Assembly);

    public async Task SendUserDataAsync(UserDataQueueMessage message, byte[] jsonData, CancellationToken cancellationToken = default)
    {
        (string language, CultureInfo culture) = ResolveLanguage(message.Language);
        string Text(string key) => Resources.GetString(key, culture) ?? Resources.GetString(key, CultureInfo.InvariantCulture) ?? key;

        string companyName = string.IsNullOrWhiteSpace(message.CompanyName) ? message.CompanyId : message.CompanyName;
        string receiverName = string.IsNullOrWhiteSpace(message.ReceiverName) ? Text("DefaultReceiverName") : message.ReceiverName;
        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        string filename = $"quality-data-{timestamp}.json";
        string subject = $"[{companyName}] {Text("Subject")}";
        string body = BuildHtmlDocument(Text, language, companyName, receiverName, message.ReceiverAntiPhishing);

        using HttpClient client = httpClientFactory.CreateClient(nameof(UserDataEmailService));
        var payload = new
        {
            Subject = subject,
            CompanyId = emailOptions.Value.CompanyId,
            Recipients = new[] { new { DisplayName = receiverName, Adressee = message.ReceiverEmail } },
            Content = body,
            AntiPhishing = message.ReceiverAntiPhishing ?? string.Empty,
            Language = language,
            Attachments = new[] { new { Name = filename, Bytes = jsonData } }
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("send-mail", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Email API returned {StatusCode} sending user data export to {Email}",
                response.StatusCode, message.ReceiverEmail);
            // Unlike courtesy emails, a data export must reach the user: fail so the queue retries.
            response.EnsureSuccessStatusCode();
        }
    }

    private static (string Language, CultureInfo Culture) ResolveLanguage(string? language)
    {
        string code = (language ?? string.Empty).Trim().ToLowerInvariant();
        if (code.StartsWith("es")) return ("es", CultureInfo.GetCultureInfo("es"));
        if (code.StartsWith("fil") || code.StartsWith("tl")) return ("fil", CultureInfo.GetCultureInfo("fil"));
        return ("en", CultureInfo.InvariantCulture);
    }

    private static string BuildHtmlDocument(Func<string, string> text, string language,
        string companyName, string receiverName, string antiPhishing)
    {
        int year = DateTime.UtcNow.Year;
        return $$"""
        <!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
        <html xmlns="http://www.w3.org/1999/xhtml" lang="{{language}}" style="color-scheme:light;">
        <head>
            <meta http-equiv="Content-Type" content="text/html; charset=UTF-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <meta name="color-scheme" content="light" />
            <meta name="supported-color-schemes" content="light" />
            <title>{{companyName}}</title>
            <style type="text/css">
                :root { color-scheme: light only; }
                body  { margin:0; padding:0; background-color:#f4f5f7; color:#2d3436; }
            </style>
        </head>
        <body style="margin:0;padding:0;background-color:#f4f5f7;color:#2d3436;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;color-scheme:light;">

            <!-- Outer wrapper -->
            <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%" bgcolor="#f4f5f7"
                   style="border-collapse:collapse;background-color:#f4f5f7;">
                <tr>
                    <td align="center" bgcolor="#f4f5f7" style="padding:40px 16px 48px 16px;background-color:#f4f5f7;">

                        <!-- CARD -->
                        <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="600" bgcolor="#ffffff"
                               style="border-collapse:collapse;background-color:#ffffff;border-radius:10px;overflow:hidden;box-shadow:0 2px 8px rgba(44,62,80,0.12);">

                            <!-- HEADER -->
                            <tr>
                                <td bgcolor="#2c3e50"
                                    style="background-color:#2c3e50;background-image:linear-gradient(135deg,#2c3e50 0%,#4a6584 100%);padding:28px 36px;">
                                    <span style="display:inline-block;background-color:rgba(255,255,255,0.15);border-radius:6px;color:#ffffff;font-size:12px;letter-spacing:1px;padding:4px 10px;text-transform:uppercase;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">{{companyName}}</span>
                                    <h1 style="color:#ffffff;font-size:22px;font-weight:600;margin:14px 0 0;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">{{text("Subject")}}</h1>
                                </td>
                            </tr>

                            <!-- ACCENT LINE -->
                            <tr>
                                <td bgcolor="#4a6584"
                                    style="font-size:0;line-height:0;height:3px;background-color:#4a6584;background-image:linear-gradient(90deg,#2c3e50 0%,#4a6584 50%,#8aa6c4 100%);">
                                    &nbsp;
                                </td>
                            </tr>

                            <!-- BODY -->
                            <tr>
                                <td bgcolor="#ffffff"
                                    style="padding:36px 36px 32px;color:#2d3436;font-size:15px;line-height:1.7;text-align:left;background-color:#ffffff;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
                                    <p style="margin:0 0 16px;">{{string.Format(text("Greeting"), receiverName)}}</p>
                                    <p style="margin:0 0 16px;">{{text("Intro")}}</p>
                                    <p style="margin:0 0 8px;">{{text("MayInclude")}}</p>
                                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0"
                                           style="border-collapse:collapse;background-color:#f8f9fb;border-left:4px solid #4a6584;border-radius:6px;margin:0 0 20px;">
                                        <tr>
                                            <td bgcolor="#f8f9fb" style="padding:16px 20px;background-color:#f8f9fb;">
                                                <p style="color:#2d3436;font-size:14px;line-height:2;margin:0;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
                                                    &#128203;&nbsp; {{text("ItemLogs")}}<br/>
                                                    &#9888;&#65039;&nbsp; {{text("ItemIncidents")}}<br/>
                                                    &#11088;&nbsp; {{text("ItemFeedback")}}
                                                </p>
                                            </td>
                                        </tr>
                                    </table>
                                    <p style="margin:0 0 24px;">{{text("Outro")}}</p>
                                    <hr style="border:none;border-top:1px solid #e1e4e8;margin:0 0 20px;"/>
                                    <p style="margin:0;">
                                        {{text("Regards")}}<br/>
                                        <strong>{{string.Format(text("Signature"), companyName)}}</strong>
                                    </p>
                                    {{BuildAntiPhishingBlock(antiPhishing)}}
                                </td>
                            </tr>

                            <!-- BRAND BAR -->
                            <tr>
                                <td bgcolor="#2c3e50"
                                    style="padding:18px 36px;background-color:#2c3e50;background-image:linear-gradient(160deg,#22303f 0%,#2c3e50 100%);">
                                    <p style="margin:0;font-size:11px;color:#ffffff;letter-spacing:0.04em;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
                                        &copy; {{year}} {{companyName}}
                                    </p>
                                </td>
                            </tr>

                        </table>
                        <!-- /CARD -->

                        <!-- LEGAL — outside the card -->
                        <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="600"
                               style="border-collapse:collapse;margin-top:18px;">
                            <tr>
                                <td style="font-size:11px;color:#9ca3af;line-height:1.65;text-align:center;padding:0 8px;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
                                    {{text("Footer")}}
                                </td>
                            </tr>
                        </table>

                    </td>
                </tr>
            </table>

        </body>
        </html>
        """;
    }

    private static string BuildAntiPhishingBlock(string antiPhishing)
    {
        if (string.IsNullOrWhiteSpace(antiPhishing))
            return string.Empty;

        return $"""
        <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%"
               style="border-collapse:collapse;margin-top:32px;font-size:13px;border-radius:6px;overflow:hidden;box-shadow:0 1px 4px rgba(44,62,80,0.15);">
            <tr>
                <td bgcolor="#2c3e50"
                    style="background-color:#2c3e50;background-image:linear-gradient(160deg,#22303f 0%,#2c3e50 100%);color:#ffd866;font-weight:700;font-size:9px;letter-spacing:0.14em;text-transform:uppercase;padding:10px 16px;white-space:nowrap;vertical-align:middle;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
                    &#128737;&#65039;&nbsp; Anti-Phishing
                </td>
                <td bgcolor="#f0f4fa"
                    style="background-color:#f0f4fa;color:#2c3e50;padding:10px 16px;width:100%;vertical-align:middle;border-left:3px solid #4a6584;font-size:13px;line-height:1.5;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
                    {antiPhishing}
                </td>
            </tr>
        </table>
        """;
    }
}
