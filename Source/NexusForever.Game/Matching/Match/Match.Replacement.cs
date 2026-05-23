using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Static.Matching;

namespace NexusForever.Game.Matching.Match
{
    public partial class Match
    {
        /// <summary>
        /// Adds players from a successful in-progress replacement proposal into this match.
        /// </summary>
        public void AddReplacementMembers(IMatchProposal matchProposal)
        {
            if (Status != MatchStatus.InProgress)
            {
                log.LogTrace("AddReplacementMembers ignored for match {Match} with status {Status}.", Guid, Status);
                return;
            }

            IMatchTeam team = teams.FirstOrDefault();
            if (team == null)
            {
                log.LogTrace("AddReplacementMembers failed for match {Match}: no teams.", Guid);
                return;
            }

            foreach (IMatchProposalTeam proposalTeam in matchProposal.GetTeams())
            {
                foreach (IMatchProposalTeamMember proposalMember in proposalTeam.GetMembers())
                {
                    if (characterTeams.ContainsKey(proposalMember.MatchingQueueProposalMember.Identity))
                        continue;

                    Identity identity = proposalMember.MatchingQueueProposalMember.Identity;
                    Role roles        = proposalMember.MatchingQueueProposalMember.Roles;
                    MatchJoin(team, identity, roles);
                }
            }
        }
    }
}
