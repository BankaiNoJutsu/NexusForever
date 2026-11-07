using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_PATH_EPISODE_20261107.md): prerequisite type 53 (evaluator case 0x35 -> FUN_14049fd80) is true when Value equals the id of the path service's current
// episode (the object at DAT_140c65970 + 0x58, id read through its definition at +0x48), false when there is no service or no current episode; comparison 1 returns that, 2 its negation, anything else
// false. ObjectId is not read (0 in all 33 retail slots); Value is a PathEpisode id (121, 284, 246, 454, 238, ...). The server sends the current episode to the client with ServerPathSetCurrentEpisode and
// kept no copy. Status: Implemented, inferred (that the client's current episode is the last one the server set is assumed).
public class PrerequisiteCheckPathEpisodeTests
{
    [Theory]
    [InlineData(121u, PrerequisiteComparison.Equal, true)]
    [InlineData(284u, PrerequisiteComparison.Equal, false)]
    [InlineData(121u, PrerequisiteComparison.NotEqual, false)]
    [InlineData(284u, PrerequisiteComparison.NotEqual, true)]
    [InlineData(121u, PrerequisiteComparison.GreaterThan, false)]
    [InlineData(121u, (PrerequisiteComparison)0, false)]
    public void ComparesValueWithTheCurrentPathEpisode(uint value, PrerequisiteComparison comparison, bool expected)
    {
        IPlayer player = Player(currentEpisode: 121u);

        Assert.Equal(expected, new PrerequisiteCheckPathEpisode().Meets(player, comparison, value, 55u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 0u, false)]       // no current episode: nothing matches, even Value 0
    [InlineData(PrerequisiteComparison.NotEqual, 0u, true)]
    [InlineData(PrerequisiteComparison.Equal, 121u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 121u, true)]
    public void WithoutACurrentEpisodeNothingMatches(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = Player(currentEpisode: 0u);

        Assert.Equal(expected, new PrerequisiteCheckPathEpisode().Meets(player, comparison, value, 0u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true)]
    public void WithoutAPathManagerThereIsNoCurrentEpisode(PrerequisiteComparison comparison, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        Assert.Equal(expected, new PrerequisiteCheckPathEpisode().Meets(player, comparison, 121u, 0u, new PrerequisiteParameters()));
    }

    private static IPlayer Player(uint currentEpisode)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IPathManager pathManager = RecordingDispatchProxy<IPathManager>.Create(out var pathProxy);
        pathProxy.SetProperty(nameof(IPathManager.CurrentEpisodeId), currentEpisode);
        playerProxy.SetProperty(nameof(IPlayer.PathManager), pathManager);
        return player;
    }
}
