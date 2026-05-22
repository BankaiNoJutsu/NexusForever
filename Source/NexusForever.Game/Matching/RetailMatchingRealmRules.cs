using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Static.Matching;
using NexusForever.Shared;

namespace NexusForever.Game.Matching
{
    public static class RetailMatchingRealmRules
    {
        public static bool RequiresSameRealm(IMatchingQueueProposal proposal)
        {
            return proposal.MatchingQueueFlags.HasFlag(MatchingQueueFlags.RealmOnly);
        }

        public static bool AreRealmCompatible(IMatchingQueueProposal left, IMatchingQueueProposal right, IPlayerManager playerManager)
        {
            if (!RequiresSameRealm(left) && !RequiresSameRealm(right))
                return true;

            ushort? requiredRealm = GetRealmId(left, playerManager);
            if (requiredRealm == null)
                return false;

            foreach (IMatchingQueueProposalMember member in right.GetMembers())
            {
                IPlayer player = playerManager.GetPlayer(member.Identity);
                if (player == null || player.Identity.RealmId != requiredRealm)
                    return false;
            }

            return true;
        }

        static ushort? GetRealmId(IMatchingQueueProposal proposal, IPlayerManager playerManager)
        {
            foreach (IMatchingQueueProposalMember member in proposal.GetMembers())
            {
                IPlayer player = playerManager.GetPlayer(member.Identity);
                if (player != null)
                    return player.Identity.RealmId;
            }

            return null;
        }
    }
}
