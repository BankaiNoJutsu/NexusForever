using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Housing;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Guild;

namespace NexusForever.Game.Guild
{
    public partial class Community
    {
        private CommunityRenameSettlement paidRename;
        private bool applyingPaidName;
        internal bool PaidNamePrepared => paidRename?.Prepared == true;
        internal void RequirePaidNameAssignment()
        {
            if (paidRename != null && !applyingPaidName) throw new InvalidOperationException("Community name belongs to a retained paid rename.");
        }
        public bool QueuePaidRename(Player player, string name, CurrencyType currency, ulong cost, IDisposable reservation)
        {
            lock (MutationGate)
            {
                if (paidRename != null || PendingDelete || NameRegistry is not GlobalGuildManager registry
                    || !ReferenceEquals(registry.GetGuild(Id), this) || !registry.OwnsPaidNameReservation(this, name, reservation)
                    || Residence is not Housing.Residence { HasPersistence: true } root)
                    return false;
                var command = new CommunityRenameSettlement(registry, this, root, player, name, currency, cost, reservation);
                paidRename = command; registry.QueuePaidRename(command); return true;
            }
        }
        internal bool OwnsPaidRename(CommunityRenameSettlement command) => ReferenceEquals(paidRename, command);
        internal void ApplyPaidRename(CommunityRenameSettlement command, string target)
        {
            lock (MutationGate)
            {
                if (!OwnsPaidRename(command)) throw new InvalidOperationException("Community lost its exact paid name reservation.");
                if (PendingDelete) return;
                applyingPaidName = true;
                try { AdoptPaidName(target); }
                finally { applyingPaidName = false; }
            }
        }
        internal IReadOnlyList<Action> CapturePaidNameBroadcast(CommunityRenameSettlement command, string target)
        {
            lock (MutationGate)
            {
                if (!OwnsPaidRename(command)) throw new InvalidOperationException("Community lost its paid name publication.");
                var recipients = new List<Action>();
                foreach (ulong id in onlineMembers)
                {
                    IPlayer player = playerManager?.GetPlayer(id);
                    IGameSession session = player?.Session;
                    if (session == null) continue;
                    recipients.Add(() =>
                    {
                        if (PendingDelete || !OwnsPaidRename(command) || !onlineMembers.Contains(id) || GetMember(id) is not { PendingDelete: false }
                            || !ReferenceEquals(playerManager.GetPlayer(id), player) || !ReferenceEquals(player.Session, session)
                            || session is not IPlayerSession current || !ReferenceEquals(current.Player, player)) return;
                        session.EnqueueMessageEncrypted(new ServerGuildRename { GuildIdentity = Identity.ToNetworkIdentity(), Name = target });
                    });
                }
                return recipients;
            }
        }
        internal void ReleasePaidRename(CommunityRenameSettlement command)
        {
            lock (MutationGate)
            {
                if (!OwnsPaidRename(command)) throw new InvalidOperationException("Community cannot release another paid name.");
                paidRename = null;
            }
        }
    }
}
