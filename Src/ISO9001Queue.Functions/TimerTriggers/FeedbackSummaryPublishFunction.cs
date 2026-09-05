namespace ISO9001Queue.Functions.TimerTriggers;

internal sealed class FeedbackSummaryPublishFunction(
    IFeedbackSummaryPublisher publisher,
    ILogger<FeedbackSummaryPublishFunction> logger)
{
    // Daily at 03:00 UTC, plus RunOnStartup so every tenant's summary blob is (re)seeded on deploy — the
    // write path already refreshes each on every new feedback; this is the backstop / initial seed.
    [Function("iso9001-feedback-summary-publish")]
    public async Task Run([TimerTrigger("0 0 3 * * *", RunOnStartup = true)] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        try
        {
            await publisher.PublishAllAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Scheduled feedback summary publish failed");
        }
    }
}
