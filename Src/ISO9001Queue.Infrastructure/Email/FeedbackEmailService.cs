namespace ISO9001Queue.Infrastructure.Email;

internal sealed class FeedbackEmailService(
    IHttpClientFactory httpClientFactory,
    IOptions<EmailOptions> emailOptions,
    ILogger<FeedbackEmailService> logger) : IFeedbackEmailService
{
    public async Task SendThankYouAsync(CustomerFeedbackQueueMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            string stars = new string('★', message.Rating) + new string('☆', 5 - message.Rating);
            string body = $"""
                Dear {message.CustomerName},<br/><br/>
                Thank you for your feedback! We have received your rating of <strong>{message.Rating}/5</strong> ({stars}).<br/><br/>
                {(string.IsNullOrWhiteSpace(message.Comments) ? string.Empty : $"Your comments: <em>{message.Comments}</em><br/><br/>")}
                Your opinion helps us continuously improve our service.<br/><br/>
                Best regards,<br/>
                The Quality Team
                """;

            await SendEmailAsync(
                recipientName: message.CustomerName,
                recipientEmail: message.CustomerEmail,
                antiPhishing: message.CustomerAntiPhishing,
                subject: "Thank you for your feedback",
                body: body,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send thank-you email to {Email} for feedback on entity {EntityId}",
                message.CustomerEmail, message.EntityId);
        }
    }

    public async Task SendLowRatingAlertAsync(CustomerFeedbackQueueMessage message, CancellationToken cancellationToken = default)
    {
        string adminEmail = emailOptions.Value.AdminEmail;
        if (string.IsNullOrWhiteSpace(adminEmail))
            return;

        try
        {
            string stars = new string('★', message.Rating) + new string('☆', 5 - message.Rating);
            string body = $"""
                <strong>⚠️ Low Customer Rating Alert</strong><br/><br/>
                A customer has submitted a low rating that requires your attention:<br/><br/>
                <ul>
                  <li><strong>Customer:</strong> {message.CustomerName} ({message.CustomerEmail})</li>
                  <li><strong>Rating:</strong> {message.Rating}/5 ({stars})</li>
                  <li><strong>Entity:</strong> {message.EntityId}</li>
                  <li><strong>Date:</strong> {message.ReportedAt:yyyy-MM-dd HH:mm} UTC</li>
                  {(string.IsNullOrWhiteSpace(message.Comments) ? string.Empty : $"<li><strong>Comments:</strong> {message.Comments}</li>")}
                </ul>
                Please review this feedback and take appropriate corrective action.
                """;

            await SendEmailAsync(
                recipientName: emailOptions.Value.AdminName,
                recipientEmail: adminEmail,
                antiPhishing: string.Empty,
                subject: $"[Quality Alert] Low rating ({message.Rating}/5) from {message.CustomerName}",
                body: body,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send low-rating alert email for feedback on entity {EntityId}", message.EntityId);
        }
    }

    private async Task SendEmailAsync(string recipientName, string recipientEmail,
        string antiPhishing, string subject, string body, CancellationToken cancellationToken)
    {
        using HttpClient client = httpClientFactory.CreateClient(nameof(FeedbackEmailService));

        var payload = new
        {
            Subject = subject,
            CompanyId = emailOptions.Value.CompanyId,
            Recipients = new[] { new { DisplayName = recipientName, Adressee = recipientEmail } },
            Content = body,
            AntiPhishing = antiPhishing ?? string.Empty,
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("send-mail", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Email API returned {StatusCode} for recipient {Email}", response.StatusCode, recipientEmail);
        }
    }
}
