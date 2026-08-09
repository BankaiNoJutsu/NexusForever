using NexusForever.Database;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Matching;
using NexusForever.Game.Matching.Queue;
using NexusForever.Game.Static.Matching;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using MatchingMatchType = NexusForever.Game.Static.Matching.MatchType;

namespace NexusForever.Game.Tests.Matching;

public class MatchingGameMap71Tests
{
    [Fact]
    public void ScaledPrimeUltimateProtogamesMap71_ExactMetadataRequiresRoleSelection()
    {
        MatchingMap matchingMap = CreateMatchingMap71();
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out _);
        IDatabaseManager databaseManager = RecordingDispatchProxy<IDatabaseManager>.Create(out _);
        var matchingDataManager = new MatchingDataManager(gameTableManager, databaseManager);

        Assert.Equal(71u, matchingMap.Id);
        Assert.Equal(2980u, matchingMap.GameMapEntry.WorldId);
        Assert.Equal(87u, matchingMap.GameMapEntry.MatchingGameTypeId);
        Assert.Equal(70u, matchingMap.GameMapEntry.RecommendedItemLevel);
        Assert.Equal(316u, matchingMap.GameMapEntry.AchievementCategoryId);
        Assert.Equal(MatchingMatchType.ScaledPrimeLevelDungeon, matchingMap.GameTypeEntry.MatchTypeEnum);
        Assert.Equal(5u, matchingMap.GameTypeEntry.TeamSize);
        Assert.Equal(50u, matchingMap.GameTypeEntry.MinLevel);
        Assert.Equal(50u, matchingMap.GameTypeEntry.MaxLevel);
        Assert.True(matchingDataManager.RequiresRoleSelection(matchingMap.GameTypeEntry.MatchTypeEnum));
        Assert.False(matchingDataManager.RequiresRoleSelection(MatchingMatchType.ScaledPrimeLevelExpedition));
    }

    [Fact]
    public void ScaledPrimeUltimateProtogamesMap71_QueueValidatorAcceptsExactLevelAndRole()
    {
        MatchingQueueValidator validator = CreateValidator(
            level: 50u,
            matchType: MatchingMatchType.ScaledPrimeLevelDungeon,
            hasMapEntrance: true,
            roles: Role.DPS,
            out IMatchingQueueProposal proposal);

        MatchingQueueResult? result = validator.CanQueue(proposal);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(49u, MatchingMatchType.ScaledPrimeLevelDungeon, true, Role.DPS, MatchingQueueResult.Level)]
    [InlineData(50u, MatchingMatchType.PrimeLevelDungeon, true, Role.DPS, MatchingQueueResult.TypeMismatch)]
    [InlineData(50u, MatchingMatchType.ScaledPrimeLevelDungeon, false, Role.DPS, MatchingQueueResult.UnableToQueue)]
    [InlineData(50u, MatchingMatchType.ScaledPrimeLevelDungeon, true, Role.None, MatchingQueueResult.GroupSize)]
    public void ScaledPrimeUltimateProtogamesMap71_QueueValidatorRejectsInvalidBoundary(
        uint level,
        MatchingMatchType matchType,
        bool hasMapEntrance,
        Role roles,
        MatchingQueueResult expected)
    {
        MatchingQueueValidator validator = CreateValidator(
            level,
            matchType,
            hasMapEntrance,
            roles,
            out IMatchingQueueProposal proposal);

        MatchingQueueResult? result = validator.CanQueue(proposal);

        Assert.Equal(expected, result);
    }

    private static MatchingQueueValidator CreateValidator(
        uint level,
        MatchingMatchType matchType,
        bool hasMapEntrance,
        Role roles,
        out IMatchingQueueProposal proposal)
    {
        Identity identity = new()
        {
            RealmId = 1,
            Id      = 71ul
        };

        proposal = RecordingDispatchProxy<IMatchingQueueProposal>.Create(out RecordingDispatchProxy<IMatchingQueueProposal> proposalProxy);
        IMatchingQueueProposalMember member = RecordingDispatchProxy<IMatchingQueueProposalMember>.Create(out RecordingDispatchProxy<IMatchingQueueProposalMember> memberProxy);
        memberProxy.SetProperty(nameof(IMatchingQueueProposalMember.Identity), identity);
        memberProxy.SetProperty(nameof(IMatchingQueueProposalMember.Roles), roles);
        proposalProxy.SetProperty(nameof(IMatchingQueueProposal.IsParty), false);
        proposalProxy.SetProperty(nameof(IMatchingQueueProposal.MatchType), matchType);
        proposalProxy.SetProperty(nameof(IMatchingQueueProposal.Faction), Faction.Dominion);
        proposalProxy.SetMethodReturn(nameof(IMatchingQueueProposal.GetMembers), new[] { member });
        proposalProxy.SetMethodReturn(nameof(IMatchingQueueProposal.GetMatchingMaps), new IMatchingMap[] { CreateMatchingMap71() });

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Faction1), Faction.Dominion);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), identity.Id);
        playerProxy.SetProperty(nameof(IPlayer.Level), level);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        IDisableManager disableManager = RecordingDispatchProxy<IDisableManager>.Create(out _);

        IMatchingCharacter matchingCharacter = RecordingDispatchProxy<IMatchingCharacter>.Create(out RecordingDispatchProxy<IMatchingCharacter> matchingCharacterProxy);
        matchingCharacterProxy.SetMethodReturn(nameof(IMatchingCharacter.GetMatchingCharacterQueue), null);
        matchingCharacterProxy.SetMethodReturn(nameof(IMatchingCharacter.GetMatchingCharacterQueues), Array.Empty<IMatchingCharacterQueue>());
        IMatchingManager matchingManager = RecordingDispatchProxy<IMatchingManager>.Create(out RecordingDispatchProxy<IMatchingManager> matchingManagerProxy);
        matchingManagerProxy.SetMethodReturn(nameof(IMatchingManager.GetMatchingCharacter), matchingCharacter);

        IMatchingDataManager matchingDataManager = RecordingDispatchProxy<IMatchingDataManager>.Create(out RecordingDispatchProxy<IMatchingDataManager> matchingDataManagerProxy);
        matchingDataManagerProxy.SetMethodReturn(nameof(IMatchingDataManager.RequiresRoleSelection), true);
        IMapEntrance mapEntrance = hasMapEntrance
            ? new MapEntrance
            {
                MapId    = 2980,
                Team     = 0,
                Position = System.Numerics.Vector3.Zero,
                Rotation = System.Numerics.Vector3.Zero
            }
            : null;
        matchingDataManagerProxy.SetMethodReturn(nameof(IMatchingDataManager.GetMapEntrance), mapEntrance);

        IMatchingRoleEnforcerResult roleResult = RecordingDispatchProxy<IMatchingRoleEnforcerResult>.Create(out RecordingDispatchProxy<IMatchingRoleEnforcerResult> roleResultProxy);
        roleResultProxy.SetProperty(nameof(IMatchingRoleEnforcerResult.Success), roles != Role.None);
        IMatchingRoleEnforcer matchingRoleEnforcer = RecordingDispatchProxy<IMatchingRoleEnforcer>.Create(out RecordingDispatchProxy<IMatchingRoleEnforcer> matchingRoleEnforcerProxy);
        matchingRoleEnforcerProxy.SetMethodReturn(nameof(IMatchingRoleEnforcer.Check), roleResult);

        IMatchCharacter matchCharacter = RecordingDispatchProxy<IMatchCharacter>.Create(out RecordingDispatchProxy<IMatchCharacter> matchCharacterProxy);
        matchCharacterProxy.SetProperty(nameof(IMatchCharacter.Match), null);
        IMatchCharacterStore matchCharacterStore = RecordingDispatchProxy<IMatchCharacterStore>.Create(out RecordingDispatchProxy<IMatchCharacterStore> matchCharacterStoreProxy);
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

    private static MatchingMap CreateMatchingMap71()
    {
        return new MatchingMap
        {
            GameMapEntry = new MatchingGameMapEntry
            {
                Id                      = 71u,
                MatchingGameMapEnumFlags = 1u,
                LocalizedTextIdName     = 648662u,
                LocalizedTextIdDescription = 648663u,
                MatchingGameTypeId      = 87u,
                WorldId                 = 2980u,
                RecommendedItemLevel    = 70u,
                AchievementCategoryId   = 316u,
                PrerequisiteId          = 0u
            },
            GameTypeEntry = new MatchingGameTypeEntry
            {
                Id                        = 87u,
                LocalizedTextIdName       = 766840u,
                LocalizedTextIdDescription = 766841u,
                MatchTypeEnum             = MatchingMatchType.ScaledPrimeLevelDungeon,
                MatchingGameTypeEnumFlags = 419u,
                TeamSize                  = 5u,
                MinLevel                  = 50u,
                MaxLevel                  = 50u
            }
        };
    }
}
