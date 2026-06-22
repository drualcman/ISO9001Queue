namespace ISO9001Queue.Functions.HttpTriggers;

public sealed class Iso9001AuditLogFunction(
    IAllAuditLogsQuery allLogsQuery,
    IAuditLogsByEntityIdQuery byEntityIdQuery,
    IAuditLogsByActionQuery byActionQuery,
    IAuditEventQuery auditEventQuery)
{
    [Function("iso9001-audit-logs")]
    public async Task<IActionResult> GetAll(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "iso9001/audit-logs")] HttpRequest req)
    {
        string companyId = req.Query["companyId"].ToString();
        if (string.IsNullOrWhiteSpace(companyId))
            return new BadRequestObjectResult("companyId is required");
        DateTime? from = ParseDate(req.Query["from"]);
        DateTime? end = ParseDate(req.Query["end"]);
        return new OkObjectResult(await allLogsQuery.HandleAsync(companyId, from, end));
    }

    [Function("iso9001-audit-logs-by-entity")]
    public async Task<IActionResult> GetByEntity(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "iso9001/audit-logs/entity/{entityId}")] HttpRequest req,
        string entityId)
    {
        string companyId = req.Query["companyId"].ToString();
        if (string.IsNullOrWhiteSpace(companyId))
            return new BadRequestObjectResult("companyId is required");
        DateTime? from = ParseDate(req.Query["from"]);
        DateTime? end = ParseDate(req.Query["end"]);
        return new OkObjectResult(await byEntityIdQuery.HandleAsync(companyId, entityId, from, end));
    }

    [Function("iso9001-audit-logs-by-action")]
    public async Task<IActionResult> GetByAction(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "iso9001/audit-logs/action/{action}")] HttpRequest req,
        string action)
    {
        string companyId = req.Query["companyId"].ToString();
        if (string.IsNullOrWhiteSpace(companyId))
            return new BadRequestObjectResult("companyId is required");
        DateTime? from = ParseDate(req.Query["from"]);
        DateTime? end = ParseDate(req.Query["end"]);
        return new OkObjectResult(await byActionQuery.HandleAsync(companyId, action, from, end));
    }

    [Function("iso9001-audit-events")]
    public async Task<IActionResult> GetAuditEvents(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "iso9001/audit-events")] HttpRequest req)
    {
        string companyId = req.Query["companyId"].ToString();
        string entityId = req.Query["entityId"].ToString();
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(entityId))
            return new BadRequestObjectResult("companyId and entityId are required");
        return new OkObjectResult(await auditEventQuery.HandleAsync(entityId, companyId));
    }

    private static DateTime? ParseDate(Microsoft.Extensions.Primitives.StringValues value)
        => DateTime.TryParse(value, out DateTime dt) ? dt : null;
}
