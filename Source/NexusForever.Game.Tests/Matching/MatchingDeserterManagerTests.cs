using NexusForever.Game.Matching;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using MatchType = NexusForever.Game.Static.Matching.MatchType;

namespace NexusForever.Game.Tests.Matching;

public class MatchingDeserterManagerTests
{
    [Fact]
    public void CrossActivity_PvEDeserterStillAllowsPvpQueue()
    {
        var manager = new MatchingDeserterManager();
        const ulong characterId = 42ul;

        manager.ApplyDeserter(characterId, MatchType.Dungeon, completionRatio: 0d);

        Assert.False(manager.CanQueue(characterId, MatchType.Dungeon));
        Assert.True(manager.CanQueue(characterId, MatchType.BattleGround));
    }

    [Fact]
    public void CrossActivity_PvPDeserterStillAllowsPveQueue()
    {
        var manager = new MatchingDeserterManager();
        const ulong characterId = 99ul;

        manager.ApplyDeserter(characterId, MatchType.Arena, completionRatio: 0d);

        Assert.False(manager.CanQueue(characterId, MatchType.Arena));
        Assert.True(manager.CanQueue(characterId, MatchType.Adventure));
    }

    [Fact]
    public void ClearDeserter_RestoresQueueAccess()
    {
        var manager = new MatchingDeserterManager();
        const ulong characterId = 7ul;

        manager.ApplyDeserter(characterId, MatchType.Dungeon, completionRatio: 0d);
        manager.ClearDeserter(characterId);

        Assert.True(manager.CanQueue(characterId, MatchType.Dungeon));
    }

    [Fact]
    public void GetMatchingPenaltyTimesMilliseconds_FillsSameActivityMatchTypesOnly()
    {
        var manager = new MatchingDeserterManager();
        const ulong characterId = 100ul;

        manager.ApplyDeserter(characterId, MatchType.Dungeon, completionRatio: 0d);

        uint[] penalties = manager.GetMatchingPenaltyTimesMilliseconds(characterId);
        Assert.NotEqual(0u, penalties[(int)MatchType.Dungeon]);
        Assert.NotEqual(0u, penalties[(int)MatchType.Adventure]);
        Assert.Equal(0u, penalties[(int)MatchType.BattleGround]);
        Assert.Equal(0u, penalties[(int)MatchType.Arena]);
    }

    [Fact]
    public void SyncDeserterUi_SendsMatchingPenaltyUpdate()
    {
        var manager = new MatchingDeserterManager();
        const ulong characterId = 101ul;
        IPlayer player = CreatePlayer(characterId, out RecordingDispatchProxy<IGameSession> sessionProxy);

        manager.ApplyDeserter(characterId, MatchType.Arena, completionRatio: 0d);
        manager.SyncDeserterUi(player);

        ServerMatchingPenaltyUpdated update = Assert.Single(GetEncryptedMessages<ServerMatchingPenaltyUpdated>(sessionProxy));
        Assert.NotEqual(0u, update.MatchingPenaltyTimesMS[(int)MatchType.Arena]);
        Assert.Equal(0u, update.MatchingPenaltyTimesMS[(int)MatchType.Dungeon]);
    }

    [Fact]
    public void PersistentPenaltyStore_RestoresQueueDenialAfterManagerRestart()
    {
        var store = new InMemoryMatchingPenaltyStore();
        var firstManager = new MatchingDeserterManager(store);
        const ulong characterId = 102ul;

        firstManager.ApplyDeserter(characterId, MatchType.Dungeon, completionRatio: 0d);

        Assert.NotNull(store.Get(characterId));

        var restoredManager = new MatchingDeserterManager(store);
        Assert.False(restoredManager.CanQueue(characterId, MatchType.Dungeon));
        Assert.True(restoredManager.CanQueue(characterId, MatchType.Arena));

        restoredManager.ClearDeserter(characterId);
        Assert.Null(store.Get(characterId));
    }

    [Fact]
    public void RestoreDeserter_UsesPersistentPenaltyForUiSync()
    {
        var store = new InMemoryMatchingPenaltyStore();
        const ulong characterId = 103ul;
        store.Save(characterId, new MatchingPenaltyState(
            IsPvP: false,
            MatchType.Dungeon,
            Spell4Id: 123u,
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(10)));
        var manager = new MatchingDeserterManager(store);
        IPlayer player = CreatePlayer(characterId, out RecordingDispatchProxy<IGameSession> sessionProxy);

        manager.RestoreDeserter(player);
        manager.SyncDeserterUi(player);

        ServerMatchingPenaltyUpdated update = Assert.Single(GetEncryptedMessages<ServerMatchingPenaltyUpdated>(sessionProxy));
        Assert.NotEqual(0u, update.MatchingPenaltyTimesMS[(int)MatchType.Dungeon]);
        Assert.Equal(0u, update.MatchingPenaltyTimesMS[(int)MatchType.Arena]);
    }

    private static IPlayer CreatePlayer(ulong characterId, out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        return player;
    }

    private static IReadOnlyList<T> GetEncryptedMessages<T>(RecordingDispatchProxy<IGameSession> sessionProxy)
        where T : class, IWritable
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Where(i => i.Arguments.Length == 1)
            .Select(i => i.Arguments[0])
            .OfType<T>()
            .ToList();
    }

    private sealed class InMemoryMatchingPenaltyStore : IMatchingPenaltyStore
    {
        private readonly Dictionary<ulong, MatchingPenaltyState> penalties = [];

        public MatchingPenaltyState? Get(ulong characterId)
        {
            return penalties.TryGetValue(characterId, out MatchingPenaltyState penalty)
                ? penalty
                : null;
        }

        public void Save(ulong characterId, MatchingPenaltyState state)
        {
            penalties[characterId] = state;
        }

        public void Delete(ulong characterId)
        {
            penalties.Remove(characterId);
        }
    }
}
