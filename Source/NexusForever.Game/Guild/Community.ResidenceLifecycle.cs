using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Static.Housing;
using NexusForever.Game.Housing;
using NexusForever.Shared;

namespace NexusForever.Game.Guild
{
    public partial class Community
    {
        private sealed class ResidenceChange(IResidence residence, string name = null)
        {
            public IResidence Residence { get; } = residence;
            public string Name { get; } = name;
        }

        private sealed class MemberRemoval(ulong characterId)
        {
            public ulong CharacterId { get; } = characterId;
        }

        private readonly object residenceLifecycleLock = new();
        internal override object MutationGate => residenceLifecycleLock;
        private readonly Dictionary<ulong, ResidenceChange> pendingResidenceChanges = [];
        private readonly Dictionary<ulong, MemberRemoval> pendingMemberRemovals = [];
        private object pendingDisband;
        private bool disbanded;
        private IResidenceEntrance disbandEntrance;

        public override bool PendingDelete
        {
            get { lock (residenceLifecycleLock) return disbanded || base.PendingDelete; }
        }

        public override bool NeedsSave
        {
            get
            {
                lock (residenceLifecycleLock)
                    return base.NeedsSave || pendingResidenceChanges.Count != 0
                        || pendingMemberRemovals.Count != 0 || pendingDisband != null;
            }
        }

        protected override void RemoveMember(ulong characterId, bool disband)
        {
            lock (residenceLifecycleLock)
            {
                if (!members.TryGetValue(characterId, out IGuildMember member))
                    throw new ArgumentException($"Invalid member {characterId} for community {Id}.");
                if (member.PendingDelete && !disband)
                    return;

                bool persisted = !member.PendingCreate;
                IResidence child = Residence?.GetChild(characterId)?.Residence;
                if (child != null)
                    DetachResidence(child, disbandEntrance);

                base.RemoveMember(characterId, disband);
                if (!disband && persisted)
                    pendingMemberRemovals[characterId] = new MemberRemoval(characterId);
            }
        }

        protected override IGuildMember AddMember(IPlayer player, byte rank = 9)
        {
            lock (residenceLifecycleLock)
            {
                IGuildMember member = base.AddMember(player, rank);
                // A previous in-flight delete must not acknowledge this later join.
                if (pendingMemberRemovals.ContainsKey(player.CharacterId))
                    pendingMemberRemovals[player.CharacterId] = new MemberRemoval(player.CharacterId);
                return member;
            }
        }

        public override void DisbandGuild()
        {
            lock (residenceLifecycleLock)
            {
                if (disbanded)
                    return;

                // Resolve required travel data before changing any memberships.
                if (Residence != null)
                    foreach (IResidenceChild child in Residence.GetChildren())
                        if (GetOwnerToMove(child.Residence) != null)
                        {
                            disbandEntrance = GetStandaloneEntrance();
                            break;
                        }

                var change = new object();
                try
                {
                    base.DisbandGuild();
                    IResidenceMapInstance map = null;
                    if (Residence != null)
                    {
                        // Also release stale child links that no longer have a member.
                        foreach (IResidenceChild child in Residence.GetChildren().ToList())
                            DetachResidence(child.Residence, disbandEntrance);

                        map = Residence.Map;
                        QueueResidenceChange(Residence);
                        Residence.GuildOwnerId = null;
                        globalResidenceManager.RemoveCommunity(this);
                    }
                    pendingDisband = change;
                    disbanded = true;
                    if (map != null && !map.UnloadStatus.HasValue)
                        map.Unload();
                }
                finally { disbandEntrance = null; }
            }
        }

        private IResidenceEntrance GetStandaloneEntrance()
            => globalResidenceManager.GetResidenceEntrance(PropertyInfoId.Residence)
                ?? throw new InvalidOperationException("Standalone residence entrance is unavailable.");

        private IPlayer GetOwnerToMove(IResidence residence)
        {
            IResidenceMapInstance map = Residence?.Map;
            IPlayer owner = residence.OwnerId.HasValue
                ? residencePlayerManager?.GetPlayer(residence.OwnerId.Value) : null;
            return map != null && ReferenceEquals(owner?.Map, map) && owner.CanTeleport() ? owner : null;
        }

        private void DetachResidence(IResidence residence, IResidenceEntrance entrance)
        {
            if (Residence == null || !ReferenceEquals(residence.Parent, Residence))
                return;

            IPlayer owner = GetOwnerToMove(residence);
            if (owner != null)
                entrance ??= GetStandaloneEntrance();

            QueueResidenceChange(residence);
            if (Residence.Map != null && ReferenceEquals(residence.Map, Residence.Map))
                Residence.Map.RemoveChild(residence);
            else
                Residence.RemoveChild(residence);
            residence.PropertyInfoId = PropertyInfoId.Residence;

            if (owner != null)
            {
                owner.Rotation = entrance.Rotation.ToEuler();
                owner.TeleportTo(entrance.Entry, entrance.Position, mapLockManager.GetResidenceLock(residence));
            }
        }

        private void QueueResidenceChange(IResidence residence, string name = null)
        {
            if (name == null && pendingResidenceChanges.TryGetValue(residence.Id, out ResidenceChange previous))
                name = previous.Name;
            var change = new ResidenceChange(residence, name);
            if (residence is Residence concrete)
                concrete.DeferSaveToCommunity(change);
            pendingResidenceChanges[residence.Id] = change;
        }

        internal Task SaveWhenReady(CharacterContext context)
        {
            lock (residenceLifecycleLock)
            {
                Task previous = Task.WhenAll(pendingResidenceChanges.Values
                    .Select(change => change.Residence).OfType<Residence>().Select(residence => residence.PreviousSaveUnits(context)));
                if (!previous.IsCompleted)
                    return previous;
                Save(context);
                return null;
            }
        }

        internal void CancelUnpersistedDisband()
        {
            lock (residenceLifecycleLock)
            {
                foreach (ResidenceChange change in pendingResidenceChanges.Values)
                    if (change.Residence is Residence concrete)
                        concrete.CompleteCommunitySave(change);
                pendingResidenceChanges.Clear();
                pendingMemberRemovals.Clear();
                pendingDisband = null;
            }
        }

        public override void Save(CharacterContext context)
        {
            lock (residenceLifecycleLock)
            {
                if (renameSaveContexts.Contains(context) || OwnsMutationSave(context))
                    return;
                if (pendingResidenceChanges.Values.Any(change => change.Name != null))
                    RequireNameSaveDatabase(context);
                if (pendingResidenceChanges.Values.Select(change => change.Residence).OfType<Residence>()
                    .Any(residence => !residence.PreviousSaveUnits(context).IsCompleted))
                    throw new InvalidOperationException("Earlier residence save units must finish before saving the community.");
                var residences = pendingResidenceChanges.ToArray();
                var removals = pendingMemberRemovals.ToArray();
                object disband = pendingDisband;
                try
                {
                    base.Save(context);

                    foreach (var pending in residences)
                    {
                        IResidence residence = pending.Value.Residence;
                        if (residence is Residence concrete)
                        {
                            if (!concrete.IsCommunitySaveOwner(pending.Value))
                                continue;
                            concrete.SaveForCommunity(context);
                            if (concrete.HasPersistence) continue;
                        }
                        else
                            residence.Save(context);
                        var entity = context.ChangeTracker.Entries<ResidenceModel>().SingleOrDefault(e => e.Entity.Id == residence.Id)
                            ?? context.Attach(new ResidenceModel { Id = residence.Id });
                        entity.Property(r => r.GuildOwnerId).CurrentValue = residence.GuildOwnerId;
                        entity.Property(r => r.PropertyInfoId).CurrentValue = (byte)residence.PropertyInfoId;
                        if (entity.State != EntityState.Added)
                        {
                            entity.Property(r => r.GuildOwnerId).IsModified = true;
                            entity.Property(r => r.PropertyInfoId).IsModified = true;
                        }
                        if (pending.Value.Name != null)
                        {
                            entity.Property(r => r.Name).CurrentValue = pending.Value.Name;
                            if (entity.State != EntityState.Added)
                                entity.Property(r => r.Name).IsModified = true;
                        }
                    }

                    foreach (var pending in HasMutationPersistence && !IsDisbandAccepted ? [] : removals)
                    {
                        ulong characterId = pending.Value.CharacterId;
                        if (members.TryGetValue(characterId, out IGuildMember currentMember) && !currentMember.PendingDelete)
                        {
                            // A failed delete may already have removed the old in-memory
                            // member. Rejoin then creates a new member while rollback kept
                            // its database row. Reconcile the row in this same retry unit.
                            bool exists = context.GuildMember.Any(m => m.Id == Id && m.CharacterId == characterId);
                            var entity = context.ChangeTracker.Entries<GuildMemberModel>()
                                .SingleOrDefault(e => e.Entity.Id == Id && e.Entity.CharacterId == characterId)
                                ?? context.Entry(new GuildMemberModel { Id = Id, CharacterId = characterId });
                            entity.State = exists ? EntityState.Unchanged : EntityState.Added;
                            entity.Property(m => m.Rank).CurrentValue = currentMember.Rank.Index;
                            entity.Property(m => m.Note).CurrentValue = currentMember.Note;
                            entity.Property(m => m.CommunityPlotReservation).CurrentValue = currentMember.CommunityPlotReservation;
                            if (exists)
                            {
                                entity.Property(m => m.Rank).IsModified = true;
                                entity.Property(m => m.Note).IsModified = true;
                                entity.Property(m => m.CommunityPlotReservation).IsModified = true;
                            }
                            continue;
                        }
                        bool tracked = context.ChangeTracker.Entries<GuildMemberModel>()
                            .Any(e => e.Entity.Id == Id && e.Entity.CharacterId == characterId);
                        // A previous caller-owned transaction may already have committed.
                        if (!tracked && context.GuildMember.Any(m => m.Id == Id && m.CharacterId == characterId))
                            context.Remove(new GuildMemberModel { Id = Id, CharacterId = characterId });
                    }

                    Func<bool> renameFlushed = ObserveResidenceRenames(context, residences);
                    context.OnSaveCompleted(committed =>
                    {
                        if (!committed || !renameFlushed())
                            return;
                        lock (residenceLifecycleLock)
                        {
                            foreach (var pending in residences)
                            {
                                if (pending.Value.Residence is Residence concrete)
                                    concrete.CompleteCommunitySave(pending.Value);
                                if (pendingResidenceChanges.TryGetValue(pending.Key, out var current) && ReferenceEquals(current, pending.Value))
                                    pendingResidenceChanges.Remove(pending.Key);
                            }
                            foreach (var pending in removals)
                                if (pendingMemberRemovals.TryGetValue(pending.Key, out var current) && ReferenceEquals(current, pending.Value))
                                    pendingMemberRemovals.Remove(pending.Key);
                            if (ReferenceEquals(pendingDisband, disband))
                                pendingDisband = null;
                        }
                    });
                }
                catch (Exception error) { context.RejectSave(error); throw; }
            }
        }
    }
}
