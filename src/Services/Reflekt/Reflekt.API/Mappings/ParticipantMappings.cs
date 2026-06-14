using Mapster;
using Reflekt.API.Features.Participants;
using Reflekt.Domain.Participants;

namespace Reflekt.API.Mappings;

public class ParticipantMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Participant, JoinSprintResponse>();
        config.NewConfig<Participant, ParticipantResponse>();
    }
}
