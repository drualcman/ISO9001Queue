namespace ISO9001Queue.Database.EF.Configurations;

internal sealed class CustomerFeedbackEntityConfiguration : IEntityTypeConfiguration<CustomerFeedbackEntity>
{
    public void Configure(EntityTypeBuilder<CustomerFeedbackEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.EntityId).IsRequired().HasMaxLength(512);
        builder.Property(x => x.CompanyId).IsRequired().HasMaxLength(256);
        builder.Property(x => x.CustomerId).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Rating).IsRequired();
        builder.Property(x => x.Comments).HasMaxLength(4000);
        builder.Property(x => x.ReportedAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.ToTable("Iso9001CustomerFeedbacks", t =>
            t.HasCheckConstraint("CK_CustomerFeedback_Rating", "[Rating] BETWEEN 1 AND 5"));
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.CustomerId);
    }
}
