namespace Fokus.Persistence.Configurations;

public class AppSettingsConfiguration : IEntityTypeConfiguration<AppSettings>
{
    public void Configure(EntityTypeBuilder<AppSettings> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.OwnsOne(s => s.HealthThresholds, b => b.ToJson());
        builder.OwnsOne(s => s.HealthWeights, b => b.ToJson());
        builder.OwnsOne(s => s.QaHealthThresholds, b => b.ToJson());
        builder.OwnsOne(s => s.QualitySubScoreWeights, b => b.ToJson());

        builder.ComplexProperty(s => s.BugRatio, b =>
        {
            b.Property(v => v.AlertThreshold).HasDefaultValue(50);
            b.Property(v => v.ConsecutiveSprintCount).HasDefaultValue(2);
            b.Property(v => v.Target).HasDefaultValue(30m);
            b.Property(v => v.DefaultSpPerBug).HasDefaultValue(3);
        });

        builder.ComplexProperty(s => s.Xray, b =>
        {
            b.Property(v => v.Enabled).HasDefaultValue(false);
            b.Property(v => v.ClientId).HasMaxLength(512);
            b.Property(v => v.ClientSecret).HasMaxLength(512);
        });

        builder.ComplexProperty(s => s.CycleTime, b =>
        {
            b.Property(v => v.StartStage);
            b.Property(v => v.EndStage);
        });

        builder.ComplexProperty(s => s.Sync, b =>
        {
            b.Property(v => v.BackSprintCount).HasDefaultValue(20);
            b.Property(v => v.PlanningWindowDays).HasDefaultValue(2);
        });

        var stringListComparer = new ValueComparer<List<string>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        builder.Property(s => s.DoneStatuses).HasConversion(
            v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
            v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(stringListComparer);

        builder.Property(s => s.WorkflowStages).HasConversion(
            v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
            v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(stringListComparer);

        builder.Property(s => s.ExcludedFromScopeStatuses).HasConversion(
            v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
            v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(stringListComparer);

        builder.Property(s => s.CompanyDomain).HasMaxLength(256);
    }
}
