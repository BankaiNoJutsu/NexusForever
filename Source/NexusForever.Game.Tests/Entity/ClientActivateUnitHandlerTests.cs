using System.Numerics;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
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

    private static IWorldSession CreateSession(
        uint creatureId,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IWorldEntity> entityProxy)
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
        playerProxy.SetMethodReturn(nameof(IPlayer.TryCastSpell), CastResult.Ok);

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

    private static ClientActivateUnit CreateClientActivateUnit(uint activateUnitId)
    {
        var message = new ClientActivateUnit();
        typeof(ClientActivateUnit)
            .GetProperty(nameof(ClientActivateUnit.ActivateUnitId))
            ?.SetValue(message, activateUnitId);
        return message;
    }
}
