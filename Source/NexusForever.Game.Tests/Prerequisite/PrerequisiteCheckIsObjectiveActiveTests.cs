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

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_OBJECTIVE_ACTIVE_20261107.md): type 43 (FUN_14049f3d0) takes ObjectId as a QUEST id (1,336 of 1,381 distinct retail ids are Quest2 ids) and Value as the OBJECTIVE
// INDEX (0 to 5). It is true when the quest is in the client's active quest tree, the objective at the index is not complete (FUN_1405510d0) and the objective is available (FUN_140553790); Equal returns that,
// NotEqual its negation, other comparisons are false. Availability: an index at or beyond the quest's objective count is not available; an objective of type 0 is available; with flag 0x2 (sequential)
// every earlier objective without flag 0x20 (optional) must be complete; with flag 0x1 only, walking back from the previous objective, one that is neither complete nor itself available makes it unavailable.
public class PrerequisiteCheckIsObjectiveActiveTests
{
    private const ushort ActiveQuest = 800;
    private const ushort CompletedQuest = 801;

    // Objectives of quest 800: 0 plain, 1 sequential (0x2), 2 requires previous (0x1), 3 sequential and optional (0x22), 4 sequential with type 0, 5 the last plain one.
    private static IGameTableManager Tables()
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.Quest2), PlayerBindPointTests.Table(
            new Quest2Entry { Id = ActiveQuest, Objectives = [9101, 9102, 9103, 9104, 9105, 9106] },
            new Quest2Entry { Id = CompletedQuest, Objectives = [9101, 0, 0, 0, 0, 0] }));
        proxy.SetProperty(nameof(IGameTableManager.QuestObjective), PlayerBindPointTests.Table(
            Objective(9101, QuestObjectiveType.TalkTo, 0u),
            Objective(9102, QuestObjectiveType.TalkTo, 0x2u),
            Objective(9103, QuestObjectiveType.TalkTo, 0x1u),
            Objective(9104, QuestObjectiveType.TalkTo, 0x22u),
            Objective(9105, 0u, 0x2u),
            Objective(9106, QuestObjectiveType.TalkTo, 0u)));
        return tables;
    }

    [Theory]
    [InlineData(0, new bool[] { false, false, false, false, false, false }, true)]     // plain objective, not complete
    [InlineData(0, new bool[] { true, false, false, false, false, false }, false)]     // already complete
    [InlineData(1, new bool[] { false, false, false, false, false, false }, false)]    // sequential: objective 0 incomplete
    [InlineData(1, new bool[] { true, false, false, false, false, false }, true)]
    [InlineData(1, new bool[] { true, true, false, false, false, false }, false)]      // complete itself
    [InlineData(2, new bool[] { false, false, false, false, false, false }, false)]    // requires previous: 1 is incomplete and not available (0 is incomplete)
    [InlineData(2, new bool[] { true, false, false, false, false, false }, true)]      // 1 is incomplete but available
    [InlineData(2, new bool[] { true, true, false, false, false, false }, true)]
    [InlineData(3, new bool[] { true, true, false, false, false, false }, false)]      // sequential: objective 2 (not optional) incomplete
    [InlineData(3, new bool[] { true, true, true, false, false, false }, true)]
    [InlineData(4, new bool[] { false, false, false, false, false, false }, true)]     // type 0 is always available
    [InlineData(5, new bool[] { false, false, false, false, false, false }, true)]     // plain
    public void AnObjectiveIsActiveWhenItIsIncompleteAndAvailable(int index, bool[] complete, bool expected)
    {
        IPlayer player = Player(complete);
        var check = new PrerequisiteCheckIsObjectiveActive(Tables());

        Assert.Equal(expected, check.Meets(player, PrerequisiteComparison.Equal, (uint)index, ActiveQuest, new PrerequisiteParameters()));
        Assert.Equal(!expected, check.Meets(player, PrerequisiteComparison.NotEqual, (uint)index, ActiveQuest, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.GreaterThan)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual)]
    [InlineData((PrerequisiteComparison)0)]
    public void OtherComparisonsNeverMeet(PrerequisiteComparison comparison)
    {
        Assert.False(new PrerequisiteCheckIsObjectiveActive(Tables()).Meets(Player(new bool[6]), comparison, 0u, ActiveQuest, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(6u)]                // an index at or beyond the objective count
    [InlineData(100u)]
    public void AnIndexBeyondTheQuestsObjectivesIsNotActive(uint index)
    {
        var check = new PrerequisiteCheckIsObjectiveActive(Tables());

        Assert.False(check.Meets(Player(new bool[6]), PrerequisiteComparison.Equal, index, ActiveQuest, new PrerequisiteParameters()));
        Assert.True(check.Meets(Player(new bool[6]), PrerequisiteComparison.NotEqual, index, ActiveQuest, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(CompletedQuest)]    // a Completed quest is not in the active tree
    [InlineData(999u)]              // not a quest the player has
    [InlineData(100_000u)]
    public void AQuestThatIsNotActiveHasNoActiveObjective(uint questId)
    {
        var check = new PrerequisiteCheckIsObjectiveActive(Tables());

        Assert.False(check.Meets(Player(new bool[6]), PrerequisiteComparison.Equal, 0u, questId, new PrerequisiteParameters()));
        Assert.True(check.Meets(Player(new bool[6]), PrerequisiteComparison.NotEqual, 0u, questId, new PrerequisiteParameters()));
    }

    private static QuestObjectiveEntry Objective(uint id, QuestObjectiveType type, uint flags) => Objective(id, (uint)type, flags);

    private static QuestObjectiveEntry Objective(uint id, uint type, uint flags) => new() { Id = id, Type = type, Flags = flags, Count = 1 };

    private static IPlayer Player(bool[] complete)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IQuestManager manager = RecordingDispatchProxy<IQuestManager>.Create(out var managerProxy);
        IQuest quest = RecordingDispatchProxy<IQuest>.Create(out var questProxy);
        var objectives = complete.Select((done, index) =>
        {
            IQuestObjective objective = RecordingDispatchProxy<IQuestObjective>.Create(out var objectiveProxy);
            objectiveProxy.SetProperty(nameof(IQuestObjective.Index), (byte)index);
            objectiveProxy.SetMethodReturn(nameof(IQuestObjective.IsComplete), done);
            return objective;
        }).ToList();
        questProxy.SetProperty(nameof(IQuest.Id), ActiveQuest);
        questProxy.SetMethodHandler("GetEnumerator", _ => objectives.GetEnumerator());
        managerProxy.SetMethodReturn(nameof(IQuestManager.GetActiveQuests), new List<IQuest> { quest });
        managerProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), args => (ushort)args[0] switch
        {
            ActiveQuest    => QuestState.Accepted,
            CompletedQuest => QuestState.Completed,
            _              => null
        });
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), manager);
        return player;
    }
}
