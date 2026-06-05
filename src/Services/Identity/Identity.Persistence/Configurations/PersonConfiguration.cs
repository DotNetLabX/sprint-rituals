using Identity.Domain.Enums;

namespace Identity.Persistence.Configurations;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        builder.Property(p => p.FirstName).IsRequired().HasMaxLength(256);
        builder.Property(p => p.LastName).IsRequired().HasMaxLength(256);

        // FullName is computed — not stored
        builder.Ignore(p => p.FullName);

        builder.Property(p => p.Email).IsRequired().HasMaxLength(256).UseCollation("NOCASE");
        builder.HasIndex(p => p.Email)
            .IsUnique()
            .HasDatabaseName("IX_Persons_Email");

        builder.Property(p => p.PictureUrl).HasMaxLength(1024);

        builder.Property(p => p.Source)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(p => p.ExternalId).HasMaxLength(256);
        builder.HasIndex(p => p.ExternalId).HasDatabaseName("IX_Persons_ExternalId");

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UserId);
    }
}
