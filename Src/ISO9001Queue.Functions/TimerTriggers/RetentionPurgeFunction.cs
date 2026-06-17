using ISO9001Queue.Database.EF.Contexts;
using ISO9001Queue.Database.EF.Options;
using Microsoft.Extensions.Options;

namespace ISO9001Queue.Functions.TimerTriggers;

internal sealed class RetentionPurgeFunction(
    IRetentionMaintenanceContext maintenance,
    IOptions<RetentionOptions> options,
    ILogger<RetentionPurgeFunction> logger)
{
    // Daily at 03:00 UTC. NCRONTAB: {second} {minute} {hour} {day} {month} {day-of-week}.
    [Function("iso9001-retention-purge")]
    public async Task Run([TimerTrigger("0 0 3 * * *")] TimerInfo timer, CancellationToken cancellationToken)
    {
        RetentionOptions opts = options.Value;
        DateTime now = DateTime.UtcNow;
        DateTime dataThreshold = now.AddDays(-opts.DataRetentionDays);
        DateTime recordThreshold = now.AddMonths(-opts.RecordRetentionMonths);

        logger.LogInformation(
            "Retention purge started. Clear Data older than {DataThreshold:u} ({DataDays}d); delete records older than {RecordThreshold:u} ({RecordMonths}m).",
            dataThreshold, opts.DataRetentionDays, recordThreshold, opts.RecordRetentionMonths);

        try
        {
            int deleted = await maintenance.DeleteExpiredRecordsAsync(recordThreshold, cancellationToken);
            int auditCleared = await maintenance.ClearAuditLogDataAsync(dataThreshold, cancellationToken);
            int incidentCleared = await maintenance.ClearIncidentReportDataAsync(dataThreshold, cancellationToken);

            logger.LogInformation(
                "Retention purge finished. Records deleted: {Deleted}. Audit Data cleared: {AuditCleared}. Incident Data cleared: {IncidentCleared}.",
                deleted, auditCleared, incidentCleared);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Retention purge failed");
            throw;
        }
    }
}
