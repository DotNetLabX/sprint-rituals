using Mapster;

namespace Fokus.API.Features.Settings.SaveSyncConfig;

public class SaveSyncConfigMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<SaveSyncConfigCommand, AppSettings>()
            .IgnoreNonMapped(true);
    }
}
