using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Group;
using NexusForever.Game.Matching;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Group;
using NexusForever.Game.Static.Matching;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Shared;
using MatchType = NexusForever.Game.Static.Matching.MatchType;

namespace NexusForever.Game.Tests.Matching;

public class RetailPartyQueueRulesTests
{
    [Fact]
    public void ValidateGroupLeader_WhenNotLeader_ReturnsRestricted()
    {
        Identity leader = new() { RealmId = 1, Id = 1 };
        Identity member = new() { RealmId = 1, Id = 2 };
        var groupState = new GroupStateManager();
        groupState.UpdateGroup(CreateGroup(leader, member));

        IPlayer memberPlayer = CreatePlayer(member);
        MatchingQueueResult? result = RetailPartyQueueRules.ValidateGroupLeader(memberPlayer, groupState);

        Assert.Equal(MatchingQueueResult.GroupMemberPrivilegeRestricted, result);
    }

    [Fact]
    public void ResolvePartyMemberIdentities_UsesGroupMembers_NotOnlyNearby()
    {
        Identity leader = new() { RealmId = 1, Id = 1 };
        Identity member = new() { RealmId = 1, Id = 2 };
        var groupState = new GroupStateManager();
        groupState.UpdateGroup(CreateGroup(leader, member));

        IPlayer leaderPlayer = CreatePlayer(leader);
        List<Identity> identities = RetailPartyQueueRules.ResolvePartyMemberIdentities(
            leaderPlayer,
            groupState,
            []);

        Assert.Equal(2, identities.Count);
        Assert.Contains(leader, identities);
        Assert.Contains(member, identities);
    }

    [Fact]
    public void TryCreateFinishedGroupRequeueProposal_WhenAllFinished_CreatesProposal()
    {
        Identity leader = new() { RealmId = 1, Id = 1 };
        Identity member = new() { RealmId = 1, Id = 2 };
        var groupState = new GroupStateManager();
        groupState.UpdateGroup(CreateGroup(leader, member));

        IPlayer leaderPlayer = CreatePlayer(leader, Class.Warrior);
        IPlayer memberPlayer = CreatePlayer(member, Class.Medic);

        IPlayerManager playerManager = new StubPlayerManager(leaderPlayer, memberPlayer);
        IMatchManager matchManager = new StubFinishedMatchManager(leader, member);

        RecordingDispatchProxy<IMatchingQueueProposal> proposalProxy = null;
        IFactory<IMatchingQueueProposal> proposalFactory = new StubFactory<IMatchingQueueProposal>(() =>
            RecordingDispatchProxy<IMatchingQueueProposal>.Create(out proposalProxy));
        IMatchingDataManager dataManager = CreateDataManager(canRequeueAsGroup: true);

        bool created = RetailPartyQueueRules.TryCreateFinishedGroupRequeueProposal(
            leaderPlayer,
            Role.Tank,
            MatchType.Dungeon,
            [],
            MatchingQueueFlags.None,
            groupState,
            playerManager,
            matchManager,
            dataManager,
            proposalFactory,
            out IMatchingQueueProposal proposal);

        Assert.True(created);
        Assert.NotNull(proposal);
        Assert.Equal(2, proposalProxy.GetInvocations(nameof(IMatchingQueueProposal.AddMember)).Count);
    }

    static GroupLootState CreateGroup(Identity leader, Identity member) => new()
    {
        GroupId          = 1,
        NormalRule       = LootRule.FreeForAll,
        ThresholdRule    = LootRule.Master,
        ThresholdQuality = LootThreshold.Superb,
        HarvestRule      = HarvestLootRule.RoundRobin,
        Leader           = leader,
        Members =
        [
            new GroupLootMember { Identity = leader, GroupIndex = 0 },
            new GroupLootMember { Identity = member, GroupIndex = 1 }
        ]
    };

    static IPlayer CreatePlayer(Identity identity, Class @class = Class.Warrior)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> proxy);
        proxy.SetProperty(nameof(IPlayer.Identity), identity);
        proxy.SetProperty(nameof(IPlayer.Class), @class);
        proxy.SetProperty(nameof(IPlayer.Faction1), (Faction)1);
        return player;
    }

    static IMatchingDataManager CreateDataManager(bool canRequeueAsGroup)
    {
        IMatchingDataManager data = RecordingDispatchProxy<IMatchingDataManager>.Create(out RecordingDispatchProxy<IMatchingDataManager> proxy);
        proxy.SetMethodReturn(nameof(IMatchingDataManager.CanRequeueAsGroup), canRequeueAsGroup);
        proxy.SetMethodReturn(nameof(IMatchingDataManager.GetDefaultRole), Role.DPS);
        return data;
    }

    sealed class StubPlayerManager(params IPlayer[] players) : IPlayerManager
    {
        readonly Dictionary<Identity, IPlayer> byIdentity = players.ToDictionary(p => p.Identity);

        public IPlayer GetPlayer(Identity identity) => byIdentity.GetValueOrDefault(identity);
        public IPlayer GetPlayer(ulong characterId) => byIdentity.Values.FirstOrDefault(p => p.Identity.Id == characterId);
        public IPlayer GetPlayer(string name) => throw new NotImplementedException();
        public IPlayer GetPlayerByAccountId(uint accountId) => throw new NotImplementedException();
        public void AddPlayer(IPlayer player) => throw new NotImplementedException();
        public void RemovePlayer(IPlayer player) => throw new NotImplementedException();
        public IEnumerator<IPlayer> GetEnumerator() => byIdentity.Values.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    sealed class StubFinishedMatchManager(params Identity[] identities) : IMatchManager
    {
        readonly Dictionary<Identity, IMatchCharacter> characters = identities.ToDictionary(
            id => id,
            id =>
            {
                IMatchCharacter matchCharacter = RecordingDispatchProxy<IMatchCharacter>.Create(out RecordingDispatchProxy<IMatchCharacter> characterProxy);
                IMatch match = RecordingDispatchProxy<IMatch>.Create(out RecordingDispatchProxy<IMatch> matchProxy);
                matchProxy.SetProperty(nameof(IMatch.Status), MatchStatus.Finished);
                characterProxy.SetProperty(nameof(IMatchCharacter.Match), match);
                return matchCharacter;
            });

        public void Update(double lastTick) { }

        public void OnLogin(IPlayer player) { }

        public void OnLogout(IPlayer player) { }

        public void CreateMatchProposal(IMatchingQueueGroup matchingQueueGroup, IMatchingMapSelectorResult matchingMapSelectorResult) { }

        public IMatchCharacter GetMatchCharacter(Identity identity) => characters[identity];

        public IMatch GetMatch(Guid guid) => throw new NotImplementedException();
    }

    sealed class StubFactory<T>(Func<T> create) : IFactory<T> where T : class
    {
        public T Resolve() => create();
    }
}
