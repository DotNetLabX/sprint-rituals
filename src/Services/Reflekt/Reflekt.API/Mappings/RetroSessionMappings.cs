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
            .Map(d => d.Column1Name, s => s.Column1.Name)
            .Map(d => d.Column1Color, s => s.Column1.Color.Value)
            .Map(d => d.Column2Name, s => s.Column2.Name)
            .Map(d => d.Column2Color, s => s.Column2.Color.Value)
            .Map(d => d.Column3Name, s => s.Column3.Name)
            .Map(d => d.Column3Color, s => s.Column3.Color.Value)
            .Map(d => d.WritingTimerDefault, s => s.Timers.Writing)
            .Map(d => d.VotingTimerDefault, s => s.Timers.Voting)
            .Map(d => d.DiscussingTimerDefault, s => s.Timers.Discussing);
    }
}
