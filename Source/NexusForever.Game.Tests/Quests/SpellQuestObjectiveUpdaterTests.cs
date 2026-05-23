using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Quests;

public class SpellQuestObjectiveUpdaterTests
{
    [Fact]
    public void UpdateSpellSuccessObjectives_CreditsAllSpellSuccessFamilies()
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);

        SpellQuestObjectiveUpdater.UpdateSpellSuccessObjectives(player, 87298u);

        AssertObjectiveUpdate(questProxy, QuestObjectiveType.SpellSuccess, 87298u, 1u);
        AssertObjectiveUpdate(questProxy, QuestObjectiveType.SpellSuccess2, 87298u, 1u);
        AssertObjectiveUpdate(questProxy, QuestObjectiveType.SpellSuccess3, 87298u, 1u);
        AssertObjectiveUpdate(questProxy, QuestObjectiveType.SpellSuccess4, 87298u, 1u);
    }

    [Fact]
    public void UpdateSpellSuccessObjectives_NullPlayerOrSpell_DoesNothing()
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);

        SpellQuestObjectiveUpdater.UpdateSpellSuccessObjectives(null, 1u);
        SpellQuestObjectiveUpdater.UpdateSpellSuccessObjectives(player, 0u);

        Assert.Empty(GetTypedObjectiveUpdates(questProxy));
    }

    private static void AssertObjectiveUpdate(
        RecordingDispatchProxy<IQuestManager> questProxy,
        QuestObjectiveType type,
        uint data,
        uint progress)
    {
        Assert.Contains(GetTypedObjectiveUpdates(questProxy), i =>
            (QuestObjectiveType)i.Arguments[0] == type
            && (uint)i.Arguments[1] == data
            && (uint)i.Arguments[2] == progress);
    }

    private static IReadOnlyList<RecordingDispatchProxy<IQuestManager>.Invocation> GetTypedObjectiveUpdates(
        RecordingDispatchProxy<IQuestManager> questProxy)
    {
        return questProxy
            .GetInvocations(nameof(IQuestManager.ObjectiveUpdate))
            .Where(i => i.Arguments.Length == 3 && i.Arguments[0] is QuestObjectiveType)
            .ToList();
    }
}
