using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Matching.Match;
using NexusForever.Game.Static.Matching;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Matching;

public class MatchProposalTeamTests
{
    [Fact]
    public void ResponseCounts_TrackPendingAcceptedAndDeclinedMembers()
    {
        Identity first = new()
        {
            RealmId = 1,
            Id      = 10ul
        };
        Identity second = new()
        {
            RealmId = 1,
            Id      = 20ul
        };
        Identity third = new()
        {
            RealmId = 1,
            Id      = 30ul
        };

        IMatchingQueueGroupTeam queueTeam = CreateQueueTeam(
        [
            CreateMember(first),
            CreateMember(second),
            CreateMember(third)
        ]);
        var proposalTeam = new MatchProposalTeam(new MatchProposalTeamMemberFactory());

        proposalTeam.Initialise(queueTeam);
        Assert.Equal(3u, proposalTeam.MemberCount);
        Assert.Equal(3u, proposalTeam.PendingCount);
        Assert.Equal(0u, proposalTeam.AcceptedCount);
        Assert.Equal(0u, proposalTeam.DeclinedCount);

        proposalTeam.Respond(first, true);
        proposalTeam.Respond(second, false);

        Assert.Equal(1u, proposalTeam.AcceptedCount);
        Assert.Equal(1u, proposalTeam.PendingCount);
        Assert.Equal(1u, proposalTeam.DeclinedCount);
        Assert.False(proposalTeam.TeamReady);
    }

    [Fact]
    public void TeamReady_RequiresEveryMemberToAccept()
    {
        Identity first = new()
        {
            RealmId = 1,
            Id      = 40ul
        };
        Identity second = new()
        {
            RealmId = 1,
            Id      = 50ul
        };
        IMatchingQueueGroupTeam queueTeam = CreateQueueTeam(
        [
            CreateMember(first),
            CreateMember(second)
        ]);
        var proposalTeam = new MatchProposalTeam(new MatchProposalTeamMemberFactory());

        proposalTeam.Initialise(queueTeam);
        proposalTeam.Respond(first, true);
        Assert.False(proposalTeam.TeamReady);

        proposalTeam.Respond(second, true);

        Assert.True(proposalTeam.TeamReady);
        Assert.Equal(2u, proposalTeam.AcceptedCount);
        Assert.Equal(0u, proposalTeam.PendingCount);
        Assert.Equal(0u, proposalTeam.DeclinedCount);
    }

    private static IMatchingQueueProposalMember CreateMember(Identity identity)
    {
        IMatchingQueueProposalMember member = RecordingDispatchProxy<IMatchingQueueProposalMember>.Create(out RecordingDispatchProxy<IMatchingQueueProposalMember> memberProxy);
        memberProxy.SetProperty(nameof(IMatchingQueueProposalMember.Identity), identity);
        memberProxy.SetProperty(nameof(IMatchingQueueProposalMember.Roles), Role.DPS);
        return member;
    }

    private static IMatchingQueueGroupTeam CreateQueueTeam(IEnumerable<IMatchingQueueProposalMember> members)
    {
        IMatchingQueueGroupTeam team = RecordingDispatchProxy<IMatchingQueueGroupTeam>.Create(out RecordingDispatchProxy<IMatchingQueueGroupTeam> teamProxy);
        teamProxy.SetMethodReturn(nameof(IMatchingQueueGroupTeam.GetMembers), members);
        return team;
    }

    private sealed class MatchProposalTeamMemberFactory : IFactory<IMatchProposalTeamMember>
    {
        public IMatchProposalTeamMember Resolve()
        {
            return new MatchProposalTeamMember(NullLogger<MatchProposalTeamMember>.Instance);
        }
    }
}
