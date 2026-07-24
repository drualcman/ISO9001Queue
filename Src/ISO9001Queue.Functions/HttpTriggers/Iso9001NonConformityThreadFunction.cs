using ISO9001.Core.Entities;

namespace ISO9001Queue.Functions.HttpTriggers;

/// <summary>
/// Conversation-shaped access to non-conformities, for support ticketing.
/// <para>
/// A non-conformity is already a thread: the master holds the subject (<c>Cause</c>) and the
/// details are its messages, ordered by <c>ReportedAt</c>. What ISO9001.Core does not expose is
/// what a ticket system needs, so these endpoints fill the gaps against the data-context
/// interfaces the package does expose:
/// </para>
/// <list type="bullet">
/// <item>listing every thread of one <c>EntityId</c> (the package has the repository method but no query/endpoint);</item>
/// <item>creating a thread and getting its id back (the package's POST returns 201 with no body,
/// and generates the Guid internally, so the caller can never address what it just created);</item>
/// <item>moving the master's status (the package's <c>UpdateStatusNonConformityMasterAsync</c> is
/// unreachable, so masters stay <c>open</c> forever and the dashboard KPI never drops);</item>
/// <item>reading a thread with no implicit date window — the package's by-entity handler defaults
/// to the last 30 days, which silently hides any older ticket.</item>
/// </list>
/// </summary>
public sealed class Iso9001NonConformityThreadFunction(
    IQueryableNonConformityDataContext query,
    IWritableNonConformityDataContext writer)
{
    /// <summary>Every thread opened by one entity (for support: one customer), newest first.</summary>
    [Function("iso9001-ncr-threads-by-entity")]
    public async Task<IActionResult> GetByEntity(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "iso9001/non-conformities/by-entity/{entityId}")] HttpRequest req,
        string entityId)
    {
        string companyId = req.Query["companyId"].ToString();
        if (string.IsNullOrWhiteSpace(companyId))
            return new BadRequestObjectResult("companyId is required");
        if (string.IsNullOrWhiteSpace(entityId))
            return new BadRequestObjectResult("entityId is required");

        DateTime? from = ParseDate(req.Query["from"]);
        DateTime? end = ParseDate(req.Query["end"]);

        IEnumerable<NonConformityReadModel> masters = await query.ToNonConformityListAsync(
            nc => nc.CompanyId == companyId && nc.EntityId == entityId
                  && (from == null || nc.ReportedAt >= from)
                  && (end == null || nc.ReportedAt <= end),
            nc => nc.OrderByDescending(x => x.ReportedAt));

        List<string> ids = masters.Select(m => m.Id).ToList();
        if (ids.Count == 0)
            return new OkObjectResult(Array.Empty<NonConformityThreadSummaryResponse>());

        IEnumerable<NonConformityDetailReadModel> details = await query.ToNonConformityDetailListAsync(
            d => ids.Contains(d.NonConformityId),
            d => d.OrderBy(x => x.ReportedAt));

        Dictionary<string, List<NonConformityDetailReadModel>> byThread = details
            .GroupBy(d => d.NonConformityId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return new OkObjectResult(masters.Select(m => ToSummary(m, byThread)).ToList());
    }

    /// <summary>One thread with all its messages. No implicit date window: a ticket is never "too old".</summary>
    [Function("iso9001-ncr-thread-get")]
    public async Task<IActionResult> GetThread(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "iso9001/non-conformities/thread/{id}")] HttpRequest req,
        string id)
    {
        string companyId = req.Query["companyId"].ToString();
        if (string.IsNullOrWhiteSpace(companyId))
            return new BadRequestObjectResult("companyId is required");
        if (!Guid.TryParse(id, out Guid threadId))
            return new BadRequestObjectResult("id must be a valid GUID");

        NonConformityReadModel? master = await FindMasterAsync(companyId, threadId);
        if (master is null)
            return new NotFoundResult();

        string masterId = master.Id;
        IEnumerable<NonConformityDetailReadModel> details = await query.ToNonConformityDetailListAsync(
            d => d.NonConformityId == masterId,
            d => d.OrderBy(x => x.ReportedAt));

        return new OkObjectResult(new NonConformityThreadResponse(
            master.Id,
            master.EntityId,
            master.CompanyId,
            master.AffectedProcess,
            master.Cause,
            master.Status,
            master.ReportedAt,
            details.Select(d => new NonConformityMessageResponse(
                d.ReportedAt, d.ReportedBy, d.Description, d.Status)).ToList()));
    }

    /// <summary>
    /// Opens a thread and returns its id, so the caller can address it afterwards. The id is
    /// generated here rather than deep inside the package, which is the only way to know it.
    /// </summary>
    [Function("iso9001-ncr-thread-create")]
    public async Task<IActionResult> CreateThread(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "iso9001/non-conformities/thread")] HttpRequest req)
    {
        CreateThreadRequest? request = await req.ReadFromJsonAsync<CreateThreadRequest>();
        if (request is null)
            return new BadRequestObjectResult("Invalid request body");
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return new BadRequestObjectResult("companyId is required");
        if (string.IsNullOrWhiteSpace(request.EntityId))
            return new BadRequestObjectResult("entityId is required");
        if (string.IsNullOrWhiteSpace(request.Description))
            return new BadRequestObjectResult("description is required");

        Guid id = Guid.NewGuid();
        DateTime reportedAt = request.ReportedAt == default ? DateTime.UtcNow : request.ReportedAt;
        string status = Normalize(request.Status, DefaultStatus);

        await writer.AddNonConformityAsync(new NonConformity
        {
            Id = id,
            EntityId = request.EntityId,
            CompanyId = request.CompanyId,
            AffectedProcess = request.AffectedProcess ?? string.Empty,
            Cause = request.Cause ?? string.Empty,
            Status = status,
            ReportedAt = reportedAt,
            NonConformityDetails = [],
        });

        await writer.AddNonConformityDetailAsync(new NonConformityDetail
        {
            ReportedAt = reportedAt,
            ReportedBy = request.ReportedBy ?? string.Empty,
            Description = request.Description,
            Status = status,
        }, id);

        await writer.SaveChangesAsync();

        return new ObjectResult(new CreatedThreadResponse(id.ToString())) { StatusCode = 201 };
    }

    /// <summary>
    /// Appends a message to a thread and moves the master to the message's status, so the thread
    /// state and the dashboard KPI stay in sync with the conversation.
    /// </summary>
    [Function("iso9001-ncr-thread-reply")]
    public async Task<IActionResult> AddMessage(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "iso9001/non-conformities/thread/{id}/messages")] HttpRequest req,
        string id)
    {
        AddMessageRequest? request = await req.ReadFromJsonAsync<AddMessageRequest>();
        if (request is null)
            return new BadRequestObjectResult("Invalid request body");
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return new BadRequestObjectResult("companyId is required");
        if (string.IsNullOrWhiteSpace(request.Description))
            return new BadRequestObjectResult("description is required");
        if (!Guid.TryParse(id, out Guid threadId))
            return new BadRequestObjectResult("id must be a valid GUID");

        // Ownership check before writing: without it a caller could append messages to another
        // tenant's thread just by knowing (or guessing) a GUID.
        NonConformityReadModel? master = await FindMasterAsync(request.CompanyId, threadId);
        if (master is null)
            return new NotFoundResult();

        string status = Normalize(request.Status, master.Status);

        await writer.AddNonConformityDetailAsync(new NonConformityDetail
        {
            ReportedAt = request.ReportedAt == default ? DateTime.UtcNow : request.ReportedAt,
            ReportedBy = request.ReportedBy ?? string.Empty,
            Description = request.Description,
            Status = status,
        }, threadId);

        master.Status = status;
        await writer.UpdateNonConformityAsync(master);
        await writer.SaveChangesAsync();

        return new StatusCodeResult(201);
    }

    /// <summary>Moves the master status without adding a message (e.g. closing a resolved ticket).</summary>
    [Function("iso9001-ncr-thread-status")]
    public async Task<IActionResult> UpdateStatus(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "iso9001/non-conformities/thread/{id}/status")] HttpRequest req,
        string id)
    {
        UpdateThreadStatusRequest? request = await req.ReadFromJsonAsync<UpdateThreadStatusRequest>();
        if (request is null)
            return new BadRequestObjectResult("Invalid request body");
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return new BadRequestObjectResult("companyId is required");
        if (string.IsNullOrWhiteSpace(request.Status))
            return new BadRequestObjectResult("status is required");
        if (!Guid.TryParse(id, out Guid threadId))
            return new BadRequestObjectResult("id must be a valid GUID");

        NonConformityReadModel? master = await FindMasterAsync(request.CompanyId, threadId);
        if (master is null)
            return new NotFoundResult();

        master.Status = Normalize(request.Status, master.Status);
        await writer.UpdateNonConformityAsync(master);
        await writer.SaveChangesAsync();

        return new OkResult();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private const string DefaultStatus = "open";

    private async Task<NonConformityReadModel?> FindMasterAsync(string companyId, Guid id)
    {
        string threadId = id.ToString();
        IEnumerable<NonConformityReadModel> found = await query.ToNonConformityListAsync(
            nc => nc.CompanyId == companyId && nc.Id == threadId);
        return found.FirstOrDefault();
    }

    private static NonConformityThreadSummaryResponse ToSummary(
        NonConformityReadModel master,
        Dictionary<string, List<NonConformityDetailReadModel>> byThread)
    {
        List<NonConformityDetailReadModel> messages = byThread.TryGetValue(master.Id, out List<NonConformityDetailReadModel>? list)
            ? list : [];
        NonConformityDetailReadModel? last = messages.Count > 0 ? messages[^1] : null;

        return new NonConformityThreadSummaryResponse(
            master.Id,
            master.EntityId,
            master.AffectedProcess,
            master.Cause,
            master.Status,
            master.ReportedAt,
            messages.Count,
            last?.ReportedAt ?? master.ReportedAt,
            last?.ReportedBy ?? string.Empty);
    }

    // Statuses are stored lowercase by ISO9001.Core; keep writing them the same way so filtering
    // by status keeps matching regardless of which endpoint created the row.
    private static string Normalize(string? status, string fallback)
        => string.IsNullOrWhiteSpace(status) ? fallback : status.Trim().ToLowerInvariant();

    private static DateTime? ParseDate(Microsoft.Extensions.Primitives.StringValues value)
        => DateTime.TryParse(value, out DateTime dt) ? dt : null;
}

// ── Contracts ─────────────────────────────────────────────────────────────────

public sealed record CreateThreadRequest(
    string EntityId,
    string CompanyId,
    DateTime ReportedAt,
    string? ReportedBy,
    string Description,
    string? AffectedProcess,
    string? Cause,
    string? Status);

public sealed record AddMessageRequest(
    string CompanyId,
    DateTime ReportedAt,
    string? ReportedBy,
    string Description,
    string? Status);

public sealed record UpdateThreadStatusRequest(
    string CompanyId,
    string Status);

public sealed record CreatedThreadResponse(string Id);

public sealed record NonConformityThreadSummaryResponse(
    string Id,
    string EntityId,
    string AffectedProcess,
    string Cause,
    string Status,
    DateTime ReportedAt,
    int MessagesCount,
    DateTime LastMessageAt,
    string LastMessageBy);

public sealed record NonConformityThreadResponse(
    string Id,
    string EntityId,
    string CompanyId,
    string AffectedProcess,
    string Cause,
    string Status,
    DateTime ReportedAt,
    List<NonConformityMessageResponse> Messages);

public sealed record NonConformityMessageResponse(
    DateTime ReportedAt,
    string ReportedBy,
    string Description,
    string Status);
