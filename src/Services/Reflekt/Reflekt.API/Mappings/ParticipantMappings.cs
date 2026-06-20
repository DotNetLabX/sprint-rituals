using Mapster;
using Reflekt.API.Features.Participants;
using Reflekt.Domain.Participants;

namespace Reflekt.API.Mappings;

public class ParticipantMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Wire-stable: response field stays `UserId` (frontend contract), sourced from PersonId (RA2 Step 3).
        config.NewConfig<Participant, JoinRetroResponse>()
            .Map(dest => dest.UserId, src => src.PersonId);
        config.NewConfig<Participant, ParticipantResponse>();
    }
}
