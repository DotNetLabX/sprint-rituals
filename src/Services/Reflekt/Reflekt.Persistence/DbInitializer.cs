using Microsoft.EntityFrameworkCore;
using Reflekt.Domain.CardGroups;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Participants;
using Reflekt.Domain.RetroSessions;
using Reflekt.Domain.Teams;
using Reflekt.Domain.Votes;

namespace Reflekt.Persistence;

internal static class DbInitializer
{
    // Dev-only placeholder Person id. NOT guaranteed to match any real Identity Person
    // (RA2 has no Identity↔Reflekt seed handshake). The seed Team exists only so the dev
    // reflekt.db has a demo board; see questions.md Q5 / plan Step 7.
    private const int SeedOwnerPersonId = 1;

    public static async Task SeedAsync(ReflektDbContext db, CancellationToken ct = default)
    {
        Team team;
        if (!await db.Teams.AnyAsync(ct))
        {
            team = Team.Create("Dev Team", SeedOwnerPersonId);
            team.ClearDomainEvents();
            db.Teams.Add(team);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            team = await db.Teams.FirstAsync(ct);
        }

        RetroSession sprint;
        if (!await db.RetroSessions.AnyAsync(s => s.TeamId == team.Id, ct))
        {
            // dev-only seed: placeholder Fokus sprint id [1] satisfies the ≥1 invariant; no real Fokus sprint 1 guaranteed (same spirit as SeedOwnerPersonId sentinel)
            sprint = RetroSession.Create("Sprint 1", team.Id, fokusSprintIds: [1]);
            sprint.Start();
            sprint.ClearDomainEvents();
            db.RetroSessions.Add(sprint);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            sprint = await db.RetroSessions.FirstAsync(s => s.TeamId == team.Id, ct);
        }

        if (!await db.Participants.AnyAsync(p => p.SprintId == sprint.Id, ct))
        {
            string[] names = ["Ana", "Mihai", "Elena"];
            foreach (var name in names)
            {
                var participant = Participant.Join(name, sprint.Id);
                participant.ClearDomainEvents();
                db.Participants.Add(participant);
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Cards.AnyAsync(c => c.SprintId == sprint.Id, ct))
        {
            var participants = await db.Participants
                .Where(p => p.SprintId == sprint.Id)
                .ToListAsync(ct);

            var cards = new[]
            {
                Card.Create("Great team collaboration this sprint!", sprint.Id, participants[0].Id, ColumnType.Column1, CardType.Note, null),
                Card.Create("CI pipeline was reliable throughout.", sprint.Id, participants[1].Id, ColumnType.Column1, CardType.Note, null),
                Card.Create("Sprint planning took too long.", sprint.Id, participants[2].Id, ColumnType.Column2, CardType.Note, null),
                Card.Create("Communication gaps between front and back.", sprint.Id, participants[0].Id, ColumnType.Column2, CardType.Note, null),
                Card.Create("Update onboarding docs.", sprint.Id, participants[1].Id, ColumnType.ActionItems, CardType.ActionItem, participants[2].DisplayName),
            };

            foreach (var card in cards)
            {
                card.ClearDomainEvents();
                db.Cards.Add(card);
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.CardGroups.AnyAsync(g => g.SprintId == sprint.Id, ct))
        {
            var noteCards = await db.Cards
                .Where(c => c.SprintId == sprint.Id && c.CardType == CardType.Note)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync(ct);

            foreach (var card in noteCards)
            {
                var group = CardGroup.Create(sprint.Id, card.ColumnType);
                group.ClearDomainEvents();
                db.CardGroups.Add(group);
                await db.SaveChangesAsync(ct);
                card.SetGroupId(group.Id);
            }
            await db.SaveChangesAsync(ct);
        }

        var firstWentWellCard = await db.Cards
            .FirstOrDefaultAsync(c => c.SprintId == sprint.Id && c.ColumnType == ColumnType.Column1, ct);

        if (firstWentWellCard is not null && !await db.EvidenceLinks.AnyAsync(e => e.CardId == firstWentWellCard.Id, ct))
        {
            firstWentWellCard.AttachEvidenceLink("https://github.com/dotnet/aspnetcore/pull/12345", EvidenceLinkSource.GitHub, null);
            firstWentWellCard.ClearDomainEvents();
            firstWentWellCard.AttachEvidenceLink("https://learn.microsoft.com/en-us/aspnet/core", EvidenceLinkSource.Generic, null);
            firstWentWellCard.ClearDomainEvents();
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Votes.AnyAsync(ct))
        {
            var participants = await db.Participants
                .Where(p => p.SprintId == sprint.Id)
                .ToListAsync(ct);

            var cards = await db.Cards
                .Where(c => c.SprintId == sprint.Id)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync(ct);

            var wentWellCards = cards.Where(c => c.ColumnType == ColumnType.Column1 && c.GroupId.HasValue).ToList();
            var didntGoWellCards = cards.Where(c => c.ColumnType == ColumnType.Column2 && c.GroupId.HasValue).ToList();

            if (wentWellCards.Count > 0 && didntGoWellCards.Count > 0)
            {
                var votesA = new[]
                {
                    Vote.Create(wentWellCards[0].GroupId!.Value, participants[0].Id),
                    Vote.Create(wentWellCards[0].GroupId!.Value, participants[0].Id),
                    Vote.Create(didntGoWellCards[0].GroupId!.Value, participants[0].Id),
                };

                var votesB = new[]
                {
                    Vote.Create(wentWellCards[0].GroupId!.Value, participants[1].Id),
                    Vote.Create(didntGoWellCards[0].GroupId!.Value, participants[1].Id),
                };

                foreach (var vote in votesA.Concat(votesB))
                {
                    vote.ClearDomainEvents();
                    db.Votes.Add(vote);
                }
                await db.SaveChangesAsync(ct);
            }
        }
    }
}

