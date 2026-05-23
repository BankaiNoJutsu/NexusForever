using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.WorldServer.Network.Message.Handler.Entity;

namespace NexusForever.Game.Tests.Entity;

public class InteractionObjectiveUpdaterTests
{
    [Fact]
    public void UpdateDirectInteractionObjectives_CreditsSucceedCSIWithCreatureId()
    {
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IQuestManager> questProxy);
        IWorldEntity entity = CreateEntity(creatureId: 73498u);

        InteractionObjectiveUpdater.UpdateDirectInteractionObjectives(player, entity, assetManager: null);

        AssertObjectiveUpdate(questProxy, QuestObjectiveType.SucceedCSI, 73498u, 1u);
        AssertObjectiveUpdate(questProxy, QuestObjectiveType.ActivateEntity, 73498u, 1u);
        AssertObjectiveUpdate(questProxy, QuestObjectiveType.TalkTo, 73498u, 1u);
        AssertObjectiveUpdate(questProxy, QuestObjectiveType.ActivateTargetGroup, 73498u, 1u);
        AssertObjectiveUpdate(questProxy, QuestObjectiveType.GatheResource, 73498u, 1u);
    }

    [Fact]
    public void UpdateActivateSuccessObjectives_DirectActivate_CreditsActivateEntityAndSucceedCSI()
    {
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IQuestManager> questProxy);
        IWorldEntity entity = CreateEntity(creatureId: 74745u);

        InteractionObjectiveUpdater.UpdateActivateSuccessObjectives(player, entity, assetManager: null, includeActivateEntity: true);

        AssertObjectiveUpdate(questProxy, QuestObjectiveType.ActivateEntity, 74745u, 1u);
        AssertObjectiveUpdate(questProxy, QuestObjectiveType.SucceedCSI, 74745u, 1u);
    }

    [Fact]
    public void UpdateActivateSuccessObjectives_ActivateCast_CreditsSucceedCSIWithoutActivateEntity()
    {
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IQuestManager> questProxy);
        IWorldEntity entity = CreateEntity(creatureId: 74746u);

        InteractionObjectiveUpdater.UpdateActivateSuccessObjectives(player, entity, assetManager: null, includeActivateEntity: false);

        AssertNoObjectiveUpdate(questProxy, QuestObjectiveType.ActivateEntity);
        AssertObjectiveUpdate(questProxy, QuestObjectiveType.SucceedCSI, 74746u, 1u);
    }

    [Fact]
    public void UpdateDirectInteractionObjectives_NullEntity_DoesNotCreditObjectives()
    {
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IQuestManager> questProxy);

        InteractionObjectiveUpdater.UpdateDirectInteractionObjectives(player, entity: null, assetManager: null);

        Assert.Empty(GetTypedObjectiveUpdates(questProxy));
    }

    private static IPlayer CreatePlayer(out RecordingDispatchProxy<IQuestManager> questProxy)
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out questProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        return player;
    }

    private static IWorldEntity CreateEntity(uint creatureId)
    {
        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> entityProxy);
        entityProxy.SetProperty(nameof(IWorldEntity.CreatureId), creatureId);
        return entity;
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

    private static void AssertNoObjectiveUpdate(RecordingDispatchProxy<IQuestManager> questProxy, QuestObjectiveType type)
    {
        Assert.DoesNotContain(GetTypedObjectiveUpdates(questProxy), i => (QuestObjectiveType)i.Arguments[0] == type);
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
