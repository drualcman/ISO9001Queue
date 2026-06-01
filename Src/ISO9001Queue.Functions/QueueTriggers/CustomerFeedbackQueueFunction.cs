namespace ISO9001Queue.Functions.QueueTriggers;

internal sealed class CustomerFeedbackQueueFunction(
    IRegisterCustomerFeedback registerCustomerFeedback,
    IFeedbackEmailService feedbackEmailService,
    ILogger<CustomerFeedbackQueueFunction> logger)
{
    private const int LowRatingThreshold = 2;

    [Function("iso9001-feedback")]
    public async Task Run([QueueTrigger(Iso9001QueueNames.CustomerFeedbacks, Connection = "Blob")] string message)
    {
        logger.LogInformation("Processing customer feedback message");
        try
        {
            CustomerFeedbackQueueMessage? msg = JsonSerializer.Deserialize<CustomerFeedbackQueueMessage>(message);
            if (msg is null)
            { logger.LogWarning("Null customer feedback message received"); return; }

            await registerCustomerFeedback.HandleAsync(new CustomerFeedbackDto(
                msg.EntityId,
                msg.CompanyId,
                msg.CustomerId,
                msg.Rating,
                msg.Comments,
                msg.ReportedAt));

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
