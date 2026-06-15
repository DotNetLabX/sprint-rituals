using Mapster;
using Reflekt.API.Features.RetroSessions;
using Reflekt.Domain.RetroSessions;

namespace Reflekt.API.Mappings;

public class RetroSessionMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<RetroSession, RetroSessionResponse>()
            .Map(d => d.State, s => s.State.ToString())
            .Map(d => d.CurrentPhase, s => s.CurrentPhase.ToString())
            .Map(d => d.Column1Color, s => s.Column1Color.Value)
            .Map(d => d.Column2Color, s => s.Column2Color.Value)
            .Map(d => d.Column3Color, s => s.Column3Color.Value);
    }
}
