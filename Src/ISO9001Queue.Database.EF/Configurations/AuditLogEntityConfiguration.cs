namespace ISO9001Queue.Database.EF.Configurations;

internal sealed class AuditLogEntityConfiguration : IEntityTypeConfiguration<AuditLogEntity>
{
    public void Configure(EntityTypeBuilder<AuditLogEntity> builder)
    {
        builder.ToTable("Iso9001AuditLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.EntityId).IsRequired().HasMaxLength(512);
        builder.Property(x => x.CompanyId).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Action).IsRequired().HasMaxLength(512);
        builder.Property(x => x.PerformedBy).HasMaxLength(256);
        builder.Property(x => x.Timestamp).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.Details).HasMaxLength(2048);
        builder.Property(x => x.Data).HasMaxLength(4000);
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.EntityId);
    }
}
