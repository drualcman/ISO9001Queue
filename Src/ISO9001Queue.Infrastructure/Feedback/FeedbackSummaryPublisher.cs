using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ISO9001.Core.Entities;
using ISO9001.Core.Interfaces.CustomerFeedbacks;

namespace ISO9001Queue.Infrastructure.Feedback;

internal sealed class FeedbackSummaryPublisher : IFeedbackSummaryPublisher
{
    private readonly IQueryableCustomerFeedbackDataContext _feedback;
    private readonly FeedbackSummaryOptions _options;
    private readonly ILogger<FeedbackSummaryPublisher> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public FeedbackSummaryPublisher(
        IQueryableCustomerFeedbackDataContext feedback,
        IOptions<FeedbackSummaryOptions> options,
        ILogger<FeedbackSummaryPublisher> logger)
    {
        _feedback = feedback;
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync(string companyId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(companyId) || !HasConnectionString())
            return;

        // Load the company's feedback (few rows), then aggregate in memory — the interface only exposes
        // ToListAsync, and this runs off the hot path so a full load is fine.
        IEnumerable<CustomerFeedbackReadModel> companyFeedback = await _feedback.ToListAsync(
            filter: x => x.CompanyId == companyId,
            orderBy: q => q.OrderByDescending(x => x.ReportedAt));

        await UploadSummaryAsync(companyId, companyFeedback.ToList(), cancellationToken);
    }

    public async Task PublishAllAsync(CancellationToken cancellationToken = default)
    {
        if (!HasConnectionString())
            return;

        // One pass over all feedback, grouped by company — republishes every tenant's blob. Runs off the
        // hot path (startup / daily), so a single full load + in-memory grouping is fine.
        IEnumerable<CustomerFeedbackReadModel> all = await _feedback.ToListAsync(
            filter: null,
            orderBy: q => q.OrderByDescending(x => x.ReportedAt));

        // Case-insensitive grouping so a tenant that changed its companyId casing over time still maps
        // to a single blob (blob paths are case-sensitive; SQL matching is not).
        foreach (IGrouping<string, CustomerFeedbackReadModel> group in
            all.GroupBy(x => x.CompanyId, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(group.Key))
                continue;
            await UploadSummaryAsync(group.Key, group.ToList(), cancellationToken);
        }
    }

    // Assumes 'ordered' is already sorted by ReportedAt descending.
    private async Task UploadSummaryAsync(string companyId, List<CustomerFeedbackReadModel> ordered,
        CancellationToken cancellationToken)
    {
        int total = ordered.Count;
        double average = total > 0 ? Math.Round(ordered.Average(x => x.Rating), 2) : 0d;
        List<FeedbackSummaryItemJson> recent = ordered
            .Take(_options.RecentCount)
            .Select(x => new FeedbackSummaryItemJson(x.Rating, x.Comments ?? string.Empty, x.ReportedAt))
            .ToList();

        FeedbackSummaryJson summary = new(average, total, recent);
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(summary, JsonOptions);

        // Lowercase the companyId in the path: blob paths are case-sensitive, so a single canonical
        // casing keeps the URL predictable for every tenant regardless of how they configure it.
        string blobPath = string.Format(_options.BlobPathFormat, companyId.ToLowerInvariant());
        BlobClient blob = new BlobServiceClient(_options.ConnectionString)
            .GetBlobContainerClient(_options.Container)
            .GetBlobClient(blobPath);

        BlobHttpHeaders headers = new() { ContentType = "application/json" };
        using MemoryStream stream = new(payload);
        await blob.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = headers }, cancellationToken);

        _logger.LogInformation(
            "Published feedback summary for {CompanyId}: {Total} ratings, avg {Average}",
            companyId, total, average);
    }

    private bool HasConnectionString()
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            _logger.LogWarning("Feedback summary not published: no blob connection string configured");
            return false;
        }
        return true;
    }

    // Public JSON contract consumed by the landing page (camelCase via Web defaults):
    // { "averageRating": .., "totalCount": .., "recent": [ { "rating": .., "comment": "..", "date": ".." } ] }
    private sealed record FeedbackSummaryJson(double AverageRating, int TotalCount, IReadOnlyList<FeedbackSummaryItemJson> Recent);

    private sealed record FeedbackSummaryItemJson(int Rating, string Comment, DateTime Date);
}
