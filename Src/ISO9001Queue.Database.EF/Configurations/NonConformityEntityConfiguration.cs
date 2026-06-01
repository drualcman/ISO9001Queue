namespace ISO9001Queue.Database.EF.Configurations;

internal sealed class NonConformityEntityConfiguration : IEntityTypeConfiguration<NonConformityEntity>
{
    public void Configure(EntityTypeBuilder<NonConformityEntity> builder)
    {
        builder.ToTable("Iso9001NonConformities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityId).IsRequired().HasMaxLength(512);
        builder.Property(x => x.CompanyId).IsRequired().HasMaxLength(256);
        builder.Property(x => x.AffectedProcess).HasMaxLength(512);
        builder.Property(x => x.Cause).HasMaxLength(1024);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(64);
        builder.Property(x => x.ReportedAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasMany(x => x.Details)
               .WithOne(x => x.NonConformity)
               .HasForeignKey(x => x.NonConformityId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.Status);
    }
}
