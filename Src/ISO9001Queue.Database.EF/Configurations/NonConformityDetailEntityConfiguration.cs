namespace ISO9001Queue.Database.EF.Configurations;

internal sealed class NonConformityDetailEntityConfiguration : IEntityTypeConfiguration<NonConformityDetailEntity>
{
    public void Configure(EntityTypeBuilder<NonConformityDetailEntity> builder)
    {
        builder.ToTable("Iso9001NonConformityDetails");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.NonConformityId).IsRequired();
        builder.Property(x => x.ReportedBy).HasMaxLength(256);
        builder.Property(x => x.Description).HasMaxLength(2048);
        builder.Property(x => x.Status).HasMaxLength(64);
        builder.Property(x => x.ReportedAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.NonConformityId);
    }
}
