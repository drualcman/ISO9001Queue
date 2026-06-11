using ISO9001.Core.Responses;

namespace ISO9001Queue.Functions.QueueTriggers;

internal sealed class UserDataQueueFunction(
    IAuditLogsByEntityIdQuery auditLogsQuery,
    IIncidentReportByEntityIdQuery incidentsQuery,
    ICustomerFeedbackByCustomerIdQuery feedbackByCustomerQuery,
    ICustomerFeedbackByEntityIdQuery feedbackByEntityQuery,
    IUserDataEmailService userDataEmailService,
    ILogger<UserDataQueueFunction> logger)
{
    [Function("iso9001-userdata")]
    public async Task Run([QueueTrigger(Iso9001QueueNames.UserData, Connection = "Blob")] string message)
    {
        logger.LogInformation("Processing user data request message");
        try
        {
            UserDataQueueMessage? msg = QueueMessageSerializer.Deserialize<UserDataQueueMessage>(message);
            if (msg is null)
            { logger.LogWarning("Null user data request message received"); return; }
            if (string.IsNullOrWhiteSpace(msg.CompanyId) || string.IsNullOrWhiteSpace(msg.ReceiverEmail))
            { logger.LogWarning("User data request without companyId or receiverEmail discarded"); return; }

            List<string> identifiers = (msg.Identifiers ?? [])
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (identifiers.Count == 0)
            { logger.LogWarning("User data request without identifiers discarded"); return; }

            List<AuditLogResponse> auditLogs = [];
            List<IncidentReportResponse> incidents = [];
            List<CustomerFeedbackResponse> feedbacks = [];
            foreach (string identifier in identifiers)
            {
                auditLogs.AddRange(await auditLogsQuery.HandleAsync(msg.CompanyId, identifier, null, null));
                incidents.AddRange(await incidentsQuery.HandleAsync(msg.CompanyId, identifier, null, null));
                feedbacks.AddRange(await feedbackByCustomerQuery.HandleAsync(msg.CompanyId, identifier, null, null));
                feedbacks.AddRange(await feedbackByEntityQuery.HandleAsync(msg.CompanyId, identifier, null, null));
            }

            var data = new
            {
                Identifiers = identifiers,
                AuditLogs = auditLogs.DistinctBy(l => l.LogId).ToList(),
                Incidents = incidents.DistinctBy(i => (i.EntityId, i.ReportedAt, i.Description)).ToList(),
                Feedbacks = feedbacks.DistinctBy(f => (f.EntityId, f.CustomerId, f.ReportedAt, f.Rating)).ToList()
            };
            byte[] jsonData = JsonSerializer.SerializeToUtf8Bytes(data, new JsonSerializerOptions { WriteIndented = true });

            await userDataEmailService.SendUserDataAsync(msg, jsonData);
            logger.LogInformation("User data export sent to {Email} ({Logs} logs, {Incidents} incidents, {Feedbacks} feedbacks)",
                msg.ReceiverEmail, data.AuditLogs.Count, data.Incidents.Count, data.Feedbacks.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing user data request message: {Message}", message);
            throw;
        }
    }
}
