using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Matching.Queue
{
    public partial class MatchingManager
    {
        // Metadata follows the Player lifetime; cancelling an old login must not
        // cancel a replacement Player's newly requested admission.
        private readonly ConditionalWeakTable<IPlayer, QueueCancellation> cancellations = new();
        private readonly Dictionary<IMatchingRoleCheck, Dictionary<Identity, Participant>> roleCheckOwners = [];

        private sealed class QueueCancellation
        {
            internal long All;
            internal readonly ConcurrentDictionary<Static.Matching.MatchType, long> Types = new();
            internal long Type(Static.Matching.MatchType type) => Types.GetValueOrDefault(type);
        }

        internal sealed class Participant(IPlayer player)
        {
            internal readonly IPlayer Player = player;
            private readonly Identity identity = player.Identity;
            private readonly IGameSession session = player.Session;
            private readonly EventQueue events = player.Session?.Events;
            private readonly IAccount account = player.Account;
            private readonly ulong epoch = player.Account?.WorldSessionEpoch ?? 0;

            internal bool Current(IPlayerManager registry) => ReferenceEquals(registry.GetPlayer(identity), Player)
                && Player.Identity == identity && ReferenceEquals(Player.Session, session) && session != null
                && ReferenceEquals(session.Events, events) && ReferenceEquals(Player.Account, account)
                && (account == null || account.WorldSessionEpoch == epoch && ReferenceEquals(account.Session, session))
                && session is IPlayerSession current && ReferenceEquals(current.Player, Player)
                && session is not NetworkSession { IsDisconnecting: true };
        }

        private sealed class PendingQueueJoin(IMatchingQueueProposal proposal, Participant participant, QueueCancellation state)
        {
            internal readonly IMatchingQueueProposal Proposal = proposal;
            private readonly long all = Volatile.Read(ref state.All), type = state.Type(proposal.MatchType);
            private bool Uncancelled() => Volatile.Read(ref state.All) == all && state.Type(Proposal.MatchType) == type;
            internal bool IsCurrent(IPlayerManager registry) => Uncancelled() && participant.Current(registry) && Uncancelled();
        }

        private PendingQueueJoin CapturePendingJoin(IPlayer player, IMatchingQueueProposal proposal)
            => new(proposal, new Participant(player), cancellations.GetValue(player, _ => new()));

        private void CancelPendingJoin(IPlayer player, Static.Matching.MatchType? type)
        {
            var state = cancellations.GetValue(player, _ => new());
            if (type.HasValue) state.Types.AddOrUpdate(type.Value, 1, (_, value) => value + 1);
            else Interlocked.Increment(ref state.All);
        }

        private bool OwnsRoleResponse(IMatchingRoleCheck check, IPlayer player)
            => roleCheckOwners.TryGetValue(check, out var owners) && owners.TryGetValue(player.Identity, out var owner)
                && ReferenceEquals(owner?.Player, player) && owner.Current(playerManager);

        private bool OwnsRoleNotices(IMatchingRoleCheck check)
            => roleCheckOwners.TryGetValue(check, out var owners) && owners.Keys.All(identity =>
                characterMatchingRoleChecks.TryGetValue(identity, out var current) && ReferenceEquals(current, check));

        private bool CanAdmitRoleCheck(IMatchingRoleCheck check)
            => check.Status == MatchingRoleCheckStatus.Success && roleCheckOwners.TryGetValue(check, out var owners)
                && owners.Values.All(owner => owner?.Current(playerManager) == true)
                && check.Status == MatchingRoleCheckStatus.Success && roleCheckOwners.ContainsKey(check);

        public bool RespondToRoleCheck(IPlayer player, Static.Matching.MatchType matchType, Role roles, bool accepted)
        {
            if (player == null || !characterMatchingRoleChecks.TryGetValue(player.Identity, out var check)
                || check.MatchingQueueProposal.MatchType != matchType || check.Status != MatchingRoleCheckStatus.Pending
                || !OwnsRoleResponse(check, player)) return false;
            if (!accepted || roles == Role.None)
            {
                check.Cancel(); RemoveRoleCheck(check); return true;
            }
            var member = check.GetMembers().Single(row => row.Identity == player.Identity);
            if (member.Roles.HasValue) return false;
            check.Respond(player.Identity, roles);
            return true;
        }

        private void CancelRoleCheck(IPlayer player, Static.Matching.MatchType? matchType)
        {
            foreach (var entry in roleCheckOwners.ToArray())
            {
                var check = entry.Key;
                if (matchType.HasValue && check.MatchingQueueProposal.MatchType != matchType.Value
                    || !entry.Value.Values.Any(owner => ReferenceEquals(owner?.Player, player))) continue;
                check.Cancel(); RemoveRoleCheck(check);
            }
        }

        private void RemoveRoleCheck(IMatchingRoleCheck check)
        {
            matchingRoleChecks.Remove(check);
            roleCheckOwners.Remove(check);
            var owned = check.GetMembers().Where(member => characterMatchingRoleChecks.TryGetValue(member.Identity, out var current)
                && ReferenceEquals(current, check)).Select(member => member.Identity).ToArray();
            foreach (var identity in owned) characterMatchingRoleChecks.Remove(identity);
            foreach (var identity in owned) GetMatchingCharacter(identity).SendMatchingStatus();
        }
    }
}
