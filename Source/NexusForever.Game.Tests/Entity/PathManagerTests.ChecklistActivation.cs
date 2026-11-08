using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NexusForever.Game.Entity;
using NexusForever.Game.Tests.TestSupport;
using Path = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Tests.Entity;

// Checklist missions keep a bitmask in the mission's ProgressCount word: the client shows popcount(ProgressCount) as the progress for mission types 17 (Explorer_ActivateChecklist) and, for activate
// modes 1 and 2, 5 and 6 (Soldier_Demolition / Soldier_Rescue), and prerequisite 107 tests the bit of the activated creature's QuestChecklistIdx
// (Decomp/Analysis/PREREQUISITE_107_PATH_MISSION_WORD_20261108.md, agent-written native read). The producer rule is inferred: activating a matching target (PathExplorerActivate / PathSoldierActivate
// row: Creature2Id or TargetGroupId) sets the bit of its QuestChecklistIdx once; the mission completes when popcount >= Count; activate mode 0 is a plain count.
public partial class PathManagerTests
{
    private const ushort ChecklistMissionId = 60;

    [Fact]
    public void ActivateChecklist_SetsOneBitPerDistinctTargetAndCompletesAtTheRequiredCount()
    {
        PathManager manager = ChecklistManager(Path.Explorer, missionType: 0x11u, out var session,
            explorerActivate: new PathExplorerActivateEntry { Id = 7u, Creature2Id = 5001u, Count = 3u });

        Assert.True(manager.ProgressActivateChecklistMissions(5001u, [], 0));
        Assert.Equal(0b001u, Progress(manager));
        Assert.True(manager.ProgressActivateChecklistMissions(5001u, [], 2));
        Assert.Equal(0b101u, Progress(manager));
        Assert.False(manager.IsMissionComplete(ChecklistMissionId));

        Assert.False(manager.ProgressActivateChecklistMissions(5001u, [], 2)); // the same target again changes nothing
        Assert.Equal(2, Updates(session).Count);

        Assert.True(manager.ProgressActivateChecklistMissions(5001u, [], 1));
        Assert.True(manager.IsMissionComplete(ChecklistMissionId));
        var last = Updates(session).Last();
        Assert.True(last.Mission.Completed);
        Assert.Equal(0b111u, last.Mission.ProgressCount);
    }

    [Fact]
    public void ActivateChecklist_MatchesATargetGroup()
    {
        PathManager manager = ChecklistManager(Path.Explorer, missionType: 0x11u, out _,
            explorerActivate: new PathExplorerActivateEntry { Id = 7u, TargetGroupId = 99u, Count = 2u });

        Assert.False(manager.ProgressActivateChecklistMissions(1234u, [5u], 0));
        Assert.True(manager.ProgressActivateChecklistMissions(1234u, [5u, 99u], 3));
        Assert.Equal(0b1000u, Progress(manager));
    }

    [Fact]
    public void ActivateChecklist_TheChecklistIndexWrapsAtThirtyTwoBitsLikePrerequisite107()
    {
        PathManager manager = ChecklistManager(Path.Explorer, missionType: 0x11u, out _,
            explorerActivate: new PathExplorerActivateEntry { Id = 7u, Creature2Id = 5001u, Count = 5u });

        Assert.True(manager.ProgressActivateChecklistMissions(5001u, [], 33));

        Assert.Equal(0b10u, Progress(manager));
    }

    [Fact]
    public void ActivateChecklist_IgnoresAnotherPathAndACompletedMission()
    {
        PathManager soldier = ChecklistManager(Path.Soldier, missionType: 0x11u, out _,
            explorerActivate: new PathExplorerActivateEntry { Id = 7u, Creature2Id = 5001u, Count = 1u });
        // an Explorer mission type on a Soldier mission row (PathTypeEnum Explorer) is not progressed by a Soldier player
        Assert.False(soldier.ProgressActivateChecklistMissions(5001u, [], 0));

        PathManager explorer = ChecklistManager(Path.Explorer, missionType: 0x11u, out _,
            explorerActivate: new PathExplorerActivateEntry { Id = 7u, Creature2Id = 5001u, Count = 1u });
        Assert.True(explorer.ProgressActivateChecklistMissions(5001u, [], 0));
        Assert.True(explorer.IsMissionComplete(ChecklistMissionId));
        Assert.False(explorer.ProgressActivateChecklistMissions(5001u, [], 1));
    }

    [Theory]
    [InlineData(5u, 1u)]
    [InlineData(6u, 1u)]
    [InlineData(6u, 2u)]
    public void SoldierActivate_ModesOneAndTwoUseTheBitmask(uint missionType, uint mode)
    {
        PathManager manager = ChecklistManager(Path.Soldier, missionType, out _,
            soldierActivate: new PathSoldierActivateEntry { Id = 7u, Creature2Id = 5001u, Count = 2u, SoldierActivateModeEnum = mode });

        Assert.True(manager.ProgressActivateChecklistMissions(5001u, [], 4));
        Assert.False(manager.ProgressActivateChecklistMissions(5001u, [], 4));
        Assert.Equal(0b10000u, Progress(manager));
        Assert.True(manager.ProgressActivateChecklistMissions(5001u, [], 0));
        Assert.True(manager.IsMissionComplete(ChecklistMissionId));
    }

    [Fact]
    public void SoldierActivate_ModeZeroIsAPlainCount()
    {
        PathManager manager = ChecklistManager(Path.Soldier, missionType: 5u, out _,
            soldierActivate: new PathSoldierActivateEntry { Id = 7u, Creature2Id = 5001u, Count = 2u, SoldierActivateModeEnum = 0u });

        Assert.True(manager.ProgressActivateChecklistMissions(5001u, [], 4));
        Assert.Equal(1u, Progress(manager));
        Assert.True(manager.ProgressActivateChecklistMissions(5001u, [], 4)); // same target counts again in count mode
        Assert.True(manager.IsMissionComplete(ChecklistMissionId));
    }

    [Fact]
    public void ActivateChecklist_ANonMatchingCreatureChangesNothing()
    {
        PathManager manager = ChecklistManager(Path.Explorer, missionType: 0x11u, out _,
            explorerActivate: new PathExplorerActivateEntry { Id = 7u, Creature2Id = 5001u, Count = 3u });

        Assert.False(manager.ProgressActivateChecklistMissions(9999u, [], 0));
        Assert.Equal(0u, Progress(manager));
    }

    private uint Progress(PathManager manager)
    {
        Assert.True(manager.TryGetMissionProgress(ChecklistMissionId, out uint progress));
        return progress;
    }

    private static List<ServerPathMissionUpdate> Updates(RecordingDispatchProxy<IGameSession> session)
        => session.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(i => i.Arguments[0]).OfType<ServerPathMissionUpdate>().ToList();

    private PathManager ChecklistManager(Path path, uint missionType, out RecordingDispatchProxy<IGameSession> session,
        PathExplorerActivateEntry explorerActivate = null, PathSoldierActivateEntry soldierActivate = null)
    {
        BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)path, PathLevel = 1u, PathXP = 0u }
            ],
            [],
            [
                new PathMissionEntry
                {
                    Id = ChecklistMissionId,
                    PathEpisodeId = 8u,
                    PathTypeEnum = (uint)(missionType == 0x11u ? Path.Explorer : Path.Soldier),
                    PathMissionTypeEnum = missionType,
                    ObjectId = 7u
                }
            ]);

        SetAutoProperty(configuredGameTableManager, nameof(NexusForever.GameTable.GameTableManager.PathExplorerActivate), CreateGameTable<PathExplorerActivateEntry>(explorerActivate == null ? [] : [explorerActivate]));
        SetAutoProperty(configuredGameTableManager, nameof(NexusForever.GameTable.GameTableManager.PathSoldierActivate), CreateGameTable<PathSoldierActivateEntry>(soldierActivate == null ? [] : [soldierActivate]));

        PathManager manager = CreateManager(path, totalXp: 0u, levelRewarded: 1, out _, out _, out session, out _, out _);
        manager.ActivateMissions(8, new Dictionary<ushort, uint> { [ChecklistMissionId] = 0 });
        return manager;
    }
}
