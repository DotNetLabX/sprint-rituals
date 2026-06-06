using Mapster;

namespace Fokus.API.Features.Settings.SaveAnalyticsTargets;

public class SaveAnalyticsTargetsMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<SaveAnalyticsTargetsCommand, AppSettings>()
            .IgnoreNonMapped(true);
    }
}
