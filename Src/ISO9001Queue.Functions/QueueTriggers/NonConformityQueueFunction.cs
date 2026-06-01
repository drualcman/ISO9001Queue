namespace ISO9001Queue.Functions.QueueTriggers;

internal sealed class NonConformityQueueFunction(
    IRegisterNonConformity registerNonConformity,
    ILogger<NonConformityQueueFunction> logger)
{
    [Function("iso9001-nonconformity")]
    public async Task Run([QueueTrigger(Iso9001QueueNames.NonConformities, Connection = "Blob")] string message)
    {
        logger.LogInformation("Processing non-conformity message");
        try
        {
            NonConformityQueueMessage? msg = JsonSerializer.Deserialize<NonConformityQueueMessage>(message);
            if (msg is null)
            { logger.LogWarning("Null non-conformity message received"); return; }

            await registerNonConformity.HandleAsync(new NonConformityDto(
                msg.EntityId,
                msg.CompanyId,
                msg.ReportedAt,
                msg.ReportedBy,
                msg.Description,
                msg.AffectedProcess,
                msg.Cause,
                msg.Status));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing non-conformity message: {Message}", message);
            throw;
        }
    }
}
