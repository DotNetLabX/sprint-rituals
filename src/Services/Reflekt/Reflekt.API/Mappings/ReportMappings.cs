using Mapster;
using Reflekt.API.Features.Reports;
using Reflekt.Domain.Cards;

namespace Reflekt.API.Mappings;

public class ReportMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<EvidenceLink, ReportEvidenceLinkEntry>()
            .Map(d => d.Source, s => s.Source.ToString());

        config.NewConfig<Card, ReportCardEntry>();
        config.NewConfig<Card, ReportActionItemEntry>();
    }
}
