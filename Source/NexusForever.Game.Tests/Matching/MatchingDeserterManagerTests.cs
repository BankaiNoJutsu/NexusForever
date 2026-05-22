using NexusForever.Game.Matching;
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
}
