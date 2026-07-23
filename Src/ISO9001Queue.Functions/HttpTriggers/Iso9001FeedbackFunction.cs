namespace ISO9001Queue.Functions.HttpTriggers;

public sealed class Iso9001FeedbackFunction(
    IAllCustomerFeedbackQuery allQuery,
    ICustomerFeedbackByRatingQuery byRatingQuery,
    ICustomerFeedbackByCustomerIdQuery byCustomerQuery,
    ICustomerFeedbackByEntityIdQuery byEntityQuery,
    IAnalyzeCustomerFeedbackQuery analyzeQuery)
{

    [Function("iso9001-feedback-list")]
    public async Task<IActionResult> GetAll(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "iso9001/feedbacks")] HttpRequest req)
    {
        string companyId = req.Query["companyId"].ToString();
        if (string.IsNullOrWhiteSpace(companyId)) return new BadRequestObjectResult("companyId is required");
        DateTime? from = ParseDate(req.Query["from"]);
        DateTime? end = ParseDate(req.Query["end"]);
        return new OkObjectResult(await allQuery.HandleAsync(companyId, from, end));
    }

    [Function("iso9001-feedback-by-rating")]
    public async Task<IActionResult> GetByRating(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "iso9001/feedbacks/rating/{rating}")] HttpRequest req,
        int rating)
    {
        string companyId = req.Query["companyId"].ToString();
        if (string.IsNullOrWhiteSpace(companyId)) return new BadRequestObjectResult("companyId is required");
        DateTime? from = ParseDate(req.Query["from"]);
        DateTime? end = ParseDate(req.Query["end"]);
        return new OkObjectResult(await byRatingQuery.HandleAsync(companyId, rating, from, end));
    }

    [Function("iso9001-feedback-by-customer")]
    public async Task<IActionResult> GetByCustomer(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "iso9001/feedbacks/customer/{customerId}")] HttpRequest req,
        string customerId)
    {
        string companyId = req.Query["companyId"].ToString();
        if (string.IsNullOrWhiteSpace(companyId)) return new BadRequestObjectResult("companyId is required");
        DateTime? from = ParseDate(req.Query["from"]);
        DateTime? end = ParseDate(req.Query["end"]);
        return new OkObjectResult(await byCustomerQuery.HandleAsync(companyId, customerId, from, end));
    }

    [Function("iso9001-feedback-by-entity")]
    public async Task<IActionResult> GetByEntity(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "iso9001/feedbacks/entity/{entityId}")] HttpRequest req,
        string entityId)
    {
        string companyId = req.Query["companyId"].ToString();
        if (string.IsNullOrWhiteSpace(companyId)) return new BadRequestObjectResult("companyId is required");
        DateTime? from = ParseDate(req.Query["from"]);
        DateTime? end = ParseDate(req.Query["end"]);
        return new OkObjectResult(await byEntityQuery.HandleAsync(companyId, entityId, from, end));
    }

    [Function("iso9001-feedback-analysis")]
    public async Task<IActionResult> Analyze(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "iso9001/feedbacks/analysis")] HttpRequest req)
    {
        string companyId = req.Query["companyId"].ToString();
        if (string.IsNullOrWhiteSpace(companyId)) return new BadRequestObjectResult("companyId is required");
        string entityId = req.Query["entityId"].ToString();
        DateTime? from = ParseDate(req.Query["from"]);
        DateTime? end = ParseDate(req.Query["end"]);
        return new OkObjectResult(await analyzeQuery.HandleAsync(companyId, entityId, from, end));
    }

    private static DateTime? ParseDate(Microsoft.Extensions.Primitives.StringValues value)
        => DateTime.TryParse(value, out DateTime dt) ? dt : null;
}
