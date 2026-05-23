using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Static.Matching;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Matching.Queue;
using NexusForever.Network.Message;
using MatchingMatchType = NexusForever.Game.Static.Matching.MatchType;

namespace NexusForever.Game.Tests.Matching;

public class MatchingQueueValidatorTests
{
    [Fact]
    public void CanQueue_WithExistingSoloQueueAndIncomingPartyQueueReturnsCannotQueueSoloAndGroup()
    {
        Identity identity = new()
        {
            RealmId = 1,
            Id = 101ul
        };
        IMatchingQueueProposal incomingParty = CreateProposal(
            identity,
            isParty: true,
            matchType: MatchingMatchType.Dungeon,
            out _);
        IMatchingQueueProposal existingSolo = CreateProposal(
            identity,
            isParty: false,
            matchType: MatchingMatchType.BattleGround,
            out _);
        IMatchingCharacter matchingCharacter = CreateMatchingCharacter(existingSolo);
        MatchingQueueValidator validator = CreateValidator(identity, matchingCharacter);

        MatchingQueueResult? result = validator.CanQueue(incomingParty);

        Assert.Equal(MatchingQueueResult.CannotQueueSoloAndGroup, result);
    }

    private static MatchingQueueValidator CreateValidator(Identity identity, IMatchingCharacter matchingCharacter)
    {
        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(out _);
        IDisableManager disableManager = RecordingDispatchProxy<IDisableManager>.Create(out _);
        IMatchingManager matchingManager = RecordingDispatchProxy<IMatchingManager>.Create(out RecordingDispatchProxy<IMatchingManager> matchingManagerProxy);
        IMatchingDataManager matchingDataManager = RecordingDispatchProxy<IMatchingDataManager>.Create(out _);
        IMatchingRoleEnforcer matchingRoleEnforcer = RecordingDispatchProxy<IMatchingRoleEnforcer>.Create(out _);
        IMatchCharacterStore matchCharacterStore = RecordingDispatchProxy<IMatchCharacterStore>.Create(out RecordingDispatchProxy<IMatchCharacterStore> matchCharacterStoreProxy);
        IMatchCharacter matchCharacter = RecordingDispatchProxy<IMatchCharacter>.Create(out _);

        matchingManagerProxy.SetMethodReturn(nameof(IMatchingManager.GetMatchingCharacter), matchingCharacter);
        matchCharacterStoreProxy.SetMethodReturn(nameof(IMatchCharacterStore.GetMatchCharacter), matchCharacter);

        IMatchingDeserterManager matchingDeserterManager = RecordingDispatchProxy<IMatchingDeserterManager>.Create(out RecordingDispatchProxy<IMatchingDeserterManager> deserterProxy);
        deserterProxy.SetMethodReturn(nameof(IMatchingDeserterManager.CanQueue), true);

        return new MatchingQueueValidator(
            playerManager,
            disableManager,
            matchingManager,
            matchingDataManager,
            matchingRoleEnforcer,
                matchCharacterStore,
            matchingDeserterManager);
    }

    private static IMatchingCharacter CreateMatchingCharacter(IMatchingQueueProposal existingProposal)
    {
        IMatchingCharacter matchingCharacter = RecordingDispatchProxy<IMatchingCharacter>.Create(out RecordingDispatchProxy<IMatchingCharacter> characterProxy);
        IMatchingCharacterQueue existingQueue = new MatchingCharacterQueue
        {
            MatchingQueueProposal = existingProposal
        };

        characterProxy.SetMethodReturn(nameof(IMatchingCharacter.GetMatchingCharacterQueue), null);
        characterProxy.SetMethodReturn(nameof(IMatchingCharacter.GetMatchingCharacterQueues), new[] { existingQueue });
        return matchingCharacter;
    }

    private static IMatchingQueueProposal CreateProposal(
        Identity identity,
        bool isParty,
        MatchingMatchType matchType,
        out IMatchingQueueProposalMember member)
    {
        IMatchingQueueProposal proposal = RecordingDispatchProxy<IMatchingQueueProposal>.Create(out RecordingDispatchProxy<IMatchingQueueProposal> proposalProxy);
        member = RecordingDispatchProxy<IMatchingQueueProposalMember>.Create(out RecordingDispatchProxy<IMatchingQueueProposalMember> memberProxy);

        proposalProxy.SetProperty(nameof(IMatchingQueueProposal.IsParty), isParty);
        proposalProxy.SetProperty(nameof(IMatchingQueueProposal.MatchType), matchType);
        proposalProxy.SetProperty(nameof(IMatchingQueueProposal.Faction), Faction.Dominion);
        proposalProxy.SetMethodReturn(nameof(IMatchingQueueProposal.GetMembers), new[] { member });
        memberProxy.SetProperty(nameof(IMatchingQueueProposalMember.Identity), identity);
        return proposal;
    }
}
