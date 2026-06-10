namespace Fokus.Persistence.Configurations;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedOnAdd();

        builder.Property(i => i.Email).IsRequired().HasMaxLength(256);
        builder.HasIndex(i => i.Email).HasDatabaseName("IX_Invitations_Email");

        builder.Property(i => i.Role).IsRequired().HasMaxLength(32).HasDefaultValue("Manager");

        builder.Property(i => i.Token).IsRequired().HasMaxLength(128);
        builder.HasIndex(i => i.Token).IsUnique().HasDatabaseName("IX_Invitations_Token");

        builder.Property(i => i.Status).IsRequired().HasMaxLength(32).HasDefaultValue("Pending");

        // Optimistic concurrency: EF appends WHERE ConcurrencyToken=<original> to UPDATEs.
        // A concurrent second redemption of the same token will affect 0 rows →
        // DbUpdateConcurrencyException → caller translates to Used outcome.
        builder.Property(i => i.ConcurrencyToken).IsConcurrencyToken();

        // InvitedByUserId is a cross-module reference to Identity.User — no DB FK constraint.
        // Cross-aggregate/cross-module references are by ID only per architecture guardrails.
        // int is non-nullable by default; no explicit IsRequired() needed (avoids snapshot delta).
        builder.Property(i => i.InvitedByUserId);
    }
}
