namespace ISO9001Queue.Infrastructure.Email;

internal sealed class EmailSender(
    IHttpClientFactory httpClientFactory,
    ILogger<EmailSender> logger) : IEmailSender
{
    internal const string HttpClientName = nameof(EmailSender);

    public async Task SendAsync(int companyId, string subject, string receiverName, string receiverEmail, string antiPhishing,
        string language, string htmlBody, IReadOnlyList<EmailAttachment>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        using HttpClient client = httpClientFactory.CreateClient(HttpClientName);

        // Sin BaseAddress, PostAsJsonAsync("send-mail") revienta con "An invalid request URI was
        // provided", que no dice nada. Un ajuste que falta se dice con su nombre: es la diferencia
        // entre arreglarlo en un minuto y mirar cinco intentos fallidos sin saber por qué.
        if (client.BaseAddress is null)
            throw new InvalidOperationException(
                "EmailOptions:Url is not configured, so no email can be sent. Set it in the app settings (EmailOptions__Url).");

        long attachmentBytes = (attachments ?? []).Sum(a => a.Bytes?.LongLength ?? 0);
        logger.LogInformation(
            "Sending \"{Subject}\" to {Email} ({Attachments} attachment(s), {AttachmentBytes} bytes, body {BodyLength} chars)",
            subject, receiverEmail, attachments?.Count ?? 0, attachmentBytes, htmlBody?.Length ?? 0);

        var payload = new
        {
            Subject = subject,
            CompanyId = companyId,
            Recipients = new[] { new { DisplayName = receiverName, Adressee = receiverEmail } },
            Content = htmlBody,
            AntiPhishing = antiPhishing ?? string.Empty,
            Language = language,
            Attachments = (attachments ?? []).Select(a => new { a.Name, a.Bytes }).ToArray()
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("send-mail", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            // El cuerpo de la respuesta es lo que dice QUÉ rechazó la API (adjunto demasiado grande,
            // destinatario inválido…). Sin él sólo queda un número de estado y a adivinar.
            string details = await ReadBodyAsync(response, cancellationToken);
            logger.LogError(
                "Email API returned {StatusCode} sending \"{Subject}\" to {Email} ({AttachmentBytes} bytes attached): {Details}",
                response.StatusCode, subject, receiverEmail, attachmentBytes, details);
            response.EnsureSuccessStatusCode();
        }
    }

    private static async Task<string> ReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string result;
        try
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            result = body.Length > 2000 ? body[..2000] : body;
        }
        catch (Exception ex)
        {
            result = $"<could not read the response body: {ex.Message}>";
        }

        return result;
    }
}
