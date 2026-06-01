namespace ISO9001Queue.Functions.HttpTriggers;

public sealed class Iso9001DashboardFunction(IQualityDashBoardQuery dashboardQuery)
{
    [Function("iso9001-dashboard")]
    public async Task<IActionResult> GetDashboard(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "iso9001/dashboard")] HttpRequest req)
    {
        string companyId = req.Query["companyId"].ToString();
        if (string.IsNullOrWhiteSpace(companyId))
            return new BadRequestObjectResult("companyId is required");

        DateTime? from = ParseDate(req.Query["from"]);
        DateTime? end = ParseDate(req.Query["end"]);

        var result = await dashboardQuery.HandleAsync(companyId, from, end);
        return new OkObjectResult(result);
    }

    private static DateTime? ParseDate(Microsoft.Extensions.Primitives.StringValues value)
        => DateTime.TryParse(value, out DateTime dt) ? dt : null;
}
