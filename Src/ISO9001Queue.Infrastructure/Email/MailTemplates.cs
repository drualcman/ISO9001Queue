using System.Globalization;

namespace ISO9001Queue.Infrastructure.Email;

/// <summary>
/// Company-agnostic full HTML email document shared by all ISO9001 outgoing emails.
/// The messaging API does not wrap content, so senders must provide a complete document.
/// </summary>
internal static class MailTemplates
{
    /// <summary>Maps a free-form language code to a supported language (en, es, fil, zh, ru) and its culture.</summary>
    public static (string Language, CultureInfo Culture) ResolveLanguage(string? language)
    {
        string code = (language ?? string.Empty).Trim().ToLowerInvariant();
        if (code.StartsWith("es")) return ("es", CultureInfo.GetCultureInfo("es"));
        if (code.StartsWith("fil") || code.StartsWith("tl")) return ("fil", CultureInfo.GetCultureInfo("fil"));
        if (code.StartsWith("zh")) return ("zh", CultureInfo.GetCultureInfo("zh"));
        if (code.StartsWith("ru")) return ("ru", CultureInfo.GetCultureInfo("ru"));
        if (code.StartsWith("fr")) return ("fr", CultureInfo.GetCultureInfo("fr"));
        if (code.StartsWith("ko")) return ("ko", CultureInfo.GetCultureInfo("ko"));
        if (code.StartsWith("it")) return ("it", CultureInfo.GetCultureInfo("it"));
        if (code.StartsWith("th")) return ("th", CultureInfo.GetCultureInfo("th"));
        if (code.StartsWith("de")) return ("de", CultureInfo.GetCultureInfo("de"));
        if (code.StartsWith("id")) return ("id", CultureInfo.GetCultureInfo("id"));
        return ("en", CultureInfo.InvariantCulture);
    }

    /// <summary>Wraps a body fragment in the full document: header card with company name and title,
    /// optional anti-phishing block, brand bar and footer note below the card.</summary>
    public static string GetEmailTemplate(string bodyHtml, string companyName, string title,
        string language, string antiPhishing = "", string footerText = "")
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
                                    <h1 style="color:#ffffff;font-size:22px;font-weight:600;margin:14px 0 0;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">{{title}}</h1>
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
                                    {{bodyHtml}}
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

                        <!-- FOOTER NOTE — outside the card -->
                        <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="600"
                               style="border-collapse:collapse;margin-top:18px;">
                            <tr>
                                <td style="font-size:11px;color:#9ca3af;line-height:1.65;text-align:center;padding:0 8px;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
                                    {{footerText}}
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
