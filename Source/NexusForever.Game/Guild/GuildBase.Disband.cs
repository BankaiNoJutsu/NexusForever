using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Entity;
using NexusForever.Network.Internal.Message.Guild;

namespace NexusForever.Game.Guild
{
    public partial class GuildBase
    {
        private sealed class DisbandChange(string name, ulong? leaderId, ulong[] memberIds)
        {
            public string Name { get; } = name;
            public ulong? LeaderId { get; } = leaderId;
            // The existing MySQL column is datetime without fractional precision.
            public DateTime DeletedAt { get; } = new(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond, DateTimeKind.Utc);
            public ulong[] MemberIds { get; } = memberIds;
            public volatile bool Committed;
            public int PublishedMembers;
            public bool Published;
            public readonly HashSet<CharacterContext> Contexts = [];
        }

        private DisbandChange pendingDisband;

        private void ObserveDisbandSave(CharacterContext context, DisbandChange disband)
        {
            var entry = context.ChangeTracker.Entries<GuildModel>().Single(row => row.Entity.Id == Id);
            var guard = new CharacterColumnSaveIntent(entry,
                [nameof(GuildModel.Id), nameof(GuildModel.DeleteTime), nameof(GuildModel.Name), nameof(GuildModel.OriginalName),
                    nameof(GuildModel.LeaderId), nameof(GuildModel.OriginalLeaderId)], true, "Guild disband");
            guard.Capture();
            bool flushed = false;
            bool MatchesSql()
            {
                GuildModel row = context.Guild.AsNoTracking().SingleOrDefault(row => row.Id == Id);
                return row != null && row.DeleteTime == disband.DeletedAt && row.Name == null && row.LeaderId == null
                    && row.OriginalName == disband.Name && row.OriginalLeaderId == disband.LeaderId;
            }
            EventHandler<SavedChangesEventArgs> onFlushed = (_, _) =>
            {
                if (context.HasSavepointRollback)
                    return;
                flushed = MatchesSql();
                if (!flushed)
                    throw new InvalidOperationException("Guild disband did not flush its captured tombstone.");
                guard.Capture();
                guard.Flushed = flushed;
            };
            lock (disband.Contexts) disband.Contexts.Add(context);
            context.SavedChanges += onFlushed;
            context.OnSaveCompleted(committed =>
            {
                context.SavedChanges -= onFlushed;
                lock (disband.Contexts) disband.Contexts.Remove(context);
                if (committed && flushed)
                    disband.Committed = true;
            });
            context.OnSaveValidation(phase =>
            {
                if (context.HasSavepointRollback)
                    return;
                if (!context.ChangeTracker.Entries<GuildModel>().Any(row => ReferenceEquals(row.Entity, entry.Entity)))
                    throw new InvalidOperationException("Guild disband lost its tracked tombstone.");
                context.ChangeTracker.DetectChanges();
                guard.Validate();
                if (phase != CharacterSaveValidationPhase.BeforeFlush && !MatchesSql())
                    throw new InvalidOperationException("Guild disband did not persist its captured tombstone.");
            });
        }

        // The manager serializes save/publication units for this original guild actor.
        internal async Task PublishDisbandAsync(bool unpersisted)
        {
            DisbandChange disband = pendingDisband;
            if (disband == null || disband.Published)
                return;
            if (!unpersisted && !disband.Committed)
                throw new InvalidOperationException("Guild disband cannot publish before its save commits.");

            while (disband.PublishedMembers < disband.MemberIds.Length)
            {
                await messagePublisher.PublishAsync(new GuildMemberRemovedMessage
                {
                    GuildId = Id,
                    Type = Type,
                    Member = new Network.Internal.Message.Shared.Identity
                    {
                        Id = disband.MemberIds[disband.PublishedMembers],
                        RealmId = Identity.RealmId
                    }
                });
                disband.PublishedMembers++;
            }
            await messagePublisher.PublishAsync(new GuildDisbandedMessage { GuildId = Id, Type = Type });
            disband.Published = true;
        }
    }
}
