using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Setting;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Instance;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Instance;

namespace NexusForever.Game.Tests.Instances;

public class InstanceSettingsHandlerTests
{
    [Fact]
    public void ClosedInstanceSettings_IgnoresDialogClose()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out _);
        var handler = new ClientClosedInstanceSettingsHandler(NullLogger<ClientClosedInstanceSettingsHandler>.Instance);

        handler.HandleMessage(session, CreateClosedInstanceSettings(1234u));

        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void ResetSingleInstance_RejectsWhenNoResettableLockExists()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out _);
        var handler = new ClientResetSingleInstanceHandler(NullLogger<ClientResetSingleInstanceHandler>.Instance);

        handler.HandleMessage(session, CreateResetSingleInstance(1234u));

        RecordingDispatchProxy<IWorldSession>.Invocation invocation =
            Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        ServerInstanceResetResult result = Assert.IsType<ServerInstanceResetResult>(invocation.Arguments[0]);
        Assert.False(result.Success);
    }

    [Fact]
    public void SetInstanceSettings_StoresPlayerSettingsAndEchoesServerSettings()
    {
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out _,
            difficulty: WorldDifficulty.Normal,
            primeLevel: 0u,
            scalingEnabled: false);
        var handler = new ClientSetInstanceSettingsHandler(NullLogger<ClientSetInstanceSettingsHandler>.Instance);

        handler.HandleMessage(session, CreateSetInstanceSettings(
            1234u,
            WorldDifficulty.Veteran,
            primeLevel: 7,
            rally: 1));

        Assert.Equal(WorldDifficulty.Veteran, session.Player.InstanceDifficulty);
        Assert.Equal(7u, session.Player.InstancePrimeLevel);
        Assert.True(session.Player.InstanceScalingEnabled);

        RecordingDispatchProxy<IWorldSession>.Invocation invocation =
            Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        ServerInstanceSettings settings = Assert.IsType<ServerInstanceSettings>(invocation.Arguments[0]);
        Assert.Equal(WorldDifficulty.Veteran, settings.Difficulty);
        Assert.Equal(7u, settings.PrimeLevel);
        Assert.Equal(ServerInstanceSettings.WorldSetting.WorldForcesLevelScaling, settings.Flags);
        Assert.Equal(125u, settings.ClientEntitySendUpdateInterval);
    }

    [Fact]
    public void SetInstanceSettings_WithInvalidDifficulty_RejectsWithoutMutatingState()
    {
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out _,
            difficulty: WorldDifficulty.Normal,
            primeLevel: 3u,
            scalingEnabled: true);
        var handler = new ClientSetInstanceSettingsHandler(NullLogger<ClientSetInstanceSettingsHandler>.Instance);

        Assert.Throws<InvalidPacketValueException>(() => handler.HandleMessage(session, CreateSetInstanceSettings(
            1234u,
            WorldDifficulty.Count,
            primeLevel: 7,
            rally: 0)));

        Assert.Equal(WorldDifficulty.Normal, session.Player.InstanceDifficulty);
        Assert.Equal(3u, session.Player.InstancePrimeLevel);
        Assert.True(session.Player.InstanceScalingEnabled);
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        WorldDifficulty difficulty = WorldDifficulty.Normal,
        uint primeLevel = 0u,
        bool scalingEnabled = false)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 77u);
        playerProxy.SetProperty(nameof(IPlayer.InstanceDifficulty), difficulty);
        playerProxy.SetProperty(nameof(IPlayer.InstancePrimeLevel), primeLevel);
        playerProxy.SetProperty(nameof(IPlayer.InstanceScalingEnabled), scalingEnabled);

        return session;
    }

    private static ClientClosedInstanceSettings CreateClosedInstanceSettings(uint instancePortalUnitId)
    {
        var message = new ClientClosedInstanceSettings();
        SetProperty(message, nameof(ClientClosedInstanceSettings.InstancePortalUnitId), instancePortalUnitId);
        return message;
    }

    private static ClientResetSingleInstance CreateResetSingleInstance(uint instancePortalUnitId)
    {
        var message = new ClientResetSingleInstance();
        SetProperty(message, nameof(ClientResetSingleInstance.InstancePortalUnitId), instancePortalUnitId);
        return message;
    }

    private static ClientSetInstanceSettings CreateSetInstanceSettings(
        uint instancePortalUnitId,
        WorldDifficulty difficulty,
        byte primeLevel,
        ushort rally)
    {
        var message = new ClientSetInstanceSettings();
        SetProperty(message, nameof(ClientSetInstanceSettings.InstancePortalUnitId), instancePortalUnitId);
        SetProperty(message, nameof(ClientSetInstanceSettings.Difficulty), difficulty);
        SetProperty(message, nameof(ClientSetInstanceSettings.PrimeLevel), primeLevel);
        SetProperty(message, nameof(ClientSetInstanceSettings.Rally), rally);
        return message;
    }

    private static void SetProperty(object instance, string propertyName, object value)
    {
        PropertyInfo property = instance.GetType().GetProperty(propertyName)!;
        property.GetSetMethod(true)!.Invoke(instance, [value]);
    }
}
