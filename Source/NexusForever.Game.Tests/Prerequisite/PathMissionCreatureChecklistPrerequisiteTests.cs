using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Prerequisite 107 (native FUN_14049fff0, Decomp/Analysis/PREREQUISITE_107_PATH_MISSION_WORD_20261108.md): a Player or Ghost subject with no target or a Player/Ghost target gives
// Equal false / NotEqual true; otherwise the unit is the target creature, a missing mission or a negative QuestChecklistIdx (signed byte) gives word 0, else the word is the mission's
// progress word (the ProgressCount of ServerPathMissionUpdate, the field behind mission vtable +0x40) masked with 1 << (idx & 31). Equal returns the word as a flag, NotEqual word == 0.
public class PathMissionCreatureChecklistPrerequisiteTests
{
    private const uint MissionId = 77u;
    private static readonly IPrerequisiteParameters None = new PrerequisiteParameters();

    [Theory]
    [InlineData((byte)0, 0b101u, true)]
    [InlineData((byte)1, 0b101u, false)]
    [InlineData((byte)2, 0b101u, true)]
    [InlineData((byte)34, 0b100u, true)]   // the index wraps at 32
    [InlineData((byte)255, 0b101u, false)] // -1: no checklist index
    [InlineData((byte)130, 0b101u, false)] // negative as a signed byte
    public void TestsTheTargetCreaturesChecklistBitOfTheMissionProgressWord(byte idx, uint word, bool expected)
    {
        IPlayer player = Player(word, found: true);
        IPrerequisiteParameters parameters = new PrerequisiteParameters { Target = Creature(idx) };
        var check = new PrerequisiteCheckPathMissionCreatureChecklist();

        Assert.Equal(expected, check.Meets(player, PrerequisiteComparison.Equal, 0u, MissionId, parameters));
        Assert.Equal(!expected, check.Meets(player, PrerequisiteComparison.NotEqual, 0u, MissionId, parameters));
        Assert.False(check.Meets(player, PrerequisiteComparison.GreaterThan, 0u, MissionId, parameters));
    }

    [Fact]
    public void AMissionThePlayerDoesNotHaveReadsAsZero()
    {
        IPlayer player = Player(uint.MaxValue, found: false);
        IPrerequisiteParameters parameters = new PrerequisiteParameters { Target = Creature(0) };
        var check = new PrerequisiteCheckPathMissionCreatureChecklist();

        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 0u, MissionId, parameters));
        Assert.True(check.Meets(player, PrerequisiteComparison.NotEqual, 0u, MissionId, parameters));
    }

    [Theory]
    [InlineData(EntityType.Player)]
    [InlineData(EntityType.Ghost)]
    public void APlayerSubjectWithNoCreatureTargetIsConstant(EntityType targetType)
    {
        IPlayer player = Player(uint.MaxValue, found: true);
        var check = new PrerequisiteCheckPathMissionCreatureChecklist();

        foreach (IPrerequisiteParameters parameters in new[] { None, new PrerequisiteParameters { Target = Creature(0, targetType) } })
        {
            Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 0u, MissionId, parameters));
            Assert.True(check.Meets(player, PrerequisiteComparison.NotEqual, 0u, MissionId, parameters));
        }
    }

    [Fact]
    public void WithoutAPathManagerTheWordIsZero()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IPrerequisiteParameters parameters = new PrerequisiteParameters { Target = Creature(0) };

        Assert.False(new PrerequisiteCheckPathMissionCreatureChecklist().Meets(player, PrerequisiteComparison.Equal, 0u, MissionId, parameters));
        Assert.True(new PrerequisiteCheckPathMissionCreatureChecklist().Meets(player, PrerequisiteComparison.NotEqual, 0u, MissionId, parameters));
    }

    private static IUnitEntity Creature(byte checklistIdx, EntityType type = EntityType.NonPlayer)
    {
        IUnitEntity entity = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.Type), type);
        proxy.SetProperty(nameof(IUnitEntity.QuestChecklistIdx), checklistIdx);
        return entity;
    }

    private static IPlayer Player(uint word, bool found)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Type), EntityType.Player);
        IPathManager pathManager = RecordingDispatchProxy<IPathManager>.Create(out var pathProxy);
        pathProxy.SetMethodHandler(nameof(IPathManager.TryGetMissionProgress), args =>
        {
            args[1] = word;
            return found && (uint)args[0] == MissionId;
        });
        playerProxy.SetProperty(nameof(IPlayer.PathManager), pathManager);
        return player;
    }
}
