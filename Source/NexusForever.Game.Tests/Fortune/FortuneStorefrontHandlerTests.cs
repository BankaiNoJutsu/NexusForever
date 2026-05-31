using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Storefront;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Fortune;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Fortune;

namespace NexusForever.Game.Tests.Fortune;

public class FortuneStorefrontHandlerTests
{
    [Fact]
    public void StorefrontNotify_SendsCatalogContractBeforeFortuneStatus()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy);
        var storefrontManager = new RecordingStorefrontManager();
        var fortuneSessionManager = new RecordingFortuneSessionManager(storefrontManager);
        var handler = new ClientFortuneNotifyStorefrontHandler(
            NullLogger<ClientFortuneNotifyStorefrontHandler>.Instance,
            storefrontManager,
            fortuneSessionManager);

        handler.HandleMessage(session, new ClientFortuneNotifyStorefront());

        Assert.Single(inventoryProxy.GetInvocations(nameof(IAccountInventoryManager.SendInitialPackets)));
        Assert.True(storefrontManager.CatalogSent);
        Assert.Equal(42u, storefrontManager.AccountId);
        Assert.True(fortuneSessionManager.SendStatusCalled);
        Assert.True(fortuneSessionManager.CatalogWasSentBeforeStatus);
        Assert.Single(GetEncryptedMessages<ServerStorePurchaseHistoryReady>(sessionProxy));
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        IAccountInventoryManager inventoryManager = RecordingDispatchProxy<IAccountInventoryManager>.Create(out inventoryProxy);

        accountProxy.SetProperty(nameof(IAccount.Id), 42u);
        accountProxy.SetProperty(nameof(IAccount.Session), session);
        accountProxy.SetProperty(nameof(IAccount.InventoryManager), inventoryManager);
        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);

        return session;
    }

    private static IReadOnlyList<T> GetEncryptedMessages<T>(RecordingDispatchProxy<IWorldSession> sessionProxy)
        where T : class, IWritable
    {
        return sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Where(i => i.Arguments.Length == 1)
            .Select(i => i.Arguments[0])
            .OfType<T>()
            .ToList();
    }

    private sealed class RecordingStorefrontManager : IGlobalStorefrontManager
    {
        public bool CatalogSent { get; private set; }
        public uint AccountId { get; private set; }

        public void Initialise()
        {
        }

        public IOfferItem GetStoreOfferItem(uint offerId)
        {
            return null;
        }

        public void HandleCatalogRequest(IGameSession session, uint accountId)
        {
            CatalogSent = true;
            AccountId = accountId;
        }

        public void SendCatalogPackets(IGameSession session, uint accountId = 0)
        {
        }

        public void MarkAccountCatalogRequestedBeforeWorldLogin(uint accountId)
        {
        }

        public void SendBootstrapCatalogPacketsIfNeeded(IGameSession session, uint accountId)
        {
        }

        public void ClearCatalogDeliveryState(string sessionId, uint accountId = 0)
        {
        }
    }

    private sealed class RecordingFortuneSessionManager : IFortuneSessionManager
    {
        private readonly RecordingStorefrontManager storefrontManager;

        public bool SendStatusCalled { get; private set; }
        public bool CatalogWasSentBeforeStatus { get; private set; }

        public RecordingFortuneSessionManager(RecordingStorefrontManager storefrontManager)
        {
            this.storefrontManager = storefrontManager;
        }

        public void SendStatus(IWorldSession session)
        {
            SendStatusCalled = true;
            CatalogWasSentBeforeStatus = storefrontManager.CatalogSent;
        }

        public void Start(IWorldSession session)
        {
        }

        public void FlipCard(IWorldSession session, ClientFortuneFlipCard flipCard)
        {
        }
    }
}
