using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Guild;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Guild;

namespace NexusForever.Game.Tests.Guild;

public class GuildBankHandlerBoundaryTests
{
    [Fact]
    public void GuildBankDiagnosticHandlers_LogOnlyAndDoNotEmitBankState()
    {
        (string name, Action<IWorldSession> handle)[] cases =
        {
            (nameof(ClientGuildBankMoneyTransaction), session => new ClientGuildBankMoneyTransactionHandler(NullLogger<ClientGuildBankMoneyTransactionHandler>.Instance)
                .HandleMessage(session, new ClientGuildBankMoneyTransaction())),
            (nameof(ClientGuildBankTransaction), session => new ClientGuildBankTransactionHandler(NullLogger<ClientGuildBankTransactionHandler>.Instance)
                .HandleMessage(session, new ClientGuildBankTransaction())),
            (nameof(ClientGuildBankTransaction2), session => new ClientGuildBankTransaction2Handler(NullLogger<ClientGuildBankTransaction2Handler>.Instance)
                .HandleMessage(session, new ClientGuildBankTransaction2())),
            (nameof(ClientGuildBankTabOpen), session => new ClientGuildBankTabOpenHandler(NullLogger<ClientGuildBankTabOpenHandler>.Instance)
                .HandleMessage(session, new ClientGuildBankTabOpen()))
        };

        foreach ((string name, Action<IWorldSession> handle) in cases)
        {
            IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy);

            handle(session);

            Assert.True(
                sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessage)).Count == 0,
                $"{name} must not enqueue plaintext guild bank state while bank economy semantics are blocked.");
            Assert.True(
                sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Count == 0,
                $"{name} must not enqueue encrypted guild bank state while bank economy semantics are blocked.");
        }
    }

    private static IWorldSession CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);

        playerProxy.SetProperty(nameof(IPlayer.Guid), 20u);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        return session;
    }
}
