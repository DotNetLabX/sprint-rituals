using Mapster;
using Reflekt.API.Features.Reports;
using Reflekt.Domain.Reports;

namespace Reflekt.API.Mappings;

public class ReportMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Domain result → wire response maps (ADR-010: endpoint owns the DTO, domain stays DTO-free).
        // All members are name-identical except ReportEvidenceLinkData.Source (enum → string).

        config.NewConfig<RetroReportData, ReportResponse>();
        config.NewConfig<ReportRetroSessionInfoData, ReportRetroSessionInfo>();
        config.NewConfig<ReportTeamInfoData, ReportTeamInfo>();
        config.NewConfig<ReportParticipantInfoData, ReportParticipantInfo>();
        config.NewConfig<ReportColumnSectionData, ReportColumnSection>();
        config.NewConfig<ReportGroupEntryData, ReportGroupEntry>();
        config.NewConfig<ReportCardEntryData, ReportCardEntry>();
        config.NewConfig<ReportActionItemData, ReportActionItemEntry>();

        // Source is the EvidenceLinkSource enum on the domain side — stringified here at the boundary.
        config.NewConfig<ReportEvidenceLinkData, ReportEvidenceLinkEntry>()
            .Map(d => d.Source, s => s.Source.ToString());

        config.NewConfig<ReportPreviousActionsData, ReportPreviousActionsSection>();
        config.NewConfig<ReportPreviousActionData, ReportPreviousActionEntry>();
    }
}
