using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Packet;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Pregame;
using NexusForever.Shared;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Misc;

namespace NexusForever.Game.Tests.Network;

public class GameSessionHeartbeatTests
{
    [Fact]
    public void HandlePacket_KeepAlivePacket_RefreshesHeartbeatThroughRegisteredHandler()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        MessageManager messageManager = CreateMessageManager(typeof(ClientPregameKeepAlive), typeof(ClientPingHandler));
        LegacyServiceProvider.Provider = new ServiceCollection()
            .AddTransient<ClientPingHandler>()
            .AddKeyedTransient<IReadable, ClientPregameKeepAlive>(GameMessageOpcode.ClientPregameKeepAlive)
            .BuildServiceProvider();

        try
        {
            var session = new TestWorldSession(messageManager);
            session.Heartbeat.Update(299d);

            session.HandlePacket(BuildPacket(GameMessageOpcode.ClientPregameKeepAlive));
            session.Heartbeat.Update(2d);

            Assert.False(session.Heartbeat.Flatline);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void HandlePacket_NonKeepAlivePacket_DoesNotRefreshHeartbeatImplicitly()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        MessageManager messageManager = CreateMessageManager(typeof(TestNeutralMessage), typeof(TestNeutralHandler));
        var handler = new TestNeutralHandler();
        LegacyServiceProvider.Provider = new ServiceCollection()
            .AddSingleton(handler)
            .AddKeyedTransient<IReadable, TestNeutralMessage>(GameMessageOpcode.ClientRequestInputKeySet)
            .BuildServiceProvider();

        try
        {
            var session = new TestWorldSession(messageManager);
            session.Heartbeat.Update(299d);

            session.HandlePacket(BuildPacket(GameMessageOpcode.ClientRequestInputKeySet));
            session.Heartbeat.Update(2d);

            Assert.Equal(1, handler.CallCount);
            Assert.True(session.Heartbeat.Flatline);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void HandlePacket_StatePacket_RefreshesHeartbeatThroughRegisteredHandler()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        MessageManager messageManager = CreateMessageManager(typeof(State), typeof(ClientStateHeartbeatHandler));
        LegacyServiceProvider.Provider = new ServiceCollection()
            .AddTransient<ClientStateHeartbeatHandler>()
            .AddKeyedTransient<IReadable, State>(GameMessageOpcode.State)
            .BuildServiceProvider();

        try
        {
            var session = new TestWorldSession(messageManager);
            session.Heartbeat.Update(299d);

            session.HandlePacket(BuildPacket(GameMessageOpcode.State));
            session.Heartbeat.Update(2d);

            Assert.False(session.Heartbeat.Flatline);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    private static MessageManager CreateMessageManager(Type messageType, Type handlerType)
    {
        var messageManager = new MessageManager(NullLogger<MessageManager>.Instance);
        messageManager.RegisterMessage(messageType);
        messageManager.RegisterMessageHandler(handlerType);
        return messageManager;
    }

    private static ClientGamePacket BuildPacket(GameMessageOpcode opcode)
    {
        byte[] data;
        using (var stream = new MemoryStream())
        {
            using (var writer = new GamePacketWriter(stream))
            {
                writer.Write((uint)opcode, 16);
                writer.FlushBits();
            }

            data = stream.ToArray();
        }

        return new ClientGamePacket
        {
            Data = data,
            IsEncrypted = false
        };
    }

    [Message(GameMessageOpcode.ClientRequestInputKeySet)]
    private sealed class TestNeutralMessage : IReadable
    {
        public void Read(GamePacketReader reader)
        {
        }
    }

    private sealed class TestNeutralHandler : IMessageHandler<IGameSession, TestNeutralMessage>
    {
        public int CallCount { get; private set; }

        public void HandleMessage(IGameSession session, TestNeutralMessage packet)
        {
            CallCount++;
        }
    }

    private sealed class TestWorldSession(IMessageManager messageManager) : GameSession(messageManager), IWorldSession
    {
        public IAccount Account { get; } = null!;
        public IPlayer Player { get; set; }
        public List<CharacterModel> Characters { get; } = [];
        public bool? IsQueued { get; set; }

        protected override IWritable BuildEncryptedMessage(byte[] data)
        {
            return new NoOpWritable();
        }

        public void Initialise(AccountModel account)
        {
        }

        public void SetEncryptionKey(byte[] sessionKey)
        {
        }

        public void ArmNextClientSpellEvidenceCapture(bool emitDiagnosticSpellBroadcasts = false)
        {
        }

        public bool TryConsumeNextClientSpellEvidenceCapture(out bool emitDiagnosticSpellBroadcasts)
        {
            emitDiagnosticSpellBroadcasts = false;
            return false;
        }

        public void ArmNextLootEvidenceCapture()
        {
        }

        public bool TryConsumeNextLootEvidenceCapture()
        {
            return false;
        }

        public void ArmNextAccountRuntimeEvidenceCapture()
        {
        }

        public bool TryConsumeNextAccountRuntimeEvidenceCapture()
        {
            return false;
        }

        public void ArmNextRewardRotationEvidenceCapture()
        {
        }

        public bool TryConsumeNextRewardRotationEvidenceCapture()
        {
            return false;
        }
    }

    private sealed class NoOpWritable : IWritable
    {
        public void Write(GamePacketWriter writer)
        {
        }
    }
}
