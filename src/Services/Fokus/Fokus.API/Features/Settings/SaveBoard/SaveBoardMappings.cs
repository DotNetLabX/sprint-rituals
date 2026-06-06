using Mapster;

namespace Fokus.API.Features.Settings.SaveBoard;

public class SaveBoardMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<SaveBoardCommand, AppSettings>()
            .IgnoreNonMapped(true);
    }
}
