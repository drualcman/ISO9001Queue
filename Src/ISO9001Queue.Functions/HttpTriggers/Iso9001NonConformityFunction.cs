namespace ISO9001Queue.Functions.HttpTriggers;

public sealed class Iso9001NonConformityFunction(
    IAllNonConformitiesQuery allQuery,
    INonConformityByStatusQuery byStatusQuery,
    INonConformityByEntityIdQuery byEntityIdQuery,
    INonConformityByAffectedProcessQuery byProcessQuery,
    IRegisterNonConformity registerNonConformity,
    IRegisterNonConformityDetail registerNonConformityDetail)
{
    [Function("iso9001-ncr-list")]
    public async Task<IActionResult> GetAll(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "iso9001/non-conformities")] HttpRequest req)
    {
        string companyId = req.Query["companyId"].ToString();
        if (string.IsNullOrWhiteSpace(companyId)) return new BadRequestObjectResult("companyId is required");
        DateTime? from = ParseDate(req.Query["from"]);
        DateTime? end = ParseDate(req.Query["end"]);
        return new OkObjectResult(await allQuery.HandleAsync(companyId, from, end));
    }

    [Function("iso9001-ncr-by-status")]
    public async Task<IActionResult> GetByStatus(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "iso9001/non-conformities/status/{status}")] HttpRequest req,
        string status)
    {
        string companyId = req.Query["companyId"].ToString();
        if (string.IsNullOrWhiteSpace(companyId)) return new BadRequestObjectResult("companyId is required");
        DateTime? from = ParseDate(req.Query["from"]);
        DateTime? end = ParseDate(req.Query["end"]);
        return new OkObjectResult(await byStatusQuery.HandleAsync(companyId, status, from, end));
    }

    [Function("iso9001-ncr-by-entity")]
    public async Task<IActionResult> GetByEntity(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "iso9001/non-conformities/entity/{entityId}")] HttpRequest req,
        string entityId)
    {
        string companyId = req.Query["companyId"].ToString();
        if (string.IsNullOrWhiteSpace(companyId)) return new BadRequestObjectResult("companyId is required");
        DateTime? from = ParseDate(req.Query["from"]);
        DateTime? end = ParseDate(req.Query["end"]);
        return new OkObjectResult(await byEntityIdQuery.HandleAsync(companyId, entityId, from, end));
    }

    [Function("iso9001-ncr-by-process")]
    public async Task<IActionResult> GetByProcess(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "iso9001/non-conformities/process/{process}")] HttpRequest req,
        string process)
    {
        string companyId = req.Query["companyId"].ToString();
        if (string.IsNullOrWhiteSpace(companyId)) return new BadRequestObjectResult("companyId is required");
        DateTime? from = ParseDate(req.Query["from"]);
        DateTime? end = ParseDate(req.Query["end"]);
        return new OkObjectResult(await byProcessQuery.HandleAsync(companyId, process, from, end));
    }

    [Function("iso9001-ncr-create")]
    public async Task<IActionResult> Create(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "iso9001/non-conformities")] HttpRequest req)
    {
        ISO9001.Core.Requests.NonConformityRequest? request =
            await req.ReadFromJsonAsync<ISO9001.Core.Requests.NonConformityRequest>();
        if (request is null) return new BadRequestObjectResult("Invalid request body");

        await registerNonConformity.HandleAsync(new NonConformityDto(
            request.EntityId,
            request.CompanyId,
            request.ReportedAt,
            request.ReportedBy,
            request.Description,
            request.AffectedProcess,
            request.Cause,
            request.Status));

        return new StatusCodeResult(201);
    }

    [Function("iso9001-ncr-detail-create")]
    public async Task<IActionResult> CreateDetail(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "iso9001/non-conformities/detail")] HttpRequest req)
    {
        ISO9001.Core.Requests.NonConformityCreateDetailRequest? request =
            await req.ReadFromJsonAsync<ISO9001.Core.Requests.NonConformityCreateDetailRequest>();
        if (request is null) return new BadRequestObjectResult("Invalid request body");

        if (!Guid.TryParse(request.NonConformityId, out Guid ncGuid))
            return new BadRequestObjectResult("NonConformityId must be a valid GUID");

        await registerNonConformityDetail.HandleAsync(new NonConformityCreateDetailDto(
            ncGuid,
            string.Empty,
            request.ReportedAt,
            request.ReportedBy,
            request.Description,
            request.Status));

        return new StatusCodeResult(201);
    }

    private static DateTime? ParseDate(Microsoft.Extensions.Primitives.StringValues value)
        => DateTime.TryParse(value, out DateTime dt) ? dt : null;
}
