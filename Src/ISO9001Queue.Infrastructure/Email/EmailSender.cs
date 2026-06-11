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
            logger.LogError("Email API returned {StatusCode} sending \"{Subject}\" to {Email}",
                response.StatusCode, subject, receiverEmail);
            response.EnsureSuccessStatusCode();
        }
    }
}
