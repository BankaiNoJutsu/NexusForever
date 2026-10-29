using System.Net;
using System.Net.Sockets;
using System.Reflection;
using NexusForever.Network.Session;

namespace NexusForever.Game.Tests.Network;

public class NetworkSessionDisconnectTests
{
    [Fact]
    public void DisconnectBeforeAcceptStillCompletes()
    {
        var session = new TestSession();

        session.ForceDisconnect();
        session.Update(0);

        Assert.True(session.CanDispose());
        Assert.Equal(1, session.DisconnectCount);
    }

    [Fact]
    public void DisconnectWithAlreadyDisposedSocketStillCompletes()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var session = new TestSession();
        typeof(NetworkSession).GetField("socket", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, socket);
        socket.Dispose();

        session.ForceDisconnect();
        session.Update(0);

        Assert.True(session.CanDispose());
        Assert.Equal(1, session.DisconnectCount);
    }

    [Fact]
    public void DisconnectClosesSocketWhenShutdownThrows()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var session = new TestSession();
        // Avoid receive callbacks changing the state of the shutdown-error fixture.
        typeof(NetworkSession).GetField("socket", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, socket);

        session.ForceDisconnect();
        session.Update(0);

        Assert.True(socket.SafeHandle.IsClosed);
        Assert.True(session.CanDispose());
        Assert.Equal(1, session.DisconnectCount);

        session.ForceDisconnect();
        session.Update(0);
        Assert.Equal(1, session.DisconnectCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisconnectClosesConnectedAndPeerClosedSockets(bool closePeerFirst)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        using var peer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await peer.ConnectAsync(listener.LocalEndpoint, timeout.Token);
        using Socket socket = await listener.AcceptSocketAsync(timeout.Token);
        var session = new TestSession();
        session.OnAccept(socket);
        if (closePeerFirst)
            peer.Dispose();

        session.ForceDisconnect();
        session.Update(0);

        Assert.True(socket.SafeHandle.IsClosed);
        Assert.True(session.CanDispose());
        Assert.Equal(1, session.DisconnectCount);
        session.Update(0);
        Assert.Equal(1, session.DisconnectCount);
    }

    private sealed class TestSession : NetworkSession
    {
        public int DisconnectCount { get; private set; }

        protected override void OnDisconnect()
        {
            DisconnectCount++;
            base.OnDisconnect();
        }

        protected override uint OnData(byte[] buffer, int offset, int count) => 0;
    }
}
