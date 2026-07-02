using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Combat;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.Pet;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Pet;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Pet;

namespace NexusForever.Game.Tests.Pet;

public class ClientPetCommandHandlerTests
{
    [Fact]
    public void ClientPetCommand_ReadsShortcutSetAndSlotIndex()
    {
        using var stream = new MemoryStream();
        var writer = new GamePacketWriter(stream);
        writer.Write((uint)ShortcutSet.PrimaryPetBar);
        writer.Write(EngineerCombatBotPetCommandHelper.PrimaryPetBarAttackSlotIndex);
        writer.FlushBits();

        using var reader = new GamePacketReader(new MemoryStream(stream.ToArray()));
        var message = new ClientPetCommand();
        message.Read(reader);

        Assert.Equal(ShortcutSet.PrimaryPetBar, message.ShortcutSet);
        Assert.Equal(EngineerCombatBotPetCommandHelper.PrimaryPetBarAttackSlotIndex, message.SlotIndex);
    }

    [Fact]
    public void HandleMessage_AttackCommand_OrdersOwnedEngineerBotsToAttackPlayerTarget()
    {
        IWorldSession session = CreateSession(1001u, out RecordingDispatchProxy<IPlayer> playerProxy);
        IUnitEntity target = CreateTarget(9000u, out _);
        IUnitEntity bot = CreateEngineerBot(
            333u,
            1001u,
            out RecordingDispatchProxy<IUnitEntity> botProxy,
            out RecordingDispatchProxy<IThreatManager> threatProxy,
            out _);
        AttachMap(session.Player, target);
        AttachSummonFactory(session.Player, 42683u, bot);
        playerProxy.SetProperty(nameof(IPlayer.TargetGuid), target.Guid);
        botProxy.SetMethodHandler(nameof(IUnitEntity.CanAttack), args => ReferenceEquals(args[0], target));
        var handler = new ClientPetCommandHandler(NullLogger<ClientPetCommandHandler>.Instance);

        handler.HandleMessage(session, CreateCommand(ShortcutSet.PrimaryPetBar, EngineerCombatBotPetCommandHelper.PrimaryPetBarAttackSlotIndex));

        Assert.False(bot.SummonCommandFollowRequested);
        RecordingDispatchProxy<IUnitEntity>.Invocation setTarget =
            Assert.Single(botProxy.GetInvocations(nameof(IUnitEntity.SetTarget)));
        Assert.Same(target, setTarget.Arguments[0]);
        Assert.Equal(1u, setTarget.Arguments[1]);
        RecordingDispatchProxy<IThreatManager>.Invocation threat =
            Assert.Single(threatProxy.GetInvocations(nameof(IThreatManager.UpdateThreat)));
        Assert.Same(target, threat.Arguments[0]);
        Assert.Equal(1, threat.Arguments[1]);
    }

    [Fact]
    public void HandleMessage_StopCommand_ClearsCombatAndRequestsFollowWithoutChangingStance()
    {
        IWorldSession session = CreateSession(1001u, out _);
        IUnitEntity bot = CreateEngineerBot(
            333u,
            1001u,
            out RecordingDispatchProxy<IUnitEntity> botProxy,
            out RecordingDispatchProxy<IThreatManager> threatProxy,
            out RecordingDispatchProxy<IMovementManager> movementProxy);
        AttachMap(session.Player, bot);
        AttachSummonFactory(session.Player, 42683u, bot);
        var handler = new ClientPetCommandHandler(NullLogger<ClientPetCommandHandler>.Instance);

        handler.HandleMessage(session, CreateCommand(ShortcutSet.PrimaryPetBar, EngineerCombatBotPetCommandHelper.PrimaryPetBarStopSlotIndex));

        Assert.True(bot.SummonCommandFollowRequested);
        Assert.Equal(PetStance.Assist, bot.SummonCommandStance);
        RecordingDispatchProxy<IUnitEntity>.Invocation setTarget =
            Assert.Single(botProxy.GetInvocations(nameof(IUnitEntity.SetTarget)));
        Assert.Null(setTarget.Arguments[0]);
        Assert.Single(threatProxy.GetInvocations(nameof(IThreatManager.ClearThreatList)));
        Assert.Single(movementProxy.GetInvocations(nameof(IMovementManager.Finalise)));
    }

    [Fact]
    public void HandleMessage_WithUnsupportedShortcutSet_DoesNotMutateEngineerBots()
    {
        IWorldSession session = CreateSession(1001u, out _);
        IUnitEntity bot = CreateEngineerBot(
            333u,
            1001u,
            out RecordingDispatchProxy<IUnitEntity> botProxy,
            out RecordingDispatchProxy<IThreatManager> threatProxy,
            out RecordingDispatchProxy<IMovementManager> movementProxy);
        AttachMap(session.Player, bot);
        AttachSummonFactory(session.Player, 42683u, bot);
        var handler = new ClientPetCommandHandler(NullLogger<ClientPetCommandHandler>.Instance);

        handler.HandleMessage(session, CreateCommand(ShortcutSet.VehicleBar, EngineerCombatBotPetCommandHelper.PrimaryPetBarAttackSlotIndex));

        Assert.Empty(botProxy.GetInvocations(nameof(IUnitEntity.SetTarget)));
        Assert.Empty(threatProxy.GetInvocations(nameof(IThreatManager.UpdateThreat)));
        Assert.Empty(threatProxy.GetInvocations(nameof(IThreatManager.ClearThreatList)));
        Assert.Empty(movementProxy.GetInvocations(nameof(IMovementManager.Finalise)));
    }

    private static IWorldSession CreateSession(uint playerGuid, out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);

        playerProxy.SetProperty(nameof(IPlayer.Guid), playerGuid);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static IUnitEntity CreateTarget(uint guid, out RecordingDispatchProxy<IUnitEntity> targetProxy)
    {
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out targetProxy);
        targetProxy.SetProperty(nameof(IUnitEntity.Guid), guid);
        targetProxy.SetProperty(nameof(IUnitEntity.IsAlive), true);
        return target;
    }

    private static IUnitEntity CreateEngineerBot(
        uint guid,
        uint summonerGuid,
        out RecordingDispatchProxy<IUnitEntity> botProxy,
        out RecordingDispatchProxy<IThreatManager> threatProxy,
        out RecordingDispatchProxy<IMovementManager> movementProxy)
    {
        IUnitEntity bot = RecordingDispatchProxy<IUnitEntity>.Create(out botProxy);
        IThreatManager threatManager = RecordingDispatchProxy<IThreatManager>.Create(out threatProxy);
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out movementProxy);
        botProxy.SetProperty(nameof(IUnitEntity.Guid), guid);
        botProxy.SetProperty(nameof(IUnitEntity.SummonerGuid), summonerGuid);
        botProxy.SetProperty(nameof(IUnitEntity.CreatureId), 42683u);
        botProxy.SetProperty(nameof(IUnitEntity.SummonCommandStance), PetStance.Assist);
        botProxy.SetProperty(nameof(IUnitEntity.SummonCommandFollowRequested), false);
        botProxy.SetProperty(nameof(IUnitEntity.ThreatManager), threatManager);
        botProxy.SetProperty(nameof(IUnitEntity.MovementManager), movementManager);
        return bot;
    }

    private static void AttachMap(IPlayer player, IUnitEntity entity)
    {
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), (method, args) =>
        {
            Type entityType = method.GetGenericArguments()[0];
            return entityType == typeof(IUnitEntity) && args.Length == 1 && (uint)args[0] == entity.Guid
                ? entity
                : null;
        });

        RecordingDispatchProxy<IPlayer> playerProxy = (RecordingDispatchProxy<IPlayer>)(object)player;
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetMethodHandler("GetVisible", (MethodInfo method, object[] args) =>
        {
            Type entityType = method.GetGenericArguments()[0];
            return entityType == typeof(IUnitEntity) && args.Length == 1 && (uint)args[0] == entity.Guid
                ? entity
                : null;
        });
    }

    private static void AttachSummonFactory(IPlayer player, uint activeCreatureId, params IWorldEntity[] activeSummons)
    {
        IEntitySummonFactory summonFactory = RecordingDispatchProxy<IEntitySummonFactory>.Create(
            out RecordingDispatchProxy<IEntitySummonFactory> summonFactoryProxy);
        summonFactoryProxy.SetMethodHandler(nameof(IEntitySummonFactory.GetSummonCreatureCount), args =>
            (uint)args[0] == activeCreatureId ? (uint)activeSummons.Length : 0u);
        summonFactoryProxy.SetMethodHandler(nameof(IEntitySummonFactory.GetSummonCreatures), args =>
            (uint)args[0] == activeCreatureId ? activeSummons : []);

        RecordingDispatchProxy<IPlayer> playerProxy = (RecordingDispatchProxy<IPlayer>)(object)player;
        playerProxy.SetProperty(nameof(IPlayer.SummonFactory), summonFactory);
    }

    private static ClientPetCommand CreateCommand(ShortcutSet shortcutSet, uint slotIndex)
    {
        using var stream = new MemoryStream();
        var writer = new GamePacketWriter(stream);
        writer.Write((uint)shortcutSet);
        writer.Write(slotIndex);
        writer.FlushBits();

        using var reader = new GamePacketReader(new MemoryStream(stream.ToArray()));
        var message = new ClientPetCommand();
        message.Read(reader);
        return message;
    }
}
