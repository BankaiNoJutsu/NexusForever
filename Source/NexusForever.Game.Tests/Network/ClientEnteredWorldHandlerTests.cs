using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Misc;

namespace NexusForever.Game.Tests.Network;

public class ClientEnteredWorldHandlerTests
{
    [Fact]
    public void HandleMessage_PlayerStillLoading_EntersWorld()
    {
        ClientEnteredWorldHandler handler = new();
        IWorldSession session = CreateSession(isLoading: true, out RecordingDispatchProxy<IPlayer> playerProxy);

        handler.HandleMessage(session, new ClientEnteredWorld());

        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.OnEnteredWorld)));
    }

    [Fact]
    public void HandleMessage_DuplicateEnteredWorldAfterLoading_IsIgnored()
    {
        ClientEnteredWorldHandler handler = new();
        IWorldSession session = CreateSession(isLoading: false, out RecordingDispatchProxy<IPlayer> playerProxy);

        handler.HandleMessage(session, new ClientEnteredWorld());

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.OnEnteredWorld)));
    }

    [Fact]
    public void HandleMessage_WithoutPlayer_IsIgnored()
    {
        ClientEnteredWorldHandler handler = new();
        IWorldSession session = CreateSessionWithoutPlayer();

        handler.HandleMessage(session, new ClientEnteredWorld());
    }

    private static IWorldSession CreateSession(bool isLoading, out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IWorldSession session = CreateSessionWithoutPlayer(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.IsLoading), isLoading);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static IWorldSession CreateSessionWithoutPlayer()
    {
        return CreateSessionWithoutPlayer(out _);
    }

    private static IWorldSession CreateSessionWithoutPlayer(out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Characters), new List<CharacterModel>());
        sessionProxy.SetProperty(nameof(IWorldSession.Events), new EventQueue());
        sessionProxy.SetProperty(nameof(IWorldSession.Heartbeat), new SocketHeartbeat());
        return session;
    }
}
