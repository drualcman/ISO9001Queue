namespace ISO9001Queue.Infrastructure.Options;

/// <summary>
/// Where the public, anonymous feedback summary is published as a static JSON blob so external apps
/// (e.g. the ShotUp landing page) can read it directly from storage — no ISO9001 Function call.
/// The connection string falls back to the host's "Blob" setting (the same account the feedback queue
/// trigger uses) when left empty.
/// </summary>
public class FeedbackSummaryOptions
{
    public const string SectionKey = nameof(FeedbackSummaryOptions);

    /// <summary>Blob storage connection string. Empty => falls back to the host "Blob" app setting.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Public container that already serves shared web resources.</summary>
    public string Container { get; set; } = "content";

    /// <summary>
    /// Blob path template for a company's summary; {0} is the companyId. One file per company so ISO9001
    /// stays multi-tenant (it serves several sites) and never couples to any single one.
    /// </summary>
    public string BlobPathFormat { get; set; } = "iso9001/{0}/feedback-summary.json";

    /// <summary>How many recent comments to keep in the published summary.</summary>
    public int RecentCount { get; set; } = 10;
}
