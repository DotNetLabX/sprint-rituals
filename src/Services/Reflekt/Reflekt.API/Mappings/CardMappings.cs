using Mapster;
using Reflekt.API.Features.Cards;
using Reflekt.Domain.Cards;

namespace Reflekt.API.Mappings;

public class CardMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<EvidenceLink, EvidenceLinkResponse>()
            .Map(d => d.Source, s => s.Source.ToString())
            .Map(d => d.FetchStatus, s => s.FetchStatus.ToString());

        config.NewConfig<Card, CardResponse>()
            .Map(d => d.ColumnType, s => s.ColumnType.ToString())
            .Map(d => d.CardType, s => s.CardType.ToString())
            .Map(d => d.VoteCount, _ => 0);
    }
}
