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
            public bool Publishing;
            public readonly HashSet<CharacterContext> Contexts = [];
        }

        private DisbandChange pendingDisband;

        private void StageDisbandHeader(CharacterContext context)
        {
            var entry = context.ChangeTracker.Entries<GuildModel>().SingleOrDefault(row => row.Entity.Id == Id)
                ?? context.Attach(context.Guild.AsNoTracking().SingleOrDefault(row => row.Id == Id)
                    ?? new GuildModel { Id = Id, Type = (byte)Type });
            context.ChangeTracker.DetectChanges();
            if (entry.Entity.Type != (byte)Type || entry.Property(row => row.Type).IsModified)
            {
                var error = new InvalidOperationException("Guild disband cannot admit a foreign tracked type.");
                context.RejectSave(error); throw error;
            }
            entry.Property(row => row.DeleteTime).CurrentValue = pendingDisband?.DeletedAt ?? DateTime.UtcNow;
            entry.Property(row => row.OriginalName).CurrentValue = pendingDisband != null ? pendingDisband.Name : Name;
            entry.Property(row => row.Name).CurrentValue = null;
            entry.Property(row => row.OriginalLeaderId).CurrentValue = pendingDisband != null ? pendingDisband.LeaderId : LeaderId;
            entry.Property(row => row.LeaderId).CurrentValue = null;
            foreach (string field in new[] { nameof(GuildModel.DeleteTime), nameof(GuildModel.OriginalName), nameof(GuildModel.Name), nameof(GuildModel.OriginalLeaderId), nameof(GuildModel.LeaderId) })
                entry.Property(field).IsModified = true;
        }

        private void ObserveDisbandSave(CharacterContext context, DisbandChange disband, bool readOnly = false)
        {
            var entry = context.ChangeTracker.Entries<GuildModel>().SingleOrDefault(row => row.Entity.Id == Id);
            if (readOnly)
            {
                GuildModel actual = context.Guild.AsNoTracking().Single(row => row.Id == Id);
                entry ??= context.Attach(actual);
                context.ChangeTracker.DetectChanges();
                string[] fields = [nameof(GuildModel.Id), nameof(GuildModel.Type), nameof(GuildModel.DeleteTime),
                    nameof(GuildModel.Name), nameof(GuildModel.OriginalName), nameof(GuildModel.LeaderId), nameof(GuildModel.OriginalLeaderId)];
                if (actual.Type != (byte)Type || !MatchesMutationTombstone(actual)
                    || fields.Any(field => entry.Property(field).IsModified
                        || !Equals(entry.Property(field).CurrentValue, context.Entry(actual).Property(field).CurrentValue)))
                {
                    var error = new InvalidOperationException("Late guild mutations cannot change the committed disband tombstone.");
                    context.RejectSave(error); throw error;
                }
            }
            var guard = new CharacterColumnSaveIntent(entry,
                [nameof(GuildModel.Id), nameof(GuildModel.Type), nameof(GuildModel.DeleteTime), nameof(GuildModel.Name), nameof(GuildModel.OriginalName),
                    nameof(GuildModel.LeaderId), nameof(GuildModel.OriginalLeaderId)], true, "Guild disband");
            guard.Capture();
            bool flushed = false;
            bool MatchesSql()
            {
                GuildModel row = context.Guild.AsNoTracking().SingleOrDefault(row => row.Id == Id);
                return row != null && row.Type == (byte)Type && row.DeleteTime == disband.DeletedAt && row.Name == null && row.LeaderId == null
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
                if ((readOnly || phase != CharacterSaveValidationPhase.BeforeFlush) && !MatchesSql())
                    throw new InvalidOperationException("Guild disband did not persist its captured tombstone.");
            });
        }

        // The manager serializes save/publication units for this original guild actor.
        internal async Task PublishDisbandAsync(bool unpersisted)
        {
            DisbandChange disband;
            lock (MutationGate)
            {
                disband = pendingDisband;
                if (disband == null || disband.Published) return;
                if (!unpersisted && (!disband.Committed || mutationPersistence?.NeedsSave == true))
                    throw new InvalidOperationException("Guild disband cannot publish before all accepted mutations commit.");
                // Retained setters share this gate: once cleanup starts no later
                // mutation can be accepted behind an already published member.
                disband.Publishing = true;
            }

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
