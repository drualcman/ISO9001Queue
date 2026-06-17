namespace ISO9001Queue.Database.EF.Contexts;

public sealed class Iso9001DbContext(IOptions<DatabaseOptions> dbOptions) : DbContext,
    IWritableAuditLogDataContext,
    IQueryableAuditLogDataContext,
    IWritableIncidentReportDataContext,
    IQueryableIncidentReportDataContext,
    IWritableNonConformityDataContext,
    IQueryableNonConformityDataContext,
    IWritableCustomerFeedbackDataContext,
    IQueryableCustomerFeedbackDataContext,
    IRetentionMaintenanceContext
{
    /// <summary>Maximum characters persisted in the debug <c>Data</c> column.</summary>
    private const int MaxDataLength = 4000;

    internal DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();
    internal DbSet<IncidentReportEntity> IncidentReports => Set<IncidentReportEntity>();
    internal DbSet<NonConformityEntity> NonConformities => Set<NonConformityEntity>();
    internal DbSet<NonConformityDetailEntity> NonConformityDetails => Set<NonConformityDetailEntity>();
    internal DbSet<CustomerFeedbackEntity> CustomerFeedbacks => Set<CustomerFeedbackEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string connection = dbOptions.Value.ConnectionString;
        optionsBuilder.UseSqlServer(connection, sql =>
        {
            sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
        });
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AuditLogEntityConfiguration());
        modelBuilder.ApplyConfiguration(new IncidentReportEntityConfiguration());
        modelBuilder.ApplyConfiguration(new NonConformityEntityConfiguration());
        modelBuilder.ApplyConfiguration(new NonConformityDetailEntityConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerFeedbackEntityConfiguration());
        base.OnModelCreating(modelBuilder);
    }

    // ── AuditLog ──────────────────────────────────────────────────────────────

    async Task IWritableAuditLogDataContext.AddAsync(AuditLog auditLog)
    {
        await AuditLogs.AddAsync(new AuditLogEntity
        {
            Id = Guid.NewGuid(),
            EntityId = auditLog.EntityId ?? string.Empty,
            CompanyId = auditLog.CompanyId ?? string.Empty,
            Action = auditLog.Action ?? string.Empty,
            PerformedBy = auditLog.PerformedBy ?? string.Empty,
            Timestamp = auditLog.Timestamp,
            CreatedAt = DateTime.UtcNow,
            Details = auditLog.Details ?? string.Empty,
            Data = Truncate(auditLog.Data),
        });
    }

    Task<IEnumerable<AuditLogReadModel>> IQueryableAuditLogDataContext.ToListAsync(
        Expression<Func<AuditLogReadModel, bool>>? filter,
        Func<IQueryable<AuditLogReadModel>, IOrderedQueryable<AuditLogReadModel>>? orderBy)
        => QueryReadModels(
            AuditLogs.AsNoTracking().Select(e => new AuditLogReadModel
            {
                LogId = e.Id.ToString(),
                EntityId = e.EntityId,
                CompanyId = e.CompanyId,
                Action = e.Action,
                PerformedBy = e.PerformedBy,
                Timestamp = e.Timestamp,
                CreatedAt = e.CreatedAt,
                Details = e.Details,
            }),
            filter, orderBy);

    // ── IncidentReport ────────────────────────────────────────────────────────

    async Task IWritableIncidentReportDataContext.AddAsync(IncidentReport incidentReport)
    {
        await IncidentReports.AddAsync(new IncidentReportEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = incidentReport.CompanyId ?? string.Empty,
            EntityId = incidentReport.EntityId ?? string.Empty,
            ReportedAt = incidentReport.ReportedAt,
            CreatedAt = DateTime.UtcNow,
            UserId = incidentReport.UserId ?? string.Empty,
            Description = incidentReport.Description ?? string.Empty,
            AffectedProcess = incidentReport.AffectedProcess ?? string.Empty,
            Severity = incidentReport.Severity ?? string.Empty,
            Data = Truncate(incidentReport.Data),
        });
    }

    Task<IEnumerable<IncidentReportReadModel>> IQueryableIncidentReportDataContext.ToListAsync(
        Expression<Func<IncidentReportReadModel, bool>>? filter,
        Func<IQueryable<IncidentReportReadModel>, IOrderedQueryable<IncidentReportReadModel>>? orderBy)
        => QueryReadModels(
            IncidentReports.AsNoTracking().Select(e => new IncidentReportReadModel
            {
                Id = e.Id.ToString(),
                CompanyId = e.CompanyId,
                EntityId = e.EntityId,
                ReportedAt = e.ReportedAt,
                CreatedAt = e.CreatedAt,
                UserId = e.UserId,
                Description = e.Description,
                AffectedProcess = e.AffectedProcess,
                Severity = e.Severity,
                Data = e.Data,
            }),
            filter, orderBy);

    // ── NonConformity ─────────────────────────────────────────────────────────

    async Task IWritableNonConformityDataContext.AddNonConformityAsync(NonConformity nonConformityMaster)
    {
        await NonConformities.AddAsync(new NonConformityEntity
        {
            Id = nonConformityMaster.Id == Guid.Empty ? Guid.NewGuid() : nonConformityMaster.Id,
            EntityId = nonConformityMaster.EntityId ?? string.Empty,
            CompanyId = nonConformityMaster.CompanyId ?? string.Empty,
            AffectedProcess = nonConformityMaster.AffectedProcess ?? string.Empty,
            Cause = nonConformityMaster.Cause ?? string.Empty,
            Status = nonConformityMaster.Status ?? string.Empty,
            ReportedAt = nonConformityMaster.ReportedAt,
            CreatedAt = DateTime.UtcNow,
        });
    }

    async Task IWritableNonConformityDataContext.AddNonConformityDetailAsync(NonConformityDetail detail, Guid nonConformityId)
    {
        await NonConformityDetails.AddAsync(new NonConformityDetailEntity
        {
            NonConformityId = nonConformityId,
            ReportedBy = detail.ReportedBy ?? string.Empty,
            Description = detail.Description ?? string.Empty,
            Status = detail.Status ?? string.Empty,
            ReportedAt = detail.ReportedAt,
            CreatedAt = DateTime.UtcNow,
        });
    }

    async Task IWritableNonConformityDataContext.UpdateNonConformityAsync(NonConformityReadModel nonConformity)
    {
        if (!Guid.TryParse(nonConformity.Id, out Guid id))
            return;
        var entity = await NonConformities.FindAsync(id);
        if (entity is null)
            return;
        entity.Status = nonConformity.Status ?? entity.Status;
        entity.AffectedProcess = nonConformity.AffectedProcess ?? entity.AffectedProcess;
        entity.Cause = nonConformity.Cause ?? entity.Cause;
        entity.EntityId = nonConformity.EntityId ?? entity.EntityId;
        entity.CompanyId = nonConformity.CompanyId ?? entity.CompanyId;
    }

    Task<IEnumerable<NonConformityReadModel>> IQueryableNonConformityDataContext.ToNonConformityListAsync(
        Expression<Func<NonConformityReadModel, bool>>? filter,
        Func<IQueryable<NonConformityReadModel>, IOrderedQueryable<NonConformityReadModel>>? orderBy)
        => QueryReadModels(
            NonConformities.AsNoTracking().Select(e => new NonConformityReadModel
            {
                Id = e.Id.ToString(),
                EntityId = e.EntityId,
                CompanyId = e.CompanyId,
                AffectedProcess = e.AffectedProcess,
                Cause = e.Cause,
                Status = e.Status,
                ReportedAt = e.ReportedAt,
                CreatedAt = e.CreatedAt,
            }),
            filter, orderBy);

    Task<IEnumerable<NonConformityDetailReadModel>> IQueryableNonConformityDataContext.ToNonConformityDetailListAsync(
        Expression<Func<NonConformityDetailReadModel, bool>>? filter,
        Func<IQueryable<NonConformityDetailReadModel>, IOrderedQueryable<NonConformityDetailReadModel>>? orderBy)
        => QueryReadModels(
            NonConformityDetails.AsNoTracking().Select(e => new NonConformityDetailReadModel
            {
                Id = e.Id.ToString(),
                NonConformityId = e.NonConformityId.ToString(),
                ReportedBy = e.ReportedBy,
                Description = e.Description,
                Status = e.Status,
                ReportedAt = e.ReportedAt,
                CreatedAt = e.CreatedAt,
            }),
            filter, orderBy);

    // ── CustomerFeedback ──────────────────────────────────────────────────────

    async Task IWritableCustomerFeedbackDataContext.AddAsync(CustomerFeedback customerFeedback)
    {
        await CustomerFeedbacks.AddAsync(new CustomerFeedbackEntity
        {
            EntityId = customerFeedback.EntityId ?? string.Empty,
            CompanyId = customerFeedback.CompanyId ?? string.Empty,
            CustomerId = customerFeedback.CustomerId ?? string.Empty,
            Rating = customerFeedback.Rating,
            Comments = customerFeedback.Comments ?? string.Empty,
            ReportedAt = customerFeedback.ReportedAt,
            CreatedAt = DateTime.UtcNow,
        });
    }

    Task<IEnumerable<CustomerFeedbackReadModel>> IQueryableCustomerFeedbackDataContext.ToListAsync(
        Expression<Func<CustomerFeedbackReadModel, bool>>? filter,
        Func<IQueryable<CustomerFeedbackReadModel>, IOrderedQueryable<CustomerFeedbackReadModel>>? orderBy)
        => QueryReadModels(
            CustomerFeedbacks.AsNoTracking().Select(e => new CustomerFeedbackReadModel
            {
                Id = e.Id.ToString(),
                EntityId = e.EntityId,
                CompanyId = e.CompanyId,
                CustomerId = e.CustomerId,
                Rating = e.Rating,
                Comments = e.Comments,
                ReportedAt = e.ReportedAt,
                CreatedAt = e.CreatedAt,
            }),
            filter, orderBy);

    // ── SaveChanges (shared) ──────────────────────────────────────────────────

    Task IWritableAuditLogDataContext.SaveChangesAsync() => base.SaveChangesAsync();
    Task IWritableIncidentReportDataContext.SaveChangesAsync() => base.SaveChangesAsync();
    Task IWritableNonConformityDataContext.SaveChangesAsync() => base.SaveChangesAsync();
    Task IWritableCustomerFeedbackDataContext.SaveChangesAsync() => base.SaveChangesAsync();

    // ── Retention maintenance ─────────────────────────────────────────────────

    async Task<int> IRetentionMaintenanceContext.ClearAuditLogDataAsync(DateTime olderThanUtc, CancellationToken cancellationToken)
        => await AuditLogs
            .Where(e => e.CreatedAt < olderThanUtc && e.Data != "")
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Data, _ => ""), cancellationToken);

    async Task<int> IRetentionMaintenanceContext.ClearIncidentReportDataAsync(DateTime olderThanUtc, CancellationToken cancellationToken)
        => await IncidentReports
            .Where(e => e.CreatedAt < olderThanUtc && e.Data != "")
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Data, _ => ""), cancellationToken);

    async Task<int> IRetentionMaintenanceContext.DeleteExpiredRecordsAsync(DateTime olderThanUtc, CancellationToken cancellationToken)
    {
        int removed = 0;
        // Details first; NonConformities cascade-delete their details at the DB level too,
        // but deleting orphan-aged details explicitly keeps the operation predictable.
        removed += await NonConformityDetails.Where(e => e.CreatedAt < olderThanUtc).ExecuteDeleteAsync(cancellationToken);
        removed += await NonConformities.Where(e => e.CreatedAt < olderThanUtc).ExecuteDeleteAsync(cancellationToken);
        removed += await AuditLogs.Where(e => e.CreatedAt < olderThanUtc).ExecuteDeleteAsync(cancellationToken);
        removed += await IncidentReports.Where(e => e.CreatedAt < olderThanUtc).ExecuteDeleteAsync(cancellationToken);
        removed += await CustomerFeedbacks.Where(e => e.CreatedAt < olderThanUtc).ExecuteDeleteAsync(cancellationToken);
        return removed;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static string Truncate(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return value.Length <= MaxDataLength ? value : value[..MaxDataLength];
    }

    private static async Task<IEnumerable<T>> QueryReadModels<T>(
        IQueryable<T> source,
        Expression<Func<T, bool>>? filter,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy)
    {
        IQueryable<T> query = source;
        if (filter is not null)
            query = query.Where(filter);
        if (orderBy is not null)
            query = orderBy(query);
        return await query.ToListAsync();
    }
}
