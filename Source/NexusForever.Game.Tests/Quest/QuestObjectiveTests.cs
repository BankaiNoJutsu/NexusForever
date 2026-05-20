using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Quest;

public class QuestObjectiveTests
{
    [Fact]
    public void ObjectiveUpdate_KillNamedCreatureScalesDynamicProgress()
    {
        var objective = CreateObjective(
            QuestObjectiveType.KillNamedCreature,
            count: 5u);

        objective.ObjectiveUpdate(1u);

        Assert.Equal(200u, objective.Progress);
    }

    [Fact]
    public void ObjectiveUpdate_KillNamedCreatureSkipsDynamicProgressWhenFlagDisabled()
    {
        var objective = CreateObjective(
            QuestObjectiveType.KillNamedCreature,
            count: 5u,
            flags: (uint)QuestObjectiveFlags.DisablesDynamicProgress);

        objective.ObjectiveUpdate(1u);

        Assert.Equal(1u, objective.Progress);
    }

    private static QuestObjective CreateObjective(QuestObjectiveType type, uint count, uint flags = 0u)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);

        IQuestInfo questInfo = RecordingDispatchProxy<IQuestInfo>.Create(out var questInfoProxy);
        questInfoProxy.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry
        {
            Id = 9001u
        });

        var objectiveInfo = new QuestObjectiveInfo(new QuestObjectiveEntry
        {
            Type  = (uint)type,
            Count = count,
            Flags = flags
        });

        return new QuestObjective(player, questInfo, objectiveInfo, index: 0);
    }
}