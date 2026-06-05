using Fokus.API.Features.Sync.SyncSprints;

namespace Fokus.API.Features.Sync.SyncBacklogSprints;

public class SyncBacklogSprintsResponse
{
    public int BacklogSprintsSynced { get; set; }
    public int EpicTicketsDiscovered { get; set; }
    public int SprintFailures { get; set; }
    public int EpicFailures { get; set; }
    public XraySyncSummary? Xray { get; set; }
}
