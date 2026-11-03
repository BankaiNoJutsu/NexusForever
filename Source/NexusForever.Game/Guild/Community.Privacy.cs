using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Housing;
using NexusForever.Game.Static.Guild;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Guild;

namespace NexusForever.Game.Guild
{
    public partial class Community
    {
        private readonly Queue<PrivacyChange> privacyChanges = new();
        private PrivacyChange CurrentPrivacy => privacyChanges.TryPeek(out var current) ? current : null;
        internal bool PrivacyPrepared { get { lock (MutationGate) return CurrentPrivacy?.Prepared == true; } }
        internal bool PrivacyNeedsCommit { get { lock (MutationGate) return CurrentPrivacy is { Prepared: true, Committed: false }; } }
        internal uint CapturePrivacyFlags(uint flags)
        {
            var current = CurrentPrivacy;
            return current?.Prepared == true ? current.WithPrivacy(flags) : flags;
        }
        internal void RequirePrivacyFlagAssignment(GuildFlag target)
        {
            if (PrivacyPrepared && ((target ^ Flags) & GuildFlag.CommunityPrivate) != 0)
                throw new InvalidOperationException("Community privacy belongs to its retained publication unit.");
        }
        internal void PrivacyMutationCommitted(uint intent, uint stored)
        {
            if (CurrentPrivacy is { Prepared: true, Committed: false } current && current.WithPrivacy(intent) == intent && current.WithPrivacy(stored) == stored)
            { current.Committed = true; current.CommittedFlags = stored; }
        }
        public bool QueuePrivacyChange(IPlayer player, bool isPrivate)
        {
            lock (MutationGate)
            {
                if (PendingDelete || !HasMutationPersistence || NameRegistry is not GlobalGuildManager registry
                    || !ReferenceEquals(registry.GetGuild(Id), this) || Residence is not Residence root
                    || globalResidenceManager is not GlobalResidenceManager residences || !ReferenceEquals(residences.GetResidence(root.Id), root)) return false;
                var change = new PrivacyChange(this, registry, residences, root, player, isPrivate);
                privacyChanges.Enqueue(change); registry.QueuePrivacyChange(change); return true;
            }
        }

        internal sealed class PrivacyChange
        {
            internal readonly Community Owner;
            private readonly GlobalGuildManager registry;
            private readonly GlobalResidenceManager residences;
            private readonly IResidence root;
            private readonly IPlayer player;
            private readonly IGameSession session;
            private readonly IGuildManager membership;
            private readonly string publicOwner;
            private readonly bool isPrivate;
            private List<Action> publication;
            private int cursor;
            internal bool Prepared, Committed, Finished;
            internal uint CommittedFlags;
            internal PrivacyChange(Community owner, GlobalGuildManager registry, GlobalResidenceManager residences, IResidence root, IPlayer player, bool isPrivate)
            {
                Owner = owner; this.registry = registry; this.residences = residences; this.root = root; this.player = player; this.isPrivate = isPrivate;
                session = player?.Session; membership = player?.GuildManager; publicOwner = player?.Name;
            }
            internal uint WithPrivacy(uint flags) => isPrivate ? flags | (uint)GuildFlag.CommunityPrivate : flags & ~(uint)GuildFlag.CommunityPrivate;
            private bool Owns() => ReferenceEquals(Owner.CurrentPrivacy, this);
            private bool CurrentRoot => !Owner.PendingDelete && ReferenceEquals(registry.GetGuild(Owner.Id), Owner)
                && ReferenceEquals(Owner.Residence, root) && ReferenceEquals(residences.GetResidence(root.Id), root);
            private bool CurrentPlayer => player != null && ReferenceEquals(player.Session, session)
                && session is IPlayerSession current && ReferenceEquals(current.Player, player)
                && session is not NetworkSession { IsDisconnecting: true } && ReferenceEquals(player.GuildManager, membership)
                && ReferenceEquals(membership.GetGuild<ICommunity>(GuildType.Community), Owner)
                && Owner.GetMember(player.CharacterId) is { PendingDelete: false };
            internal bool Advance()
            {
                lock (Owner.MutationGate)
                {
                    if (Finished || !Owns()) return false;
                    if (!Prepared)
                    {
                        if (!CurrentRoot || player != null && !CurrentPlayer) { Finish(); return false; }
                        if (Owner.PaidNamePrepared || Owner.NameUnitActive || Owner.HasActiveMutationUnit) return false;
                        if (Owner.PendingCreate) return true; // Settle its existing complete creation first.
                        Prepared = true;
                    }
                    if (!Committed) return true;
                    Owner.AdoptCommunityPrivacy(isPrivate, CommittedFlags);
                    publication ??= CapturePublication();
                    while (cursor < publication.Count) { publication[cursor](); cursor++; }
                    Finish(); return false;
                }
            }
            private List<Action> CapturePublication()
            {
                string name = Owner.Name;
                var stages = new List<Action>
                {
                    () => residences.PublishCommunityPrivacy(root, Owner, isPrivate, publicOwner, name)
                };
                foreach (ulong id in Owner.onlineMembers)
                {
                    IPlayer recipient = Owner.playerManager?.GetPlayer(id); IGameSession connection = recipient?.Session;
                    if (connection == null) continue;
                    stages.Add(() =>
                    {
                        if (!CurrentRoot || Owner.GetMember(id) is not { PendingDelete: false } || !Owner.onlineMembers.Contains(id)
                            || !ReferenceEquals(Owner.playerManager.GetPlayer(id), recipient) || !ReferenceEquals(recipient.Session, connection)
                            || connection is not IPlayerSession current || !ReferenceEquals(current.Player, recipient)
                            || connection is NetworkSession { IsDisconnecting: true }) return;
                        connection.EnqueueMessageEncrypted(new ServerGuildFlagsUpdate { GuildIdentity = Owner.Identity.ToNetworkIdentity(), Flags = (GuildFlag)CommittedFlags });
                    });
                }
                stages.Add(() =>
                {
                    if (!CurrentRoot || !CurrentPlayer) return;
                    var reply = new ServerHousingCommunityPrivacyLevelUpdate
                    {
                        Flags = isPrivate ? ServerHousingCommunityPrivacyLevelUpdate.PrivateFlag : 0u,
                        PrivacyLevel = isPrivate ? ServerHousingCommunityPrivacyLevelUpdate.PrivateLuaValue : ServerHousingCommunityPrivacyLevelUpdate.PublicLuaValue
                    };
                    reply.TargetResidence.RealmId = Owner.Identity.RealmId; reply.TargetResidence.ResidenceId = root.Id;
                    session.EnqueueMessageEncrypted(reply);
                });
                return stages;
            }
            private void Finish()
            {
                if (!Owns()) throw new InvalidOperationException("Community privacy cannot release another request.");
                Owner.privacyChanges.Dequeue(); Finished = true;
            }
        }
    }
}
