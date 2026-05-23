using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Retail;
using NexusForever.Game.Static.Matching;
using NexusForever.Network.World.Message.Model;
using MatchTeamType = NexusForever.Game.Static.Matching.MatchTeam;
using MatchType = NexusForever.Game.Static.Matching.MatchType;

namespace NexusForever.Game.Matching.Match
{
    public partial class PvpMatch
    {
        private bool surrenderVoteActive;
        private MatchTeamType surrenderingTeam;
        private readonly HashSet<Identity> surrenderVotesYes = [];

        public MatchingQueueResult? TryInitiateVoteSurrender(IPlayer initiator)
        {
            if (MatchingMap.GameTypeEntry.MatchTypeEnum != MatchType.Warplot)
                return MatchingQueueResult.SurrenderPermissionDenied;

            if (state != PvpGameState.InProgress || surrenderVoteActive)
                return MatchingQueueResult.SurrenderAlreadyActive;

            uint elapsedMs = (uint)Math.Max(0, (DateTimeOffset.UtcNow - matchStartedUtc).TotalMilliseconds);
            if (elapsedMs < RetailCertainRules.WarplotSurrenderMinElapsedMs)
            {
                initiator.Session?.EnqueueMessageEncrypted(new ServerMatchingMatchOperationResult
                {
                    Result               = MatchingQueueResult.PersonalSurrenderCooldown,
                    WaitTimeBeforeVoteMS = RetailCertainRules.WarplotSurrenderMinElapsedMs - elapsedMs
                });
                return MatchingQueueResult.PersonalSurrenderCooldown;
            }

            surrenderVoteActive = true;
            surrenderingTeam = GetTeam(initiator.Identity)?.Team ?? MatchTeamType.Red;
            surrenderVotesYes.Clear();
            surrenderVotesYes.Add(initiator.Identity);

            Broadcast(new ServerMatchingMatchVoteSurrenderBegin());
            return null;
        }

        public MatchingQueueResult? CastVoteSurrender(IPlayer voter, bool voteYes)
        {
            if (!surrenderVoteActive)
                return MatchingQueueResult.NotQueued;

            if (voteYes)
                surrenderVotesYes.Add(voter.Identity);
            else
                surrenderVotesYes.Remove(voter.Identity);

            int eligibleVoters = GetTeams().SelectMany(t => t.GetMembers()).Count();
            int requiredYes = (int)Math.Ceiling(eligibleVoters * RetailCertainRules.WarplotSurrenderVoteRatio);
            if (surrenderVotesYes.Count < requiredYes)
                return null;

            surrenderVoteActive = false;
            MatchTeamType winner = surrenderingTeam == MatchTeamType.Red ? MatchTeamType.Blue : MatchTeamType.Red;
            surrenderingTeam = default;
            surrenderVotesYes.Clear();

            MatchFinish(winner == MatchTeamType.Red ? MatchWinner.Red : MatchWinner.Blue, MatchEndReason.Forfeit);
            return null;
        }
    }
}
