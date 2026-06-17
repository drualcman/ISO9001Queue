namespace ISO9001Queue.Database.EF.Configurations;

internal sealed class IncidentReportEntityConfiguration : IEntityTypeConfiguration<IncidentReportEntity>
{
    public void Configure(EntityTypeBuilder<IncidentReportEntity> builder)
    {
        builder.ToTable("Iso9001IncidentReports");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.CompanyId).IsRequired().HasMaxLength(256);
        builder.Property(x => x.EntityId).IsRequired().HasMaxLength(512);
        builder.Property(x => x.ReportedAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(256);
        builder.Property(x => x.AffectedProcess).HasMaxLength(512);
        builder.Property(x => x.Severity).HasMaxLength(64);
        builder.Property(x => x.Description).HasMaxLength(2048);
        builder.Property(x => x.Data).HasMaxLength(4000);
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.EntityId);
    }
}
