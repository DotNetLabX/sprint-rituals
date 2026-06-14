using Mapster;
using Reflekt.API.Features.CardGroups;
using Reflekt.Domain.CardGroups;
using Reflekt.Domain.Cards;

namespace Reflekt.API.Mappings;

public class CardGroupMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Card, CardGroupCardEntry>();

        config.NewConfig<CardGroup, CardGroupResponse>()
            .Map(d => d.ColumnType, s => s.ColumnType.ToString())
            .Map(d => d.IsDiscussed, s => s.DiscussedAt.HasValue)
            .Ignore(d => d.Cards!)
            .Ignore(d => d.VoteCount)
            .Ignore(d => d.IsFocused);
    }
}
