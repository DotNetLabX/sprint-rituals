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

        RetroSession session;
        if (!await db.RetroSessions.AnyAsync(s => s.TeamId == team.Id, ct))
        {
            // dev-only seed: placeholder Fokus sprint id [1] satisfies the ≥1 invariant; no real Fokus sprint 1 guaranteed (same spirit as SeedOwnerPersonId sentinel)
            session = RetroSession.Create("Retro 1", team.Id, fokusSprintIds: [1]);
            session.Start();
            session.ClearDomainEvents();
            db.RetroSessions.Add(session);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            session = await db.RetroSessions.FirstAsync(s => s.TeamId == team.Id, ct);
        }

        if (!await db.Participants.AnyAsync(p => p.RetroSessionId == session.Id, ct))
        {
            string[] names = ["Ana", "Mihai", "Elena"];
            foreach (var name in names)
            {
                var participant = Participant.Join(name, session.Id);
                participant.ClearDomainEvents();
                db.Participants.Add(participant);
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Cards.AnyAsync(c => c.RetroSessionId == session.Id, ct))
        {
            var participants = await db.Participants
                .Where(p => p.RetroSessionId == session.Id)
                .ToListAsync(ct);

            var cards = new[]
            {
                Card.Create("Great team collaboration this retro!", session.Id, participants[0].Id, ColumnType.Column1, CardType.Note, null),
                Card.Create("CI pipeline was reliable throughout.", session.Id, participants[1].Id, ColumnType.Column1, CardType.Note, null),
                Card.Create("Planning took too long.", session.Id, participants[2].Id, ColumnType.Column2, CardType.Note, null),
                Card.Create("Communication gaps between front and back.", session.Id, participants[0].Id, ColumnType.Column2, CardType.Note, null),
                Card.Create("Update onboarding docs.", session.Id, participants[1].Id, ColumnType.ActionItems, CardType.ActionItem, participants[2].DisplayName),
            };

            foreach (var card in cards)
            {
                card.ClearDomainEvents();
                db.Cards.Add(card);
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.CardGroups.AnyAsync(g => g.RetroSessionId == session.Id, ct))
        {
            var noteCards = await db.Cards
                .Where(c => c.RetroSessionId == session.Id && c.CardType == CardType.Note)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync(ct);

            foreach (var card in noteCards)
            {
                var group = CardGroup.Create(session.Id, card.ColumnType);
                group.ClearDomainEvents();
                db.CardGroups.Add(group);
                await db.SaveChangesAsync(ct);
                card.SetGroupId(group.Id);
            }
            await db.SaveChangesAsync(ct);
        }

        var firstWentWellCard = await db.Cards
            .FirstOrDefaultAsync(c => c.RetroSessionId == session.Id && c.ColumnType == ColumnType.Column1, ct);

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
                .Where(p => p.RetroSessionId == session.Id)
                .ToListAsync(ct);

            var cards = await db.Cards
                .Where(c => c.RetroSessionId == session.Id)
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

