using Mapster;
using Reflekt.API.Features.Sprints;
using Reflekt.Domain.Sprints;

namespace Reflekt.API.Mappings;

public class SprintMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Sprint, SprintResponse>()
            .Map(d => d.State, s => s.State.ToString())
            .Map(d => d.CurrentPhase, s => s.CurrentPhase.ToString())
            .Map(d => d.Column1Color, s => s.Column1Color.Value)
            .Map(d => d.Column2Color, s => s.Column2Color.Value)
            .Map(d => d.Column3Color, s => s.Column3Color.Value);
    }
}
