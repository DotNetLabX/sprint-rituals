using Mapster;

namespace Fokus.API.Features.Settings.SaveHealthConfig;

public class SaveHealthConfigMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<SaveHealthConfigCommand, AppSettings>()
            .IgnoreNonMapped(true);
    }
}
