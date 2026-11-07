using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_PATH_MISSION_COUNT_20261107.md): prerequisite type 110 (evaluator case 0x6e -> FUN_1404a00e0) finds the player's path mission with id ObjectId and compares
// its progress value with Value. The mission class's virtual +0x30 chooses the value: for definition types 3, 0xd, 0xe, 0x11, 0x14, 0x17, 0x19, 0x1a and 0x1b it is the population count of the progress word
// (+0x48 of the class), otherwise the progress word itself (+0x40); the word is the ProgressCount the server sends in ServerPathMissionUpdate (constructor value 2). The compare is the integer helper
// (progress first, Value second). Retail: 47 slots with Value counts (3, 4, 6, 30, 60) and percentages (33, 66, 100), comparisons == 26, >= 7, > 6, <= 5, < 3. Status: Implemented, inferred (definition
// types 5 and 6 depend on a table row that was not read and are treated as plain counts; a missing mission counts as 0).
public class PrerequisiteCheckPlayerPathMissionCountTests
{
    [Theory]
    [InlineData(2u, 30u, PrerequisiteComparison.Equal, 30u, true)]        // type 2: the plain progress word
    [InlineData(2u, 30u, PrerequisiteComparison.GreaterThanOrEqual, 31u, false)]
    [InlineData(0x13u, 60u, PrerequisiteComparison.GreaterThan, 59u, true)]
    [InlineData(3u, 0b1011u, PrerequisiteComparison.Equal, 3u, true)]     // type 3: bits set in the word
    [InlineData(0x19u, 0b1111111u, PrerequisiteComparison.Equal, 7u, true)]
    [InlineData(0xdu, 0b1111111u, PrerequisiteComparison.GreaterThanOrEqual, 8u, false)]
    [InlineData(0x1bu, 0xffffffffu, PrerequisiteComparison.Equal, 32u, true)]
    [InlineData(3u, 0b1011u, PrerequisiteComparison.NotEqual, 3u, false)]
    [InlineData(3u, 0b1011u, PrerequisiteComparison.LessThan, 4u, true)]
    [InlineData(3u, 0b1011u, PrerequisiteComparison.LessThanOrEqual, 2u, false)]
    [InlineData(2u, 30u, (PrerequisiteComparison)0, 30u, false)]
    public void ComparesTheMissionProgressChosenByItsDefinitionType(uint missionType, uint progress, PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = Player(progress, found: true, out _);
        var check = new PrerequisiteCheckPlayerPathMissionCount(Tables(missionType));

        Assert.Equal(expected, check.Meets(player, comparison, value, 77u, new PrerequisiteParameters()));
    }

    [Fact]
    public void ObjectIdIsTheMissionId()
    {
        IPlayer player = Player(5u, found: true, out var pathProxy);

        new PrerequisiteCheckPlayerPathMissionCount(Tables(2u)).Meets(player, PrerequisiteComparison.Equal, 5u, 77u, new PrerequisiteParameters());

        var call = Assert.Single(pathProxy.GetInvocations(nameof(IPathManager.TryGetMissionProgress)));
        Assert.Equal(77u, call.Arguments[0]);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 0u, true)]       // a missing mission counts as 0
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 1u, false)]
    [InlineData(PrerequisiteComparison.LessThan, 3u, true)]
    public void AMissionTheServerDoesNotHaveCountsAsZero(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = Player(99u, found: false, out _);

        Assert.Equal(expected, new PrerequisiteCheckPlayerPathMissionCount(Tables(2u)).Meets(player, comparison, value, 77u, new PrerequisiteParameters()));
    }

    [Fact]
    public void WithoutAPathManagerThereIsNoProgress()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        Assert.True(new PrerequisiteCheckPlayerPathMissionCount(Tables(2u)).Meets(player, PrerequisiteComparison.Equal, 0u, 77u, new PrerequisiteParameters()));
    }

    private static IPlayer Player(uint progress, bool found, out RecordingDispatchProxy<IPathManager> pathProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IPathManager pathManager = RecordingDispatchProxy<IPathManager>.Create(out pathProxy);
        pathProxy.SetMethodHandler(nameof(IPathManager.TryGetMissionProgress), args =>
        {
            args[1] = progress;
            return found;
        });
        playerProxy.SetProperty(nameof(IPlayer.PathManager), pathManager);
        return player;
    }

    private static IGameTableManager Tables(uint missionType)
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.PathMission), PlayerBindPointTests.Table(new PathMissionEntry { Id = 77u, PathMissionTypeEnum = missionType }));
        return tables;
    }
}
