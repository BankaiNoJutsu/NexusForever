using System.Reflection;
using System.Text;
using System.Xml;
using NexusForever.Cryptography;
using NexusForever.Database.Auth.Model;
using NexusForever.Network.Session;
using NexusForever.Network.Sts;
using NexusForever.Network.Sts.Model;
using NexusForever.StsServer.Network;
using NexusForever.StsServer.Network.Message;
using NexusForever.StsServer.Network.Message.Handler;
using NexusForever.StsServer.Network.Packet;

namespace NexusForever.Game.Tests.Sts;

public class StsSessionStateTests
{
    [Fact]
    public void HandlePacket_RejectsNonNoneHandlerWhenSessionStateDoesNotMatch()
    {
        var messageManager = new TestMessageManager(SessionState.Connected);
        var session = new StsSession(messageManager)
        {
            State = SessionState.None
        };

        HandlePacket(session, CreatePacket("/Auth/LoginStart"));

        Assert.False(messageManager.HandlerInvoked);
        Assert.False(messageManager.Message.ReadInvoked);
    }

    [Fact]
    public void HandlePacket_AllowsNonNoneHandlerWhenSessionStateMatches()
    {
        var messageManager = new TestMessageManager(SessionState.Connected);
        var session = new StsSession(messageManager)
        {
            State = SessionState.Connected
        };

        HandlePacket(session, CreatePacket("/Auth/LoginStart"));

        Assert.True(messageManager.HandlerInvoked);
    }

    [Fact]
    public void HandlePacket_AllowsNoneStateHandlerForCompatibilityRoutes()
    {
        var messageManager = new TestMessageManager(SessionState.None);
        var session = new StsSession(messageManager)
        {
            State = SessionState.LoginStart
        };

        HandlePacket(session, CreatePacket("/Auth/LoginFinish"));

        Assert.True(messageManager.HandlerInvoked);
    }

    [Fact]
    public void PresenceLogout_ResetsSessionStateForNextLoginStart()
    {
        var session = new StsSession(new TestMessageManager(SessionState.None))
        {
            Account = new AccountModel(),
            State = SessionState.LoginStart
        };

        PresenceHandler.HandlePresenceLogout(session, new PresenceLogoutMessage());

        Assert.Equal(SessionState.Connected, session.State);
        Assert.Null(session.Account);
        Assert.Null(session.KeyExchange);
    }

    [Fact]
    public void LoginFinish_ResetsSessionStateForNextLoginStart()
    {
        var account = new AccountModel
        {
            Id = 1u,
            Email = "player"
        };
        var session = new StsSession(new TestMessageManager(SessionState.None))
        {
            Account = account,
            State = SessionState.LoginStart
        };

        AuthenticationHandler.HandleLoginFinish(session, new ClientLoginFinishMessage());

        Assert.Equal(SessionState.Connected, session.State);
        Assert.Same(account, session.Account);
    }

    [Fact]
    public void KeyData_InvalidProof_ResetsSessionStateForNextLoginStart()
    {
        byte[] salt = [1, 2, 3, 4];
        byte[] verifier = Srp6Provider.GenerateVerifier(salt, "player", "password");
        var keyExchange = new Srp6Provider("player", salt, verifier);
        keyExchange.GenerateServerCredentials();

        var session = new StsSession(new TestMessageManager(SessionState.None))
        {
            Account = new AccountModel
            {
                Id = 1u,
                Email = "player"
            },
            KeyExchange = keyExchange,
            State = SessionState.LoginStart
        };

        AuthenticationHandler.HandleKeyData(session, CreateKeyData([1], [2, 3, 4]));

        Assert.Equal(SessionState.Connected, session.State);
        Assert.Null(session.Account);
        Assert.Null(session.KeyExchange);
    }

    private static ClientStsPacket CreatePacket(string uri)
    {
        byte[] data = Encoding.UTF8.GetBytes($"POST {uri} STS/1.0\r\nl:0\r\n\r\n");
        var packet = new ClientStsPacket(data);
        packet.SetBody([], 0u);
        return packet;
    }

    private static void HandlePacket(StsSession session, ClientStsPacket packet)
    {
        MethodInfo method = typeof(StsSession)
            .GetMethod("HandlePacket", BindingFlags.Instance | BindingFlags.NonPublic)!;
        method.Invoke(session, [packet]);
    }

    private static ClientKeyDataMessage CreateKeyData(byte[] a, byte[] m1)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write(a.Length);
            writer.Write(a);
            writer.Write(m1.Length);
            writer.Write(m1);
        }

        string keyData = Convert.ToBase64String(stream.ToArray());
        var document = new XmlDocument();
        document.LoadXml($"<Request><KeyData>{keyData}</KeyData></Request>");

        var message = new ClientKeyDataMessage();
        message.Read(document);
        return message;
    }

    private sealed class TestMessageManager : IMessageManager
    {
        private readonly SessionState handlerState;

        public TestMessage Message { get; } = new();
        public bool HandlerInvoked { get; private set; }

        public TestMessageManager(SessionState handlerState)
        {
            this.handlerState = handlerState;
        }

        public void Initialise()
        {
        }

        public IReadable GetMessage(string uri)
        {
            return Message;
        }

        public MessageHandlerInfo GetMessageHandler(string uri)
        {
            return new MessageHandlerInfo((NetworkSession _, IReadable _) =>
            {
                HandlerInvoked = true;
            }, handlerState);
        }
    }

    private sealed class TestMessage : IReadable
    {
        public bool ReadInvoked { get; private set; }

        public void Read(XmlDocument document)
        {
            ReadInvoked = true;
        }
    }
}
