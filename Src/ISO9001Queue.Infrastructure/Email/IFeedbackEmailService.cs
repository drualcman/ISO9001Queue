namespace ISO9001Queue.Infrastructure.Email;

public interface IFeedbackEmailService
{
    Task SendThankYouAsync(CustomerFeedbackQueueMessage message, CancellationToken cancellationToken = default);
    Task SendLowRatingAlertAsync(CustomerFeedbackQueueMessage message, CancellationToken cancellationToken = default);
}
