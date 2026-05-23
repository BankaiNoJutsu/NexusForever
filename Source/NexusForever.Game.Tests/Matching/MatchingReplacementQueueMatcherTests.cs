using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Matching;
using NexusForever.Game.Matching.Queue;
using NexusForever.Game.Static.Matching;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using MatchType = NexusForever.Game.Static.Matching.MatchType;

namespace NexusForever.Game.Tests.Matching;

public class MatchingReplacementQueueMatcherTests
{
    [Fact]
    public void Match_ReplacementGroupRejectsRolesOutsideRequestedMask()
    {
        MatchingQueueGroupMatcher matcher = CreateMatcher(
            requestedRoles: Role.Healer,
            candidateRoles: Role.DPS,
            out IMatchingQueueGroup replacementGroup,
            out IMatchingQueueProposal candidateProposal);

        IMatchingQueueGroupTeam match = matcher.Match(replacementGroup, candidateProposal);

        Assert.Null(match);
    }

    [Fact]
    public void Match_ReplacementGroupAllowsRolesInsideRequestedMask()
    {
        MatchingQueueGroupMatcher matcher = CreateMatcher(
            requestedRoles: Role.Healer,
            candidateRoles: Role.Healer | Role.DPS,
            out IMatchingQueueGroup replacementGroup,
            out IMatchingQueueProposal candidateProposal);

        IMatchingQueueGroupTeam match = matcher.Match(replacementGroup, candidateProposal);

        Assert.NotNull(match);
    }

    private static MatchingQueueGroupMatcher CreateMatcher(
        Role requestedRoles,
        Role candidateRoles,
        out IMatchingQueueGroup replacementGroup,
        out IMatchingQueueProposal candidateProposal)
    {
        IMatchingMap matchingMap = CreateMatchingMap();
        replacementGroup = CreateReplacementGroup(matchingMap, out _);
        candidateProposal = CreateProposal(matchingMap, candidateRoles);

        var replacementRegistry = new MatchingReplacementRegistry();
        replacementRegistry.Register(Guid.NewGuid(), replacementGroup, requestedRoles);

        IMatchingDataManager matchingDataManager = RecordingDispatchProxy<IMatchingDataManager>.Create(out RecordingDispatchProxy<IMatchingDataManager> dataProxy);
        dataProxy.SetMethodReturn(nameof(IMatchingDataManager.IsSingleFactionEnforced), false);
        dataProxy.SetMethodReturn(nameof(IMatchingDataManager.RequiresRoleSelection), false);

        IMatchingRoleEnforcer roleEnforcer = RecordingDispatchProxy<IMatchingRoleEnforcer>.Create(out _);
        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(out _);

        return new MatchingQueueGroupMatcher(
            NullLogger<MatchingQueueGroupMatcher>.Instance,
            matchingDataManager,
            roleEnforcer,
            playerManager,
            replacementRegistry);
    }

    private static IMatchingMap CreateMatchingMap()
    {
        return new MatchingMap
        {
            GameMapEntry = new MatchingGameMapEntry
            {
                Id = 7u
            },
            GameTypeEntry = new MatchingGameTypeEntry
            {
                MatchTypeEnum = MatchType.Adventure,
                TeamSize      = 5u
            }
        };
    }

    private static IMatchingQueueGroup CreateReplacementGroup(IMatchingMap matchingMap, out IMatchingQueueGroupTeam replacementTeam)
    {
        replacementTeam = RecordingDispatchProxy<IMatchingQueueGroupTeam>.Create(out RecordingDispatchProxy<IMatchingQueueGroupTeam> teamProxy);
        teamProxy.SetProperty(nameof(IMatchingQueueGroupTeam.Guid), Guid.NewGuid());
        teamProxy.SetProperty(nameof(IMatchingQueueGroupTeam.Faction), (Faction?)null);
        teamProxy.SetMethodReturn(nameof(IMatchingQueueGroupTeam.GetMembers), Enumerable.Empty<IMatchingQueueProposalMember>());

        IMatchingQueueGroup replacementGroup = RecordingDispatchProxy<IMatchingQueueGroup>.Create(out RecordingDispatchProxy<IMatchingQueueGroup> groupProxy);
        groupProxy.SetProperty(nameof(IMatchingQueueGroup.Guid), Guid.NewGuid());
        groupProxy.SetProperty(nameof(IMatchingQueueGroup.MatchType), MatchType.Adventure);
        groupProxy.SetProperty(nameof(IMatchingQueueGroup.InProgress), true);
        groupProxy.SetProperty(nameof(IMatchingQueueGroup.IsPaused), false);
        groupProxy.SetMethodReturn(nameof(IMatchingQueueGroup.GetMatchingMaps), new List<IMatchingMap> { matchingMap });
        groupProxy.SetMethodReturn(nameof(IMatchingQueueGroup.GetTeams), new[] { replacementTeam });
        return replacementGroup;
    }

    private static IMatchingQueueProposal CreateProposal(IMatchingMap matchingMap, Role roles)
    {
        IMatchingQueueProposal proposal = RecordingDispatchProxy<IMatchingQueueProposal>.Create(out RecordingDispatchProxy<IMatchingQueueProposal> proposalProxy);
        proposalProxy.SetProperty(nameof(IMatchingQueueProposal.Guid), Guid.NewGuid());
        proposalProxy.SetProperty(nameof(IMatchingQueueProposal.Faction), Faction.Exile);
        proposalProxy.SetProperty(nameof(IMatchingQueueProposal.MatchType), MatchType.Adventure);
        proposalProxy.SetProperty(nameof(IMatchingQueueProposal.MatchingQueueFlags), MatchingQueueFlags.None);
        proposalProxy.SetMethodReturn(nameof(IMatchingQueueProposal.GetMatchingMaps), new[] { matchingMap });

        IMatchingQueueProposalMember member = RecordingDispatchProxy<IMatchingQueueProposalMember>.Create(out RecordingDispatchProxy<IMatchingQueueProposalMember> memberProxy);
        memberProxy.SetProperty(nameof(IMatchingQueueProposalMember.MatchingQueueProposal), proposal);
        memberProxy.SetProperty(nameof(IMatchingQueueProposalMember.Identity), new Identity { RealmId = 1, Id = 42ul });
        memberProxy.SetProperty(nameof(IMatchingQueueProposalMember.Roles), roles);
        proposalProxy.SetMethodReturn(nameof(IMatchingQueueProposal.GetMembers), new[] { member });

        return proposal;
    }
}
