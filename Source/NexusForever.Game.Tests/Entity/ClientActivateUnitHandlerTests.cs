using System.Numerics;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Combat;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Abstract.Trade;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Entity;

namespace NexusForever.Game.Tests.Entity;

public class ClientActivateUnitHandlerTests
{
    [Fact]
    public void HandleMessage_WithAttackableUnit_TargetsAndStartsThreatWithoutActivating()
    {
        ITradeManager tradeManager = RecordingDispatchProxy<ITradeManager>.Create(out RecordingDispatchProxy<ITradeManager> tradeProxy);
        IAssetManager assetManager = RecordingDispatchProxy<IAssetManager>.Create(out _);
        var handler = new ClientActivateUnitHandler(tradeManager, assetManager);
        IWorldSession session = CreateAttackableUnitSession(
            out IUnitEntity target,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IUnitEntity> targetProxy,
            out RecordingDispatchProxy<IThreatManager> threatProxy);

        handler.HandleMessage(session, CreateClientActivateUnit(77u));

        Assert.Contains(playerProxy.GetInvocations(nameof(IPlayer.SetTarget)), i =>
            i.Arguments.Length == 2
            && ReferenceEquals(i.Arguments[0], target)
            && (uint)i.Arguments[1] == 1u);
        Assert.Contains(threatProxy.GetInvocations(nameof(IThreatManager.UpdateThreat)), i =>
            i.Arguments.Length == 2
            && ReferenceEquals(i.Arguments[0], session.Player)
            && (int)i.Arguments[1] == 1);
        Assert.Single(tradeProxy.GetInvocations(nameof(ITradeManager.Cancel)));
        Assert.Empty(targetProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
        Assert.Empty(targetProxy.GetInvocations(nameof(IWorldEntity.OnActivateFail)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
    }

    [Fact]
    public void HandleMessage_WithTutorialHoverboardProjector_CastsDirectMountAndCompletesActivation()
    {
        ITradeManager tradeManager = RecordingDispatchProxy<ITradeManager>.Create(out _);
        IAssetManager assetManager = RecordingDispatchProxy<IAssetManager>.Create(out _);
        var handler = new ClientActivateUnitHandler(tradeManager, assetManager);
        IWorldSession session = CreateSession(
            creatureId: 73419u,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IWorldEntity> entityProxy);

        handler.HandleMessage(session, CreateClientActivateUnit(77u));

        RecordingDispatchProxy<IPlayer>.Invocation mountCast = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
        Assert.Equal(85562u, (uint)mountCast.Arguments[0]);
        Assert.True(((ISpellParameters)mountCast.Arguments[1]).IgnoreGlobalCooldown);
        Assert.Single(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
        Assert.Empty(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateFail)));
    }

    [Fact]
    public void HandleMessage_WithTutorialHoverboardProjectorAndFailedMountCast_FailsActivation()
    {
        ITradeManager tradeManager = RecordingDispatchProxy<ITradeManager>.Create(out _);
        IAssetManager assetManager = RecordingDispatchProxy<IAssetManager>.Create(out _);
        var handler = new ClientActivateUnitHandler(tradeManager, assetManager);
        IWorldSession session = CreateSession(
            creatureId: 73419u,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IWorldEntity> entityProxy,
            mountCastResult: CastResult.TargetUnknown);

        handler.HandleMessage(session, CreateClientActivateUnit(77u));

        RecordingDispatchProxy<IPlayer>.Invocation mountCast = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
        Assert.Equal(85562u, (uint)mountCast.Arguments[0]);
        Assert.Single(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateFail)));
        Assert.Empty(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
    }

    private static IWorldSession CreateSession(
        uint creatureId,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IWorldEntity> entityProxy,
        CastResult mountCastResult = CastResult.Ok)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out entityProxy);
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _);

        questManagerProxy.SetMethodReturn(nameof(IQuestManager.GetActiveQuests), Array.Empty<IQuest>());

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        playerProxy.SetProperty(nameof(IPlayer.Guid), 17u);
        playerProxy.SetProperty(nameof(IPlayer.Position), Vector3.Zero);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetMethodReturn("GetVisible", entity);
        playerProxy.SetMethodReturn(nameof(IPlayer.TryCastSpell), mountCastResult);

        entityProxy.SetProperty(nameof(IGridEntity.Guid), 77u);
        entityProxy.SetProperty(nameof(IGridEntity.Position), Vector3.Zero);
        entityProxy.SetProperty(nameof(IWorldEntity.IsBusy), false);
        entityProxy.SetProperty(nameof(IWorldEntity.CreatureId), creatureId);
        entityProxy.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry
        {
            Id = creatureId,
            Spell4IdActivate00 = 86744u,
            ActivateSpellMaxRange = 0f
        });

        return session;
    }

    private static IWorldSession CreateAttackableUnitSession(
        out IUnitEntity target,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IUnitEntity> targetProxy,
        out RecordingDispatchProxy<IThreatManager> threatProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        target = RecordingDispatchProxy<IUnitEntity>.Create(out targetProxy);
        IThreatManager threatManager = RecordingDispatchProxy<IThreatManager>.Create(out threatProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        playerProxy.SetProperty(nameof(IPlayer.Guid), 17u);
        playerProxy.SetMethodReturn("GetVisible", target);
        playerProxy.SetMethodReturn(nameof(IPlayer.CanAttack), true);

        targetProxy.SetProperty(nameof(IGridEntity.Guid), 77u);
        targetProxy.SetProperty(nameof(IGridEntity.Position), Vector3.Zero);
        targetProxy.SetProperty(nameof(IWorldEntity.CreatureId), 70000u);
        targetProxy.SetProperty(nameof(IWorldEntity.IsBusy), false);
        targetProxy.SetProperty(nameof(IUnitEntity.ThreatManager), threatManager);

        return session;
    }

    private static ClientActivateUnit CreateClientActivateUnit(uint activateUnitId)
    {
        var message = new ClientActivateUnit();
        typeof(ClientActivateUnit)
            .GetProperty(nameof(ClientActivateUnit.ActivateUnitId))
            ?.SetValue(message, activateUnitId);
        return message;
    }
}
