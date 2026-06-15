using Blocks.Contracts.IntegrationEvents;
using MassTransit;
using Reflekt.Domain.FokusSprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.FokusSprints.Consumers;

/// <summary>
/// Refreshes <see cref="FokusSprintSnapshot"/> when a Fokus sprint is synced (ADR-019).
/// Forward-only, last-write-wins: always upserts with the event payload, even for sprints
/// not yet referenced by any <c>RetroSession</c>. This keeps the snapshot table consistent
/// without coupling creation to retro lifecycle.
/// Consumer is idempotent: upsert with the same data leaves the row unchanged.
/// </summary>
public sealed class RefreshFokusSprintSnapshotOnSprintSyncedConsumer(
    FokusSprintSnapshotRepository snapshotRepository)
    : IConsumer<SprintSynced>
{
    public async Task Consume(ConsumeContext<SprintSynced> context)
    {
        var msg = context.Message;

        var snapshot = FokusSprintSnapshot.CreateOrUpdate(
            msg.SprintId,
            msg.SprintName,
            msg.BoardId,
            msg.SyncedAt,
            msg.State);

        await snapshotRepository.UpsertAsync(snapshot, context.CancellationToken);
        await snapshotRepository.SaveChangesAsync(context.CancellationToken);
    }
}
