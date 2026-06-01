namespace ISO9001Queue.Functions.HttpTriggers;

public sealed class Iso9001ReportsFunction(
    IGenerateAuditReport auditReport,
    IGenerateAuditLogReport auditLogReport,
    IGenerateNonConformityMasterReport ncrMasterReport,
    IGenerateNonConformityDetailsReport ncrDetailReport,
    IGenerateCustomerFeedbackReport feedbackReport,
    IGenerateIncidentReportReport incidentReport)
{
    [Function("iso9001-report-audit")]
    public async Task<IActionResult> GetAuditReport(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "iso9001/reports/audit")] HttpRequest req)
    {
        string companyId = req.Query["companyId"].ToString();
        if (string.IsNullOrWhiteSpace(companyId)) return new BadRequestObjectResult("companyId is required");
        string? entityId = req.Query["entityId"].ToString();
        DateTime? from = ParseDate(req.Query["from"]);
        DateTime? end = ParseDate(req.Query["end"]);
        var report = await auditReport.HandleAsync(companyId, entityId, from, end);
        return new OkObjectResult(report);
    }

    [Function("iso9001-report-auditlog")]
    public async Task<IActionResult> GetAuditLogReport(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "iso9001/reports/audit-log")] HttpRequest req)
    {
        string companyId = req.Query["companyId"].ToString();
        if (string.IsNullOrWhiteSpace(companyId)) return new BadRequestObjectResult("companyId is required");
        string? entityId = req.Query["entityId"].ToString();
        DateTime? from = ParseDate(req.Query["from"]);
        DateTime? end = ParseDate(req.Query["end"]);
        var report = await auditLogReport.HandleAsync(companyId, entityId, from, end);
        return new OkObjectResult(report);
    }

    [Function("iso9001-report-ncr-master")]
    public async Task<IActionResult> GetNcrMasterReport(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "iso9001/reports/non-conformity/master")] HttpRequest req)
    {
        string companyId = req.Query["companyId"].ToString();
        if (string.IsNullOrWhiteSpace(companyId)) return new BadRequestObjectResult("companyId is required");
        string? entityId = req.Query["entityId"].ToString();
        DateTime? from = ParseDate(req.Query["from"]);
        DateTime? end = ParseDate(req.Query["end"]);
        var report = await ncrMasterReport.HandleAsync(companyId, entityId, from, end);
        return new OkObjectResult(report);
    }

    [Function("iso9001-report-ncr-detail")]
    public async Task<IActionResult> GetNcrDetailReport(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "iso9001/reports/non-conformity/detail")] HttpRequest req)
    {
        string companyId = req.Query["companyId"].ToString();
        string ncId = req.Query["ncId"].ToString();
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(ncId))
            return new BadRequestObjectResult("companyId and ncId are required");
        DateTime? from = ParseDate(req.Query["from"]);
        DateTime? end = ParseDate(req.Query["end"]);
        var report = await ncrDetailReport.HandleAsync(companyId, ncId, from, end);
        return new OkObjectResult(report);
    }

    [Function("iso9001-report-feedback")]
    public async Task<IActionResult> GetFeedbackReport(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "iso9001/reports/feedback")] HttpRequest req)
    {
        string companyId = req.Query["companyId"].ToString();
        if (string.IsNullOrWhiteSpace(companyId)) return new BadRequestObjectResult("companyId is required");
        string? entityId = req.Query["entityId"].ToString();
        DateTime? from = ParseDate(req.Query["from"]);
        DateTime? end = ParseDate(req.Query["end"]);
        var report = await feedbackReport.HandleAsync(companyId, entityId, from, end);
        return new OkObjectResult(report);
    }

    [Function("iso9001-report-incident")]
    public async Task<IActionResult> GetIncidentReport(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "iso9001/reports/incident")] HttpRequest req)
    {
        string companyId = req.Query["companyId"].ToString();
        if (string.IsNullOrWhiteSpace(companyId)) return new BadRequestObjectResult("companyId is required");
        string? entityId = req.Query["entityId"].ToString();
        DateTime? from = ParseDate(req.Query["from"]);
        DateTime? end = ParseDate(req.Query["end"]);
        var report = await incidentReport.HandleAsync(companyId, entityId, from, end);
        return new OkObjectResult(report);
    }

    private static DateTime? ParseDate(Microsoft.Extensions.Primitives.StringValues value)
        => DateTime.TryParse(value, out DateTime dt) ? dt : null;
}
