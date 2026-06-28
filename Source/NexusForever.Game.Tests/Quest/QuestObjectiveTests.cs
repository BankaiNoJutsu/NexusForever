using System.Collections.Immutable;
using NexusForever.Game.Abstract;
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

    [Fact]
    public void ObjectiveUpdate_SucceedCsiWithDynamicProgressFlagUsesRawCounts()
    {
        var objective = CreateObjective(
            QuestObjectiveType.SucceedCSI,
            count: 3u,
            flags: (uint)QuestObjectiveFlags.UsesDynamicProgress);

        objective.ObjectiveUpdate(1u);
        Assert.Equal(1u, objective.Progress);
        Assert.False(objective.IsComplete());

        objective.ObjectiveUpdate(1u);
        Assert.Equal(2u, objective.Progress);
        Assert.False(objective.IsComplete());

        objective.ObjectiveUpdate(1u);
        Assert.Equal(3u, objective.Progress);
        Assert.True(objective.IsComplete());
    }

    [Fact]
    public void ObjectiveUpdate_ChecklistWithDynamicProgressFlagKeepsChecklistBits()
    {
        var objective = CreateObjective(
            QuestObjectiveType.ActivateTargetGroupChecklist,
            count: 2u,
            flags: (uint)QuestObjectiveFlags.UsesDynamicProgress);

        objective.ObjectiveUpdate(0u);
        Assert.Equal(0b01u, objective.Progress);
        Assert.False(objective.IsComplete());

        objective.ObjectiveUpdate(1u);
        Assert.Equal(0b11u, objective.Progress);
        Assert.True(objective.IsComplete());
    }

    [Fact]
    public void IsTarget_ActivateEntityWithRewardPaneTargetGroupMatchesExpandedTargets()
    {
        IPlayer player = CreatePlayer();
        IQuestInfo questInfo = CreateQuestInfo();
        IAssetManager assetManager = RecordingDispatchProxy<IAssetManager>.Create(out var assetManagerProxy);
        assetManagerProxy.SetMethodHandler(nameof(IAssetManager.GetQuestObjectiveTargetIds), args =>
        {
            Assert.Equal(6508u, (uint)args[0]);
            return ImmutableList.Create(17189u, 19595u);
        });

        var objectiveInfo = new QuestObjectiveInfo(new QuestObjectiveEntry
        {
            Id                      = 6508u,
            Type                    = (uint)QuestObjectiveType.ActivateEntity,
            Data                    = 0u,
            Count                   = 5u,
            TargetGroupIdRewardPane = 4323u
        });

        var objective = new QuestObjective(player, questInfo, objectiveInfo, index: 0, assetManager);

        Assert.True(objective.IsTarget(17189u));
        Assert.True(objective.IsTarget(19595u));
        Assert.False(objective.IsTarget(4323u));
    }

    [Fact]
    public void Q5596DeadDominionDemolitionsExperts_CompleteChecklistObjective()
    {
        IPlayer player = CreatePlayer();
        IQuestInfo questInfo = CreateQuestInfo(5596u);
        IAssetManager assetManager = RecordingDispatchProxy<IAssetManager>.Create(out var assetManagerProxy);
        assetManagerProxy.SetMethodHandler(nameof(IAssetManager.GetQuestObjectiveTargetIds), args =>
        {
            Assert.Equal(8468u, (uint)args[0]);
            return ImmutableList.Create(24703u);
        });

        var objectiveInfo = new QuestObjectiveInfo(new QuestObjectiveEntry
        {
            Id                      = 8468u,
            Type                    = (uint)QuestObjectiveType.ActivateTargetGroupChecklist,
            Count                   = 3u,
            TargetGroupIdRewardPane = 2619u
        });

        var objective = new QuestObjective(player, questInfo, objectiveInfo, index: 1, assetManager);

        Assert.True(objective.IsTarget(24703u));

        objective.ObjectiveUpdate(1u);
        objective.ObjectiveUpdate(2u);
        Assert.False(objective.IsComplete());

        objective.ObjectiveUpdate(3u);

        Assert.True(objective.IsComplete());
        Assert.Equal(0b1110u, objective.Progress);
    }

    private static QuestObjective CreateObjective(QuestObjectiveType type, uint count, uint flags = 0u)
    {
        IPlayer player = CreatePlayer();
        IQuestInfo questInfo = CreateQuestInfo();

        var objectiveInfo = new QuestObjectiveInfo(new QuestObjectiveEntry
        {
            Type  = (uint)type,
            Count = count,
            Flags = flags
        });

        return new QuestObjective(player, questInfo, objectiveInfo, index: 0);
    }

    private static IPlayer CreatePlayer()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        return player;
    }

    private static IQuestInfo CreateQuestInfo(uint questId = 9001u)
    {
        IQuestInfo questInfo = RecordingDispatchProxy<IQuestInfo>.Create(out var questInfoProxy);
        questInfoProxy.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry
        {
            Id = questId
        });
        return questInfo;
    }
}
