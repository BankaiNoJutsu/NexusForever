using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Retail;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Static.Matching;
using MatchType = NexusForever.Game.Static.Matching.MatchType;

namespace NexusForever.Game.Matching
{
    public static class RetailWarplotQueueRules
    {
        public static MatchingQueueResult? ValidateQueue(
            IMatchingQueueProposal proposal,
            IPlayerManager playerManager,
            IMatchingManager matchingManager)
        {
            if (proposal.MatchType != MatchType.Warplot)
                return null;

            IMatchingQueueProposalMember leaderMember = proposal.GetMembers().FirstOrDefault();
            if (leaderMember == null)
                return MatchingQueueResult.GroupSize;

            IPlayer leader = playerManager.GetPlayer(leaderMember.Identity);
            if (leader == null)
                return MatchingQueueResult.OfflineGroupMember;

            IWarParty warParty = leader.GuildManager.GetGuild<IWarParty>(GuildType.WarParty);
            if (warParty == null)
                return MatchingQueueResult.RequiresFullGroup;

            int onlineMembers = warParty.Count(member => playerManager.GetPlayer(member.PlayerIdentity) != null);
            if (onlineMembers < RetailCertainRules.WarplotOnlineMembersRequiredToQueue)
                return MatchingQueueResult.RequiresFullGroup;

            HashSet<Identity> proposalIdentities = proposal.GetMembers()
                .Select(m => m.Identity)
                .ToHashSet();

            int queuedOrJoining = warParty.Count(member =>
                proposalIdentities.Contains(member.PlayerIdentity)
                || matchingManager.GetMatchingCharacter(member.PlayerIdentity).GetMatchingCharacterQueue(MatchType.Warplot) != null);

            if (queuedOrJoining < RetailCertainRules.WarplotQueuedMembersRequiredToPop)
                return MatchingQueueResult.RequiresFullGroup;

            return null;
        }
    }
}
