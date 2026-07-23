namespace ISO9001Queue.Functions.QueueTriggers;

internal sealed class CustomerFeedbackQueueFunction(
    IRegisterCustomerFeedback registerCustomerFeedback,
    IFeedbackEmailService feedbackEmailService,
    IFeedbackSummaryPublisher feedbackSummaryPublisher,
    ILogger<CustomerFeedbackQueueFunction> logger)
{
    private const int LowRatingThreshold = 2;

    [Function("iso9001-feedback")]
    public async Task Run([QueueTrigger(Iso9001QueueNames.CustomerFeedbacks, Connection = "Blob")] string message)
    {
        logger.LogInformation("Processing customer feedback message");
        try
        {
            CustomerFeedbackQueueMessage? msg = QueueMessageSerializer.Deserialize<CustomerFeedbackQueueMessage>(message);
            if (msg is null)
            { logger.LogWarning("Null customer feedback message received"); return; }

            await registerCustomerFeedback.HandleAsync(new CustomerFeedbackDto(
                msg.EntityId,
                msg.CompanyId,
                msg.CustomerId,
                msg.Rating,
                msg.Comments,
                msg.ReportedAt));

            // Refresh the public summary blob the landing page reads. Non-fatal: a failure here must
            // not re-queue the feedback (which would resend the thank-you/low-rating emails).
            try
            {
                await feedbackSummaryPublisher.PublishAsync(msg.CompanyId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish feedback summary for {CompanyId}", msg.CompanyId);
            }

            if (!string.IsNullOrWhiteSpace(msg.CustomerEmail))
            {
                await feedbackEmailService.SendThankYouAsync(msg);

                if (msg.Rating <= LowRatingThreshold)
                    await feedbackEmailService.SendLowRatingAlertAsync(msg);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing customer feedback message: {Message}", message);
            throw;
        }
    }
}
