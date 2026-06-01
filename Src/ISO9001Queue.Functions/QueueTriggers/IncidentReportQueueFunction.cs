namespace ISO9001Queue.Functions.QueueTriggers;

internal sealed class IncidentReportQueueFunction(
    IRegisterIncidentReport registerIncidentReport,
    ILogger<IncidentReportQueueFunction> logger)
{
    [Function("iso9001-incident")]
    public async Task Run([QueueTrigger(Iso9001QueueNames.Incidents, Connection = "Blob")] string message)
    {
        logger.LogInformation("Processing incident report message");
        try
        {
            IncidentReportQueueMessage? msg = JsonSerializer.Deserialize<IncidentReportQueueMessage>(message);
            if (msg is null)
            { logger.LogWarning("Null incident report message received"); return; }

            await registerIncidentReport.HandleAsync(new IncidentReportDto(
                msg.CompanyId,
                msg.Reference,
                msg.ReportedAt,
                msg.UserId,
                msg.Description,
                msg.AffectedProcess,
                msg.Severity,
                string.IsNullOrWhiteSpace(msg.Exception) ? msg.Data : $"{msg.Data}\n\nException:\n{msg.Exception}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing incident report message: {Message}", message);
            throw;
        }
    }
}
