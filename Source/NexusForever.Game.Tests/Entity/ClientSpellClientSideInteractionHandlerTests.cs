using System.Numerics;
using System.Reflection;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Entity;

namespace NexusForever.Game.Tests.Entity;

public class ClientSpellClientSideInteractionHandlerTests
{
    [Fact]
    public void HandleMessage_CompleteActionWithPendingActivation_CompletesActivation()
    {
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IQuestManager> questProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            worldId: 426u);
        IWorldEntity entity = CreateVisibleEntity(playerProxy, guid: 77u, creatureId: 27196u, out RecordingDispatchProxy<IWorldEntity> entityProxy);
        playerProxy.SetMethodReturn(nameof(IPlayer.TryCompleteClientSideInteractionSpell), true);
        PendingClientSideInteractionActivationStore.Set(session, entity, clientSideInteractionId: 266u, spell4BaseId: 18613u, invokeActivateCast: true);
        var handler = new ClientSpellClientSideInteractionHandler(assetManager: null);

        handler.HandleMessage(session, CreateClientSideInteraction(spellCastId: 1u, action: 1, spell4BaseIdPlusOne: 18614u));

        Assert.Contains(playerProxy.GetInvocations(nameof(IPlayer.TryCompleteClientSideInteractionSpell)), i =>
            i.Arguments.Length == 2
            && (uint)i.Arguments[0] == 1u
            && i.Arguments[1] is bool wasCancelled
            && !wasCancelled);
        Assert.Single(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateCast)));
        Assert.Single(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
        Assert.Empty(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateFail)));
        AssertObjectiveUpdate(questProxy, QuestObjectiveType.ActivateEntity, 27196u, 1u);
        AssertObjectiveUpdate(questProxy, QuestObjectiveType.SucceedCSI, 27196u, 1u);
    }

    [Fact]
    public void HandleMessage_StartActionWithPendingActivation_KeepsPendingActivation()
    {
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IQuestManager> questProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            worldId: 426u);
        IWorldEntity entity = CreateVisibleEntity(playerProxy, guid: 77u, creatureId: 27196u, out RecordingDispatchProxy<IWorldEntity> entityProxy);
        PendingClientSideInteractionActivationStore.Set(session, entity, clientSideInteractionId: 266u, spell4BaseId: 18613u, invokeActivateCast: true);
        var handler = new ClientSpellClientSideInteractionHandler(assetManager: null);

        handler.HandleMessage(session, CreateClientSideInteraction(spellCastId: 1u, action: 3, spell4BaseIdPlusOne: 18614u));

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TryCompleteClientSideInteractionSpell)));
        Assert.Empty(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateCast)));
        Assert.Empty(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
        Assert.Empty(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateFail)));
        Assert.Empty(questProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.True(PendingClientSideInteractionActivationStore.TryConsume(session, 18613u, out _));
    }

    [Fact]
    public void HandleMessage_CancelActionWithPendingActivation_FailsActivationWithoutCredit()
    {
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IQuestManager> questProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            worldId: 426u);
        IWorldEntity entity = CreateVisibleEntity(playerProxy, guid: 77u, creatureId: 27196u, out RecordingDispatchProxy<IWorldEntity> entityProxy);
        playerProxy.SetMethodReturn(nameof(IPlayer.TryCompleteClientSideInteractionSpell), true);
        PendingClientSideInteractionActivationStore.Set(session, entity, clientSideInteractionId: 266u, spell4BaseId: 18613u, invokeActivateCast: true);
        var handler = new ClientSpellClientSideInteractionHandler(assetManager: null);

        handler.HandleMessage(session, CreateClientSideInteraction(spellCastId: 1u, action: 0, spell4BaseIdPlusOne: 18614u));

        Assert.Contains(playerProxy.GetInvocations(nameof(IPlayer.TryCompleteClientSideInteractionSpell)), i =>
            i.Arguments.Length == 2
            && (uint)i.Arguments[0] == 1u
            && i.Arguments[1] is bool wasCancelled
            && wasCancelled);
        Assert.Empty(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateCast)));
        Assert.Empty(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
        Assert.Single(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateFail)));
        Assert.Empty(questProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.False(PendingClientSideInteractionActivationStore.TryConsume(session, 18613u, out _));
    }

    [Fact]
    public void HandleMessage_CompleteActionWithoutPendingActivation_DoesNotCreditObjectives()
    {
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IQuestManager> questProxy,
            out _,
            worldId: 426u);
        var handler = new ClientSpellClientSideInteractionHandler(assetManager: null);

        handler.HandleMessage(session, CreateClientSideInteraction(spellCastId: 1u, action: 1, spell4BaseIdPlusOne: 18614u));

        Assert.Empty(questProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IQuestManager> questProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        uint worldId)
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out questProxy);
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = worldId });
        playerProxy.SetProperty(nameof(IPlayer.Guid), 17u);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetProperty(nameof(IGridEntity.Position), Vector3.Zero);

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static IWorldEntity CreateVisibleEntity(
        RecordingDispatchProxy<IPlayer> playerProxy,
        uint guid,
        uint creatureId,
        out RecordingDispatchProxy<IWorldEntity> entityProxy)
    {
        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out entityProxy);
        entityProxy.SetProperty(nameof(IGridEntity.Guid), guid);
        entityProxy.SetProperty(nameof(IGridEntity.Position), Vector3.Zero);
        entityProxy.SetProperty(nameof(IWorldEntity.CreatureId), creatureId);
        entityProxy.SetProperty(nameof(IWorldEntity.IsBusy), false);
        playerProxy.SetMethodReturn("GetVisible", entity);
        return entity;
    }

    private static ClientSpellClientSideInteraction CreateClientSideInteraction(
        uint spellCastId,
        byte action,
        uint spell4BaseIdPlusOne)
    {
        var message = new ClientSpellClientSideInteraction();
        SetBackingField(message, nameof(ClientSpellClientSideInteraction.SpellCastId), spellCastId);
        SetBackingField(message, nameof(ClientSpellClientSideInteraction.Action), action);
        SetBackingField(message, nameof(ClientSpellClientSideInteraction.Spell4BaseIdPlusOne), spell4BaseIdPlusOne);
        return message;
    }

    private static void SetBackingField<T>(T target, string propertyName, object value)
    {
        FieldInfo field = typeof(T)
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(target, value);
    }

    private static void AssertObjectiveUpdate(
        RecordingDispatchProxy<IQuestManager> questProxy,
        QuestObjectiveType objectiveType,
        uint objectId,
        uint count)
    {
        Assert.Contains(questProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)), invocation =>
            invocation.Arguments.Length >= 3
            && invocation.Arguments[0] is QuestObjectiveType type
            && type == objectiveType
            && invocation.Arguments[1] is uint objectiveObjectId
            && objectiveObjectId == objectId
            && invocation.Arguments[2] is uint objectiveCount
            && objectiveCount == count);
    }
}
