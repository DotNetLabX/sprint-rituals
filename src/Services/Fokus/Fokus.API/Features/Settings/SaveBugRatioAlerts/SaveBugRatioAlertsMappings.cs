using Mapster;

namespace Fokus.API.Features.Settings.SaveBugRatioAlerts;

public class SaveBugRatioAlertsMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<SaveBugRatioAlertsCommand, AppSettings>()
            .IgnoreNonMapped(true);
    }
}
