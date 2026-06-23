using System.Globalization;
using System.Resources;

namespace ISO9001Queue.Infrastructure.Email;

internal sealed class FeedbackEmailService(
    IEmailSender emailSender,
    IOptions<EmailOptions> emailOptions,
    ILogger<FeedbackEmailService> logger) : IFeedbackEmailService
{
    private static readonly ResourceManager Resources = new(
        "ISO9001Queue.Infrastructure.Email.Resources.FeedbackEmailResource",
        typeof(FeedbackEmailService).Assembly);

    public async Task SendThankYouAsync(CustomerFeedbackQueueMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            (string language, CultureInfo culture) = MailTemplates.ResolveLanguage(message.Language);
            string Text(string key) => Resources.GetString(key, culture) ?? Resources.GetString(key, CultureInfo.InvariantCulture) ?? key;

            string receiverName = string.IsNullOrWhiteSpace(message.CustomerName) ? Text("DefaultReceiverName") : message.CustomerName;
            string stars = Stars(message.Rating);
            string bodyFragment = $"""
                <p style="margin:0 0 16px;">{string.Format(Text("Greeting"), receiverName)}</p>
                <p style="margin:0 0 16px;">{string.Format(Text("ThankYouIntro"), message.Rating, stars)}</p>
                {(string.IsNullOrWhiteSpace(message.Comments) ? string.Empty : $"""
                <p style="margin:0 0 4px;">{Text("YourComments")}</p>
                <p style="margin:0 0 16px;"><em>{message.Comments}</em></p>
                """)}
                <p style="margin:0 0 24px;">{Text("ThankYouOutro")}</p>
                <hr style="border:none;border-top:1px solid #e1e4e8;margin:0 0 20px;"/>
                <p style="margin:0;">
                    {Text("Regards")}<br/>
                    <strong>{Text("Signature")}</strong>
                </p>
                """;

            string subject = $"[{message.CompanyId}] {Text("ThankYouSubject")}";
            string body = MailTemplates.GetEmailTemplate(bodyFragment, message.CompanyId, Text("ThankYouSubject"),
                language, message.CustomerAntiPhishing, Text("Footer"));

            await emailSender.SendAsync(message.EmailCompanyId, subject, receiverName, message.CustomerEmail, message.CustomerAntiPhishing,
                language, body, cancellationToken: cancellationToken);
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
            string stars = Stars(message.Rating);
            string bodyFragment = $"""
                <p style="margin:0 0 16px;"><strong>&#9888;&#65039; Low Customer Rating Alert</strong></p>
                <p style="margin:0 0 16px;">A customer has submitted a low rating that requires your attention:</p>
                <ul style="margin:0 0 16px;line-height:1.8;">
                    <li><strong>Customer:</strong> {message.CustomerName} ({message.CustomerEmail})</li>
                    <li><strong>Rating:</strong> {message.Rating}/5 ({stars})</li>
                    <li><strong>Entity:</strong> {message.EntityId}</li>
                    <li><strong>Date:</strong> {message.ReportedAt:yyyy-MM-dd HH:mm} UTC</li>
                    {(string.IsNullOrWhiteSpace(message.Comments) ? string.Empty : $"<li><strong>Comments:</strong> {message.Comments}</li>")}
                </ul>
                <p style="margin:0;">Please review this feedback and take appropriate corrective action.</p>
                """;

            string subject = $"[Quality Alert] Low rating ({message.Rating}/5) from {message.CustomerName}";
            string body = MailTemplates.GetEmailTemplate(bodyFragment, message.CompanyId, "Low Customer Rating Alert",
                message.Language, footerText: "Internal quality alert generated automatically from customer feedback.");

            await emailSender.SendAsync(message.EmailCompanyId, subject, emailOptions.Value.AdminName, adminEmail, string.Empty,
                message.Language, body, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send low-rating alert email for feedback on entity {EntityId}", message.EntityId);
        }
    }

    private static string Stars(int rating) =>
        new string('★', Math.Clamp(rating, 0, 5)) + new string('☆', 5 - Math.Clamp(rating, 0, 5));
}
