namespace ISO9001Queue.Functions.QueueTriggers;

internal sealed class AuditLogQueueFunction(
    IRegisterAuditLog registerAuditLog,
    ILogger<AuditLogQueueFunction> logger)
{
    [Function("iso9001-auditlog")]
    public async Task Run([QueueTrigger(Iso9001QueueNames.AuditLogs, Connection = "Blob")] string message)
    {
        logger.LogInformation("Processing audit log message");
        try
        {
            AuditLogQueueMessage? msg = QueueMessageSerializer.Deserialize<AuditLogQueueMessage>(message);
            if (msg is null)
            { logger.LogWarning("Null audit log message received"); return; }

            await registerAuditLog.HandleAsync(new AuditLogDto(
                msg.Reference,
                msg.CompanyId,
                msg.Action,
                msg.PerformedBy,
                msg.Timestamp,
                msg.Description,
                msg.Data));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing audit log message: {Message}", message);
            throw;
        }
    }
}
