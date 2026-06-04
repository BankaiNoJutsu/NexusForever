using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Misc;

namespace NexusForever.Game.Tests.Network;

public class ClientUnresolvedDiagnosticHandlerTests
{
    [Fact]
    public void DiagnosticHandlers_LogOnlyAndDoNotEmitServerState()
    {
        (string name, Action<IWorldSession> handle)[] cases =
        {
            (nameof(ClientAccountRealmData), session => new ClientAccountRealmDataHandler(NullLogger<ClientAccountRealmDataHandler>.Instance)
                .HandleMessage(session, new ClientAccountRealmData())),
            (nameof(Client0x00C8), session => new Client0x00C8Handler(NullLogger<Client0x00C8Handler>.Instance)
                .HandleMessage(session, new Client0x00C8())),
            (nameof(Client0x00ED), session => new Client0x00EDHandler(NullLogger<Client0x00EDHandler>.Instance)
                .HandleMessage(session, new Client0x00ED())),
            (nameof(Client0x011B), session => new Client0x011BHandler(NullLogger<Client0x011BHandler>.Instance)
                .HandleMessage(session, new Client0x011B())),
            (nameof(Client0x011D), session => new Client0x011DHandler(NullLogger<Client0x011DHandler>.Instance)
                .HandleMessage(session, new Client0x011D())),
            (nameof(Client0x012D), session => new Client0x012DHandler(NullLogger<Client0x012DHandler>.Instance)
                .HandleMessage(session, new Client0x012D())),
            (nameof(Client0x0550), session => new Client0x0550Handler(NullLogger<Client0x0550Handler>.Instance)
                .HandleMessage(session, new Client0x0550())),
            (nameof(Client0x062A), session => new Client0x062AHandler(NullLogger<Client0x062AHandler>.Instance)
                .HandleMessage(session, new Client0x062A())),
            (nameof(Client0x0634), session => new Client0x0634Handler(NullLogger<Client0x0634Handler>.Instance)
                .HandleMessage(session, new Client0x0634())),
            (nameof(Client0x063E), session => new Client0x063EHandler(NullLogger<Client0x063EHandler>.Instance)
                .HandleMessage(session, new Client0x063E())),
            (nameof(Client0x0701), session => new Client0x0701Handler(NullLogger<Client0x0701Handler>.Instance)
                .HandleMessage(session, new Client0x0701())),
            (nameof(ClientRealmListRealmRow), session => new ClientRealmListRealmRowHandler(NullLogger<ClientRealmListRealmRowHandler>.Instance)
                .HandleMessage(session, new ClientRealmListRealmRow())),
            (nameof(ClientRealmListMessageRow), session => new ClientRealmListMessageRowHandler(NullLogger<ClientRealmListMessageRowHandler>.Instance)
                .HandleMessage(session, new ClientRealmListMessageRow())),
            (nameof(ClientAddonModuleList), session => new ClientAddonModuleListHandler(NullLogger<ClientAddonModuleListHandler>.Instance)
                .HandleMessage(session, new ClientAddonModuleList())),
            (nameof(Client0x07E3), session => new Client0x07E3Handler(NullLogger<Client0x07E3Handler>.Instance)
                .HandleMessage(session, new Client0x07E3())),
            (nameof(Client0x0928), session => new Client0x0928Handler(NullLogger<Client0x0928Handler>.Instance)
                .HandleMessage(session, new Client0x0928()))
        };

        foreach ((string name, Action<IWorldSession> handle) in cases)
        {
            IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy);

            handle(session);

            Assert.True(
                sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessage)).Count == 0,
                $"{name} must not enqueue plaintext messages while semantics are diagnostic-only.");
            Assert.True(
                sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Count == 0,
                $"{name} must not enqueue encrypted messages while semantics are diagnostic-only.");
        }
    }

    private static IWorldSession CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);

        playerProxy.SetProperty(nameof(IPlayer.Guid), 10u);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        return session;
    }
}
