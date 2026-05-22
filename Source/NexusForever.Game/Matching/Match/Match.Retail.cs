using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Retail;
using NexusForever.Game.Static.Matching;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Matching.Match
{
    public partial class Match
    {
        protected DateTimeOffset matchStartedUtc = DateTimeOffset.UtcNow;

        private Identity activeKickVoteTarget;
        private Identity activeKickVoteInitiator;
        private readonly HashSet<Identity> kickVotesYes = [];

        public MatchingQueueResult? TryInitiateVoteKick(IPlayer initiator, Identity memberToKick)
        {
            if (Status != MatchStatus.InProgress || activeKickVoteTarget is not null)
                return MatchingQueueResult.KickAlreadyActive;

            if (initiator.Identity.Equals(memberToKick))
                return MatchingQueueResult.CannotKickYourself;

            IMatchTeamMember targetMember = GetTeam(memberToKick)?.GetMember(memberToKick);
            if (targetMember == null)
                return MatchingQueueResult.InvalidTeamMember;

            uint requiredCooldownMs = (uint)(targetMember.InMatch
                ? RetailCertainRules.VoteKickCooldownInInstanceMs
                : RetailCertainRules.VoteKickCooldownOfflineOrAbsentMs);

            uint elapsedMs = (uint)Math.Max(0, (DateTimeOffset.UtcNow - matchStartedUtc).TotalMilliseconds);
            if (elapsedMs < requiredCooldownMs)
            {
                initiator.Session?.EnqueueMessageEncrypted(new ServerMatchingMatchKickCooldownUpdate
                {
                    Result                = MatchingQueueResult.PersonalKickCooldown,
                    WaitTimeBeforeVoteMS  = requiredCooldownMs - elapsedMs
                });
                return MatchingQueueResult.PersonalKickCooldown;
            }

            activeKickVoteTarget     = memberToKick;
            activeKickVoteInitiator  = initiator.Identity;
            kickVotesYes.Clear();
            kickVotesYes.Add(initiator.Identity);

            Broadcast(new ServerMatchingMatchVoteKickBegin
            {
                Initiator    = initiator.Identity.ToNetworkIdentity(),
                MemberToKick = memberToKick.ToNetworkIdentity()
            });

            return null;
        }

        public MatchingQueueResult? CastVoteKick(IPlayer voter, Identity memberToKick, bool voteYes)
        {
            if (activeKickVoteTarget is null || !activeKickVoteTarget.Equals(memberToKick))
                return MatchingQueueResult.NotQueued;

            if (voteYes)
                kickVotesYes.Add(voter.Identity);
            else
                kickVotesYes.Remove(voter.Identity);

            int eligibleVoters = GetTeams().SelectMany(t => t.GetMembers()).Count();
            int requiredYes = (int)Math.Ceiling(eligibleVoters * 0.5);
            if (kickVotesYes.Count < requiredYes)
                return null;

            IPlayer kickedPlayer = playerManager.GetPlayer(memberToKick);
            if (kickedPlayer != null)
                MatchExit(kickedPlayer, true);

            MatchLeave(memberToKick);

            Broadcast(new ServerMatchingMatchVoteKickSucceeded());
            activeKickVoteTarget    = default;
            activeKickVoteInitiator = default;
            kickVotesYes.Clear();

            return null;
        }
    }
}
