using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Account;

namespace NexusForever.Game.Tests.Account.Inventory;

[Collection(LegacyServiceProviderCollection.Name)]
public class StorefrontRequestPurchaseHistoryHandlerTests
{
    [Fact]
    public void Request_WithAccount_SendsPurchaseHistoryReady()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        var handler = new ClientStorefrontRequestPurchaseHistoryHandler(
            NullLogger<ClientStorefrontRequestPurchaseHistoryHandler>.Instance);

        handler.HandleMessage(session, new ClientStorefrontRequestPurchaseHistory());

        ServerStorePurchaseHistoryReady message = Assert.Single(GetMessages<ServerStorePurchaseHistoryReady>(sessionProxy));
        Assert.Empty(message.Rows);
    }

    [Fact]
    public void Request_WithoutAccount_DoesNotSendPurchaseHistory()
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        var handler = new ClientStorefrontRequestPurchaseHistoryHandler(
            NullLogger<ClientStorefrontRequestPurchaseHistoryHandler>.Instance);

        handler.HandleMessage(session, new ClientStorefrontRequestPurchaseHistory());

        Assert.Empty(GetMessages<ServerStorePurchaseHistoryReady>(sessionProxy));
    }

    [Fact]
    public void Read_WithEmptyPayload_Completes()
    {
        using var stream = new MemoryStream();
        using var reader = new GamePacketReader(stream);
        var request = new ClientStorefrontRequestPurchaseHistory();

        request.Read(reader);
    }

    private static IWorldSession CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);

        accountProxy.SetProperty(nameof(IAccount.Id), 88001u);
        accountProxy.SetProperty(nameof(IAccount.Session), session);
        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);

        return session;
    }

    private static IReadOnlyList<T> GetMessages<T>(RecordingDispatchProxy<IWorldSession> sessionProxy)
        where T : class, IWritable
    {
        return sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Where(i => i.Arguments.Length == 1)
            .Select(i => i.Arguments[0])
            .OfType<T>()
            .ToList();
    }
}
