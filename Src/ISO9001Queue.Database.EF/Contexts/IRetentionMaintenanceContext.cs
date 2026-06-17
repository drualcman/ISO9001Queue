namespace ISO9001Queue.Database.EF.Contexts;

/// <summary>
/// Storage maintenance operations used by the scheduled retention job to keep the
/// database small: clears stale debug <c>Data</c> blobs and deletes expired records.
/// </summary>
public interface IRetentionMaintenanceContext
{
    /// <summary>Clears the debug <c>Data</c> of audit logs created before <paramref name="olderThanUtc"/>.</summary>
    Task<int> ClearAuditLogDataAsync(DateTime olderThanUtc, CancellationToken cancellationToken = default);

    /// <summary>Clears the debug <c>Data</c> of incident reports created before <paramref name="olderThanUtc"/>.</summary>
    Task<int> ClearIncidentReportDataAsync(DateTime olderThanUtc, CancellationToken cancellationToken = default);

    /// <summary>Deletes every record created before <paramref name="olderThanUtc"/> across all tables. Returns rows removed.</summary>
    Task<int> DeleteExpiredRecordsAsync(DateTime olderThanUtc, CancellationToken cancellationToken = default);
}
