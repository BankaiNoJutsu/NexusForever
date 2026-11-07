using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Entity;

// Native evidence (Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md): prerequisite types 22, 23, 33, 34, 35 and 36 are one family that calls the same evaluator
// virtual (+0x408) with (unit, comparison, objectId, index 0..5, value). Retail rows use ObjectId as a quest id and Value at or below the required count of that
// quest's objective at the index. The checks compare the player's progress on the active quest's objective at that index with Value; anything not active or missing
// stays false, exactly as these unhandled types behaved before.
public class PrerequisiteCheckQuestObjectiveIndexTests
{
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
        IPlayer player = Player(questId: 700, progress: [10, 11, 12, 13, 14, 15]);
        var instance = (IPrerequisiteCheck)Activator.CreateInstance(check);
        Assert.True(instance.Meets(player, PrerequisiteComparison.Equal, 10u + (uint)index, 700u, new PrerequisiteParameters()));
        Assert.False(instance.Meets(player, PrerequisiteComparison.Equal, 10u + (uint)((index + 1) % 6), 700u, new PrerequisiteParameters()));
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
    public void ComparisonsApplyToTheObjectiveProgress(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = Player(questId: 700, progress: [3]);
        Assert.Equal(expected, new PrerequisiteCheckQuestObjective0().Meets(player, comparison, value, 700u, new PrerequisiteParameters()));
    }

    [Fact]
    public void AQuestThatIsNotActiveOrAnObjectiveThatDoesNotExistStaysFalse()
    {
        IPlayer player = Player(questId: 700, progress: [0, 0]);
        Assert.False(new PrerequisiteCheckQuestObjective0().Meets(player, PrerequisiteComparison.Equal, 0u, 701u, new PrerequisiteParameters())); // other quest
        Assert.False(new PrerequisiteCheckQuestObjective3().Meets(player, PrerequisiteComparison.Equal, 0u, 700u, new PrerequisiteParameters())); // no objective at index 3
        Assert.False(new PrerequisiteCheckQuestObjective0().Meets(player, PrerequisiteComparison.Equal, 0u, 100_000u, new PrerequisiteParameters())); // id beyond ushort
    }

    private static IPlayer Player(ushort questId, uint[] progress)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IQuestManager manager = RecordingDispatchProxy<IQuestManager>.Create(out var managerProxy);
        IQuest quest = RecordingDispatchProxy<IQuest>.Create(out var questProxy);
        var objectives = progress.Select((value, index) =>
        {
            IQuestObjective objective = RecordingDispatchProxy<IQuestObjective>.Create(out var objectiveProxy);
            objectiveProxy.SetProperty(nameof(IQuestObjective.Index), (byte)index);
            objectiveProxy.SetProperty(nameof(IQuestObjective.Progress), value);
            return objective;
        }).ToList();
        questProxy.SetProperty(nameof(IQuest.Id), questId);
        questProxy.SetMethodHandler("GetEnumerator", _ => objectives.GetEnumerator());
        managerProxy.SetMethodReturn(nameof(IQuestManager.GetActiveQuests), new List<IQuest> { quest });
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), manager);
        return player;
    }
}
