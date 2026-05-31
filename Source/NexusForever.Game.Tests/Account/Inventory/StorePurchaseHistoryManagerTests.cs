using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Account.Inventory;

[Collection(LegacyServiceProviderCollection.Name)]
public class StorePurchaseHistoryManagerTests
{
    [Fact]
    public void SendPurchaseHistory_WithoutAuthDatabase_SendsEmptyReadyPacket()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = new ServiceCollection().BuildServiceProvider();

        try
        {
            IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
            IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
            accountProxy.SetProperty(nameof(IAccount.Id), 1u);
            accountProxy.SetProperty(nameof(IAccount.Session), session);

            StorePurchaseHistoryManager.SendPurchaseHistory(account);

            IReadOnlyList<ServerStorePurchaseHistoryReady> messages = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(invocation => invocation.Arguments[0])
                .OfType<ServerStorePurchaseHistoryReady>()
                .ToList();

            ServerStorePurchaseHistoryReady message = Assert.Single(messages);
            Assert.Empty(message.Rows);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }
}