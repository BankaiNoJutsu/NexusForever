using NexusForever.Game.Static.Guild;
using NexusForever.Network.Internal.Message.Guild;
using NexusForever.Shared;

namespace NexusForever.Game.Guild
{
    public abstract partial class GuildBase
    {
        private abstract record GuildPublication
        {
            internal abstract Task Publish(GuildBase owner);
        }
        private sealed record GuildCreatedPublication(ulong Guild, GuildType Type) : GuildPublication
        {
            internal override Task Publish(GuildBase owner) => owner.messagePublisher.PublishAsync(new GuildCreatedMessage { GuildId = Guild, Type = Type });
        }
        private sealed record MemberPublication(ulong Guild, GuildType Type, ushort Realm, ulong Character,
            byte? Rank, GuildRankPermission Permissions, bool Added) : GuildPublication
        {
            internal override Task Publish(GuildBase owner) => owner.messagePublisher.PublishAsync(Build());
            private object Build() => !Rank.HasValue ? new GuildMemberRemovedMessage { GuildId = Guild, Type = Type,
                Member = new() { Id = Character, RealmId = Realm } }
                : Added ? new GuildMemberAddedMessage { GuildId = Guild, Type = Type,
                    Member = new() { Id = Character, RealmId = Realm }, Rank = Rank.Value, Permissions = Permissions }
                : new GuildMemberRankUpdatedMessage { GuildId = Guild, Type = Type,
                    Member = new() { Id = Character, RealmId = Realm }, Rank = Rank.Value, Permissions = Permissions };
        }
        private sealed record AchievementPublication(Achievement.GuildAchievementManager Manager, GuildMutationPersistence.Achievement[] Rows) : GuildPublication
        {
            internal override Task Publish(GuildBase owner) { Manager.Publish(Rows); return Task.CompletedTask; }
        }
        private sealed record RealmFirstPublication(Achievement.GuildAchievementManager Manager, ushort Id, string Name) : GuildPublication
        {
            internal override Task Publish(GuildBase owner) { Manager.PublishRealmFirst(Id, Name); return Task.CompletedTask; }
        }
        internal void EnqueueAchievementUpdate(Achievement.GuildAchievementManager manager, GuildMutationPersistence.Achievement[] rows)
            => pendingMutationPublications.Enqueue(new AchievementPublication(manager, rows.ToArray()));
        internal void EnqueueRealmFirst(Achievement.GuildAchievementManager manager, ushort id, string name)
            => pendingMutationPublications.Enqueue(new RealmFirstPublication(manager, id, name));
        private readonly Queue<GuildPublication> pendingMutationPublications = new();
        private Task mutationPublication;
        private bool NeedsMutationPublication { get { lock (MutationGate) return pendingMutationPublications.Count != 0; } }
        private void CaptureMutationPublications(GuildMutationPersistence.Snapshot before, GuildMutationPersistence.Snapshot after)
        {
            foreach (ulong character in before.Members.Select(row => row.Character).Union(after.Members.Select(row => row.Character)).Order())
            {
                var old = before.Members.SingleOrDefault(row => row.Character == character);
                var next = after.Members.SingleOrDefault(row => row.Character == character);
                GuildRankPermission permissions = next == null ? GuildRankPermission.None
                    : (GuildRankPermission)after.Ranks.Single(row => row.Index == next.Rank).Permission;
                bool changed = old == null || next == null || old.Rank != next.Rank
                    || before.Ranks.Single(row => row.Index == old.Rank).Permission != (uint)permissions;
                if (changed) pendingMutationPublications.Enqueue(new MemberPublication(Id, Type, Identity.RealmId, character, next?.Rank, permissions, old == null));
            }
        }
        private void PublishMemberMutation(object message)
        {
            // The ordinary graph derives committed net membership changes. The
            // existing first-create/disband paths keep their distinct lifecycle.
            if (mutationPersistence == null || IsDisbandAccepted)
                messagePublisher.PublishAsync(message).FireAndForgetAsync();
        }
        internal Task PublishMutationsAsync()
        {
            lock (MutationGate)
            {
                if (mutationPublication != null) return mutationPublication;
                var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                mutationPublication = completion.Task;
                Drain(completion).FireAndForgetAsync();
                return completion.Task;
            }
        }
        private async Task Drain(TaskCompletionSource completion)
        {
            try
            {
                while (true)
                {
                    GuildPublication next;
                    lock (MutationGate)
                    {
                        if (!pendingMutationPublications.TryPeek(out next)) break;
                    }
                    await next.Publish(this);
                    lock (MutationGate)
                    {
                        if (!ReferenceEquals(pendingMutationPublications.Peek(), next))
                            throw new InvalidOperationException("Guild publication lost its original acknowledged message.");
                        pendingMutationPublications.Dequeue();
                    }
                }
                completion.TrySetResult();
            }
            catch (Exception error) { completion.TrySetException(error); }
            finally { lock (MutationGate) mutationPublication = null; }
        }
    }
}
