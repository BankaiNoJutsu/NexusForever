using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Static.Support;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.Network.World.Message.Model.Support;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Support;

namespace NexusForever.Game.Tests.Support;

public class SupportStuckHandlerTests
{
    [Fact]
    public void FreeSuicide_WhenPlayerAliveDealsCurrentHealthDamage()
    {
        var handler = CreateHandler();
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IUnitEntity.IsAlive), true);
        playerProxy.SetProperty(nameof(IWorldEntity.Health), 123u);

        handler.HandleMessage(session, CreateStuck(UnstickType.FreeSuicide, 9001u));

        RecordingDispatchProxy<IPlayer>.Invocation invocation =
            Assert.Single(playerProxy.GetInvocations(nameof(IUnitEntity.ModifyHealth)));
        Assert.Equal(123u, invocation.Arguments[0]);
        Assert.Equal(DamageType.Physical, invocation.Arguments[1]);
        Assert.Same(session.Player, invocation.Arguments[2]);
    }

    [Fact]
    public void FreeSuicide_WhenPlayerDeadDoesNotModifyHealth()
    {
        var handler = CreateHandler();
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IUnitEntity.IsAlive), false);

        handler.HandleMessage(session, CreateStuck(UnstickType.FreeSuicide, 9002u));

        Assert.Empty(playerProxy.GetInvocations(nameof(IUnitEntity.ModifyHealth)));
    }

    [Fact]
    public void FreeSuicide_WithZeroHealthStillDealsAtLeastOneDamage()
    {
        var handler = CreateHandler();
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IUnitEntity.IsAlive), true);
        playerProxy.SetProperty(nameof(IWorldEntity.Health), 0u);

        handler.HandleMessage(session, CreateStuck(UnstickType.FreeSuicide, 9003u));

        RecordingDispatchProxy<IPlayer>.Invocation invocation =
            Assert.Single(playerProxy.GetInvocations(nameof(IUnitEntity.ModifyHealth)));
        Assert.Equal(1u, invocation.Arguments[0]);
    }

    [Fact]
    public void RecallTransmat_WhenTeleportBlockedDoesNotTeleport()
    {
        var handler = CreateHandler();
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), false);

        handler.HandleMessage(session, CreateStuck(UnstickType.RecallTransmat, 9004u));

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Fact]
    public void InvalidUnstickTypeDoesNotMutatePlayer()
    {
        var handler = CreateHandler();
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IUnitEntity.IsAlive), true);
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), true);

        handler.HandleMessage(session, CreateStuck((UnstickType)7u, 9005u));

        Assert.Empty(playerProxy.GetInvocations(nameof(IUnitEntity.ModifyHealth)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    private static ClientStuckHandler CreateHandler()
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out _);
        IGlobalResidenceManager globalResidenceManager = RecordingDispatchProxy<IGlobalResidenceManager>.Create(out _);
        IMapLockManager mapLockManager = RecordingDispatchProxy<IMapLockManager>.Create(out _);

        return new ClientStuckHandler(
            NullLogger<ClientStuckHandler>.Instance,
            gameTableManager,
            globalResidenceManager,
            mapLockManager);
    }

    private static IWorldSession CreateSession(out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static ClientStuck CreateStuck(UnstickType type, uint contextToken)
    {
        var stuck = (ClientStuck)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ClientStuck));
        typeof(ClientStuck).GetProperty(nameof(ClientStuck.UnstickingType))!.SetValue(stuck, type);
        typeof(ClientStuck).GetProperty(nameof(ClientStuck.ContextToken))!.SetValue(stuck, contextToken);
        return stuck;
    }
}
