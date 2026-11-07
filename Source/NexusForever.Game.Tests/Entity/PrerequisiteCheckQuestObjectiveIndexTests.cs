using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Entity;

// Native evidence (Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md): prerequisite types 22, 23, 33, 34, 35 and 36 are one family that calls the same evaluator
// virtual (+0x408, FUN_14049f4d0) with (unit, comparison, objectId, index 0..5, value). The body looks the quest definition up by ObjectId; when it is found and the player's
// quest state is Completed (3) the progress is the objective's required count (FUN_140551840: 0 for a missing objective, 100 for a kill-type objective with count >= 2 and
// without flag 0x200), otherwise the player's active quest objective progress (0 when the quest is not active). The result is always passed to the shared compare helper
// FUN_1404a2090(comparison, progress, Value), so a quest that is not started compares progress 0 rather than failing outright.
public class PrerequisiteCheckQuestObjectiveIndexTests
{
    private const ushort ActiveQuest = 700;
    private const ushort CompletedQuest = 701;
    private const ushort KillQuest = 706;

    [Theory]
    [InlineData(PrerequisiteType.QuestObjective0, 0)]
    [InlineData(PrerequisiteType.QuestObjective1, 1)]
    [InlineData(PrerequisiteType.QuestObjective2, 2)]
    [InlineData(PrerequisiteType.QuestObjective3, 3)]
    [InlineData(PrerequisiteType.QuestObjective4, 4)]
    [InlineData(PrerequisiteType.QuestObjective5, 5)]
    public void EachTypeIsRegisteredForItsObjectiveIndex(PrerequisiteType type, int index)
    {
        Type check = typeof(PrerequisiteCheckQuestObjective0).Assembly.GetTypes()
            .Single(candidate => candidate.GetCustomAttributes(typeof(PrerequisiteCheckAttribute), false)
                .Cast<PrerequisiteCheckAttribute>().Any(attribute => attribute.Type == type));
        Assert.True(typeof(IPrerequisiteCheck).IsAssignableFrom(check));

        // Progress is distinct per index, so a check wired to the wrong index would compare the wrong objective.
        IPlayer player = Player(activeProgress: [10, 11, 12, 13, 14, 15]);
        var instance = (IPrerequisiteCheck)Activator.CreateInstance(check, Tables());
        Assert.True(instance.Meets(player, PrerequisiteComparison.Equal, 10u + (uint)index, ActiveQuest, new PrerequisiteParameters()));
        Assert.False(instance.Meets(player, PrerequisiteComparison.Equal, 10u + (uint)((index + 1) % 6), ActiveQuest, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 3u, true)]
    [InlineData(PrerequisiteComparison.Equal, 2u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 2u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 2u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 3u, false)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 3u, true)]
    [InlineData(PrerequisiteComparison.LessThan, 4u, true)]
    [InlineData(PrerequisiteComparison.LessThan, 3u, false)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 3u, true)]
    public void ComparisonsApplyToTheActiveObjectiveProgress(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = Player(activeProgress: [3]);
        Assert.Equal(expected, Check(0).Meets(player, comparison, value, ActiveQuest, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 0u, true)]
    [InlineData(PrerequisiteComparison.Equal, 1u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 0u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 1u, true)]
    [InlineData(PrerequisiteComparison.LessThan, 1u, true)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 0u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 0u, false)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 1u, false)]
    public void AQuestThatIsNotActiveOrCompletedComparesZeroProgress(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = Player(activeProgress: [3]);

        // 702 is a known quest the player never accepted, 799 is not a quest definition at all, 100000 is not even a quest id the client could hold.
        foreach (uint questId in new uint[] { 702, 799, 100_000 })
            Assert.Equal(expected, Check(0).Meets(player, comparison, value, questId, new PrerequisiteParameters()));
    }

    [Fact]
    public void AnObjectiveIndexTheActiveQuestDoesNotHaveComparesZeroProgress()
    {
        IPlayer player = Player(activeProgress: [3, 4]);

        Assert.True(Check(3).Meets(player, PrerequisiteComparison.Equal, 0u, ActiveQuest, new PrerequisiteParameters()));
        Assert.False(Check(3).Meets(player, PrerequisiteComparison.GreaterThan, 0u, ActiveQuest, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(0, 7u)]     // plain objective: the required count
    [InlineData(1, 100u)]   // kill-type objective, count 10, flag 0x200 clear: native reports 100
    [InlineData(2, 10u)]    // the same kill type with flag 0x200 set keeps its count
    [InlineData(3, 1u)]     // kill-type with count 1 keeps 1
    [InlineData(4, 0u)]     // the objective id is zero, so there is no objective at the index
    [InlineData(5, 0u)]     // the objective id is not in the objective table
    public void ACompletedQuestComparesTheObjectivesRequiredCount(int index, uint expectedProgress)
    {
        IPlayer player = Player(activeProgress: [3]);
        var parameters = new PrerequisiteParameters();

        Assert.True(Check((byte)index).Meets(player, PrerequisiteComparison.Equal, expectedProgress, CompletedQuest, parameters));
        Assert.False(Check((byte)index).Meets(player, PrerequisiteComparison.Equal, expectedProgress + 1u, CompletedQuest, parameters));
        Assert.Equal(expectedProgress > 0u, Check((byte)index).Meets(player, PrerequisiteComparison.GreaterThanOrEqual, 1u, CompletedQuest, parameters));
    }

    // Native FUN_1406113b0: for a kill-type objective (types 2, 8, 15, 16, 46) with a required count of at least 2 and flag 0x200 clear, the progress handed to the compare helper is a percentage:
    // a Completed quest reports 100 (FUN_140551840) and an active one reports `100 * raw * 0.001` of the objective's own progress value, so 3 of 10 kills is 30. Other objectives report their count.
    // The conversion of the server's kill count to the client's per-mille value is assumed to be kills * 1000 / count, which this reading makes equal to kills * 100 / count percent.
    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 30u, true)]
    [InlineData(PrerequisiteComparison.Equal, 3u, false)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 30u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 30u, false)]
    [InlineData(PrerequisiteComparison.LessThan, 31u, true)]
    public void AnActiveKillObjectiveWithoutTheCountFlagComparesAPercentage(PrerequisiteComparison comparison, uint value, bool expected)
    {
        // Quest 706: index 0 a plain TalkTo, index 1 a kill objective of 10 (no flag), index 2 a kill objective of 10 with flag 0x200, index 3 a single-kill objective.
        IPlayer player = Player(activeProgress: [5, 3, 3, 1], KillQuest);

        Assert.Equal(expected, Check(1).Meets(player, comparison, value, KillQuest, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(0, 5u)]     // a plain objective: the raw count
    [InlineData(2, 3u)]     // flag 0x200 set: the raw count
    [InlineData(3, 1u)]     // required count 1: not a percentage
    public void ObjectivesThatAreNotPercentageKillsKeepTheirCount(int index, uint expected)
    {
        IPlayer player = Player(activeProgress: [5, 3, 3, 1], KillQuest);

        Assert.True(Check((byte)index).Meets(player, PrerequisiteComparison.Equal, expected, KillQuest, new PrerequisiteParameters()));
    }

    [Fact]
    public void APercentageIsCappedAtOneHundred()
    {
        IPlayer player = Player(activeProgress: [5, 14, 3, 1], KillQuest);

        Assert.True(Check(1).Meets(player, PrerequisiteComparison.Equal, 100u, KillQuest, new PrerequisiteParameters()));
    }

    private static IPrerequisiteCheck Check(byte index) => index switch
    {
        0 => new PrerequisiteCheckQuestObjective0(Tables()),
        1 => new PrerequisiteCheckQuestObjective1(Tables()),
        2 => new PrerequisiteCheckQuestObjective2(Tables()),
        3 => new PrerequisiteCheckQuestObjective3(Tables()),
        4 => new PrerequisiteCheckQuestObjective4(Tables()),
        _ => new PrerequisiteCheckQuestObjective5(Tables())
    };

    private static IGameTableManager Tables()
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);

        proxy.SetProperty(nameof(IGameTableManager.Quest2), PlayerBindPointTests.Table(
            new Quest2Entry { Id = ActiveQuest, Objectives = [9001, 9002, 9003, 9004, 0, 0] },
            new Quest2Entry { Id = 702, Objectives = [9001, 0, 0, 0, 0, 0] },
            new Quest2Entry { Id = CompletedQuest, Objectives = [9010, 9011, 9012, 9013, 0, 9999] },
            new Quest2Entry { Id = KillQuest, Objectives = [9001, 9011, 9012, 9013, 0, 0] }));

        proxy.SetProperty(nameof(IGameTableManager.QuestObjective), PlayerBindPointTests.Table(
            Objective(9001, QuestObjectiveType.TalkTo, 0u, 5),
            Objective(9002, QuestObjectiveType.TalkTo, 0u, 5),
            Objective(9003, QuestObjectiveType.TalkTo, 0u, 5),
            Objective(9004, QuestObjectiveType.TalkTo, 0u, 5),
            Objective(9010, QuestObjectiveType.CollectItem, 0u, 7),
            Objective(9011, QuestObjectiveType.KillCreature, 0u, 10),
            Objective(9012, QuestObjectiveType.KillCreature, 0x200u, 10),
            Objective(9013, QuestObjectiveType.KillCreature, 0u, 1)));

        return tables;
    }

    private static QuestObjectiveEntry Objective(uint id, QuestObjectiveType type, uint flags, uint count)
        => new() { Id = id, Type = (uint)type, Flags = flags, Count = count };

    private static IPlayer Player(uint[] activeProgress, ushort activeQuestId = ActiveQuest)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IQuestManager manager = RecordingDispatchProxy<IQuestManager>.Create(out var managerProxy);
        IQuest quest = RecordingDispatchProxy<IQuest>.Create(out var questProxy);
        var objectives = activeProgress.Select((value, index) =>
        {
            IQuestObjective objective = RecordingDispatchProxy<IQuestObjective>.Create(out var objectiveProxy);
            objectiveProxy.SetProperty(nameof(IQuestObjective.Index), (byte)index);
            objectiveProxy.SetProperty(nameof(IQuestObjective.Progress), value);
            return objective;
        }).ToList();
        questProxy.SetProperty(nameof(IQuest.Id), activeQuestId);
        questProxy.SetMethodHandler("GetEnumerator", _ => objectives.GetEnumerator());
        managerProxy.SetMethodReturn(nameof(IQuestManager.GetActiveQuests), new List<IQuest> { quest });
        managerProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), args => (ushort)args[0] == activeQuestId
            ? QuestState.Accepted
            : (ushort)args[0] == CompletedQuest ? QuestState.Completed : null);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), manager);
        return player;
    }
}
