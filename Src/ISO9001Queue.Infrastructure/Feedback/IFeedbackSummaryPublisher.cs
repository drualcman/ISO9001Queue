namespace ISO9001Queue.Infrastructure.Feedback;

/// <summary>
/// Recomputes the anonymous feedback summary for a company (average, total, latest rated comments)
/// and publishes it as a static JSON blob for direct, low-latency public consumption. Called off the
/// hot path: after a feedback is committed on the queue, and by a scheduled backstop.
/// </summary>
public interface IFeedbackSummaryPublisher
{
    /// <summary>Republishes the summary blob for a single company (used on the feedback write path).</summary>
    Task PublishAsync(string companyId, CancellationToken cancellationToken = default);

    /// <summary>Republishes the summary blob for every company that has feedback (seed / scheduled backstop).</summary>
    Task PublishAllAsync(CancellationToken cancellationToken = default);
}
