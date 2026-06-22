namespace ISO9001Queue.Functions.HttpTriggers;

public sealed class Iso9001IncidentFunction(
    IAllIncidentReportsQuery allQuery,
    IIncidentReportByEntityIdQuery byEntityQuery)
{
    [Function("iso9001-incidents-list")]
    public async Task<IActionResult> GetAll(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "iso9001/incidents")] HttpRequest req)
    {
        string companyId = req.Query["companyId"].ToString();
        if (string.IsNullOrWhiteSpace(companyId)) return new BadRequestObjectResult("companyId is required");
        DateTime? from = ParseDate(req.Query["from"]);
        DateTime? end = ParseDate(req.Query["end"]);
        return new OkObjectResult(await allQuery.HandleAsync(companyId, from, end));
    }

    [Function("iso9001-incidents-by-entity")]
    public async Task<IActionResult> GetByEntity(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "iso9001/incidents/entity/{entityId}")] HttpRequest req,
        string entityId)
    {
        string companyId = req.Query["companyId"].ToString();
        if (string.IsNullOrWhiteSpace(companyId)) return new BadRequestObjectResult("companyId is required");
        DateTime? from = ParseDate(req.Query["from"]);
        DateTime? end = ParseDate(req.Query["end"]);
        return new OkObjectResult(await byEntityQuery.HandleAsync(companyId, entityId, from, end));
    }

    private static DateTime? ParseDate(Microsoft.Extensions.Primitives.StringValues value)
        => DateTime.TryParse(value, out DateTime dt) ? dt : null;
}
