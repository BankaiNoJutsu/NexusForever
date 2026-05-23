using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Matching;
using NexusForever.Game.Matching.Match;
using NexusForever.Game.Matching.Queue;
using NexusForever.Game.Static.Matching;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Shared;
using MatchType = NexusForever.Game.Static.Matching.MatchType;

namespace NexusForever.Game.Tests.Matching;

public class MatchManagerReplacementTests
{
    [Fact]
    public void TryStartLookingForReplacements_WhenLeaderIsNotInMatch_ReturnsUnableToQueue()
    {
        Identity leaderIdentity = new()
        {
            RealmId = 1,
            Id      = 7ul
        };

        IPlayer leader = TestPlayerBuilder.Create()
            .WithIdentity(leaderIdentity)
            .Build();
        IMatch match = RecordingDispatchProxy<IMatch>.Create(out RecordingDispatchProxy<IMatch> matchProxy);
        matchProxy.SetProperty(nameof(IMatch.Status), MatchStatus.InProgress);
        matchProxy.SetMethodReturn(nameof(IMatch.GetTeam), null);

        var matchingManager = new MatchingManager(
            NullLogger<MatchingManager>.Instance,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            new MatchingReplacementRegistry());

        MatchingQueueResult? result = matchingManager.TryStartLookingForReplacements(leader, match, Role.Healer);

        Assert.Equal(MatchingQueueResult.UnableToQueue, result);
    }

    [Fact]
    public void ReplacementProposalWithoutActiveMatchClosesQueueInsteadOfCreatingMatch()
    {
        Identity candidateIdentity = new()
        {
            RealmId = 1,
            Id      = 42ul
        };

        IMatchingQueueGroup replacementGroup = CreateReplacementGroup(candidateIdentity);
        var replacementRegistry = new MatchingReplacementRegistry();
        replacementRegistry.Register(Guid.NewGuid(), replacementGroup, Role.Healer);

        IMatchingManager matchingManager = RecordingDispatchProxy<IMatchingManager>.Create(out RecordingDispatchProxy<IMatchingManager> matchingManagerProxy);
        matchingManagerProxy.SetMethodReturnFactory(nameof(IMatchingManager.GetMatchingCharacter), CreateEmptyMatchingCharacter);
        IMatchingQueueTimeManager queueTimeManager = RecordingDispatchProxy<IMatchingQueueTimeManager>.Create(out _);
        IMatchFactory matchFactory = RecordingDispatchProxy<IMatchFactory>.Create(out RecordingDispatchProxy<IMatchFactory> matchFactoryProxy);

        var matchManager = new MatchManager(
            NullLogger<MatchManager>.Instance,
            new StubFactory<IMatchCharacter>(() => new MatchCharacter(NullLogger<MatchCharacter>.Instance)),
            new StubFactory<IMatchProposal>(CreateMatchProposal),
            matchFactory,
            matchingManager,
            queueTimeManager,
            replacementRegistry);

        matchManager.CreateMatchProposal(replacementGroup, new MatchingMapSelectorResult
        {
            MatchingMap = CreateMatchingMap()
        });

        IPlayer candidate = TestPlayerBuilder.Create()
            .WithIdentity(candidateIdentity)
            .Build();
        matchManager.GetMatchCharacter(candidateIdentity).MatchProposal.Respond(candidate, true);

        matchManager.Update(0d);

        Assert.Empty(matchFactoryProxy.GetInvocations(nameof(IMatchFactory.CreateMatch)));

        RecordingDispatchProxy<IMatchingManager>.Invocation stopCall =
            Assert.Single(matchingManagerProxy.GetInvocations(nameof(IMatchingManager.StopLookingForReplacements)));
        Assert.Same(replacementGroup, stopCall.Arguments[0]);
        Assert.Equal(MatchingQueueResult.UnableToQueue, stopCall.Arguments[1]);
    }

    [Fact]
    public void ReplacementProposalDeclinedClosesReplacementQueue()
    {
        Identity candidateIdentity = new()
        {
            RealmId = 1,
            Id      = 84ul
        };

        IMatchingQueueGroup replacementGroup = CreateReplacementGroup(candidateIdentity);
        var replacementRegistry = new MatchingReplacementRegistry();
        replacementRegistry.Register(Guid.NewGuid(), replacementGroup, Role.Healer);

        IMatchingManager matchingManager = RecordingDispatchProxy<IMatchingManager>.Create(out RecordingDispatchProxy<IMatchingManager> matchingManagerProxy);
        matchingManagerProxy.SetMethodReturnFactory(nameof(IMatchingManager.GetMatchingCharacter), CreateEmptyMatchingCharacter);
        IMatchingQueueTimeManager queueTimeManager = RecordingDispatchProxy<IMatchingQueueTimeManager>.Create(out _);
        IMatchFactory matchFactory = RecordingDispatchProxy<IMatchFactory>.Create(out RecordingDispatchProxy<IMatchFactory> matchFactoryProxy);

        var matchManager = new MatchManager(
            NullLogger<MatchManager>.Instance,
            new StubFactory<IMatchCharacter>(() => new MatchCharacter(NullLogger<MatchCharacter>.Instance)),
            new StubFactory<IMatchProposal>(CreateMatchProposal),
            matchFactory,
            matchingManager,
            queueTimeManager,
            replacementRegistry);

        matchManager.CreateMatchProposal(replacementGroup, new MatchingMapSelectorResult
        {
            MatchingMap = CreateMatchingMap()
        });

        IPlayer candidate = TestPlayerBuilder.Create()
            .WithIdentity(candidateIdentity)
            .Build();
        matchManager.GetMatchCharacter(candidateIdentity).MatchProposal.Respond(candidate, false);

        matchManager.Update(0d);

        Assert.Empty(matchFactoryProxy.GetInvocations(nameof(IMatchFactory.CreateMatch)));

        RecordingDispatchProxy<IMatchingManager>.Invocation stopCall =
            Assert.Single(matchingManagerProxy.GetInvocations(nameof(IMatchingManager.StopLookingForReplacements)));
        Assert.Same(replacementGroup, stopCall.Arguments[0]);
        Assert.Equal(MatchingQueueResult.Declined, stopCall.Arguments[1]);
    }

    private static IMatchProposal CreateMatchProposal()
    {
        return new MatchProposal(
            NullLogger<MatchProposal>.Instance,
            new StubFactory<IMatchProposalTeam>(() => new MatchProposalTeam(
                new StubFactory<IMatchProposalTeamMember>(() => new MatchProposalTeamMember(
                    NullLogger<MatchProposalTeamMember>.Instance)))));
    }

    private static IMatchingCharacter CreateEmptyMatchingCharacter()
    {
        IMatchingCharacter matchingCharacter = RecordingDispatchProxy<IMatchingCharacter>.Create(out RecordingDispatchProxy<IMatchingCharacter> characterProxy);
        characterProxy.SetMethodReturn(nameof(IMatchingCharacter.GetMatchingCharacterQueues), Enumerable.Empty<IMatchingCharacterQueue>());
        return matchingCharacter;
    }

    private static IMatchingQueueGroup CreateReplacementGroup(Identity candidateIdentity)
    {
        IMatchingQueueProposal candidateProposal = RecordingDispatchProxy<IMatchingQueueProposal>.Create(out RecordingDispatchProxy<IMatchingQueueProposal> proposalProxy);
        proposalProxy.SetProperty(nameof(IMatchingQueueProposal.Guid), Guid.NewGuid());
        proposalProxy.SetProperty(nameof(IMatchingQueueProposal.Faction), Faction.Exile);
        proposalProxy.SetProperty(nameof(IMatchingQueueProposal.MatchType), MatchType.Adventure);
        proposalProxy.SetMethodReturn(nameof(IMatchingQueueProposal.GetMatchingMaps), new[] { CreateMatchingMap() });

        IMatchingQueueProposalMember candidateMember = RecordingDispatchProxy<IMatchingQueueProposalMember>.Create(out RecordingDispatchProxy<IMatchingQueueProposalMember> memberProxy);
        memberProxy.SetProperty(nameof(IMatchingQueueProposalMember.MatchingQueueProposal), candidateProposal);
        memberProxy.SetProperty(nameof(IMatchingQueueProposalMember.Identity), candidateIdentity);
        memberProxy.SetProperty(nameof(IMatchingQueueProposalMember.Roles), Role.Healer);
        proposalProxy.SetMethodReturn(nameof(IMatchingQueueProposal.GetMembers), new[] { candidateMember });

        IMatchingQueueGroupTeam team = RecordingDispatchProxy<IMatchingQueueGroupTeam>.Create(out RecordingDispatchProxy<IMatchingQueueGroupTeam> teamProxy);
        teamProxy.SetProperty(nameof(IMatchingQueueGroupTeam.Guid), Guid.NewGuid());
        teamProxy.SetProperty(nameof(IMatchingQueueGroupTeam.Faction), (Faction?)null);
        teamProxy.SetMethodReturn(nameof(IMatchingQueueGroupTeam.GetMembers), new[] { candidateMember });
        teamProxy.SetMethodReturn(nameof(IMatchingQueueGroupTeam.GetMatchingMaps), new[] { CreateMatchingMap() });

        IMatchingQueueGroup group = RecordingDispatchProxy<IMatchingQueueGroup>.Create(out RecordingDispatchProxy<IMatchingQueueGroup> groupProxy);
        groupProxy.SetProperty(nameof(IMatchingQueueGroup.Guid), Guid.NewGuid());
        groupProxy.SetProperty(nameof(IMatchingQueueGroup.MatchType), MatchType.Adventure);
        groupProxy.SetProperty(nameof(IMatchingQueueGroup.InProgress), true);
        groupProxy.SetProperty(nameof(IMatchingQueueGroup.IsSolo), false);
        groupProxy.SetMethodReturn(nameof(IMatchingQueueGroup.GetTeams), new[] { team });
        groupProxy.SetMethodReturn(nameof(IMatchingQueueGroup.GetMatchingMaps), new[] { CreateMatchingMap() });

        return group;
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

    private sealed class StubFactory<T>(Func<T> create) : IFactory<T> where T : class
    {
        public T Resolve()
        {
            return create();
        }
    }
}
