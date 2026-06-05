using Microsoft.EntityFrameworkCore;

namespace Identity.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedOnAdd();

        builder.Property(u => u.PersonId).IsRequired();
        builder.HasIndex(u => u.PersonId).IsUnique().HasDatabaseName("IX_Users_PersonId");

        // 1:1 Person FK — Restrict delete so we don't cascade-delete Users when Person removed
        builder.HasOne<Person>()
            .WithOne()
            .HasForeignKey<User>(u => u.PersonId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(u => u.Role).IsRequired().HasMaxLength(32).HasDefaultValue("Manager");
        builder.Property(u => u.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(u => u.RegistrationDate).IsRequired();
        builder.Property(u => u.LastLogin);
    }
}
