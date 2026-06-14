
using Reflekt.Domain.Users;

namespace Reflekt.Persistence.EntityConfigurations;

internal sealed class UserConfiguration : EntityConfiguration<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);
        builder.ToTable("Users");
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256).UseCollation("NOCASE");
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("IX_Users_Email");
        builder.Property(u => u.Name).IsRequired().HasMaxLength(256);
        builder.Property(u => u.GivenName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.CreatedAt).IsRequired();
    }
}
