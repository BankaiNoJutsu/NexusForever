using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Static.Storefront;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Account;

namespace NexusForever.Game.Tests.Account.Inventory;

public class VirtualCurrencyPackageHandlerTests
{
    [Fact]
    public void Purchase_WithKnownPackageId_GrantsCurrencyAndSendsResults()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy);
        var handler = new ClientStorefrontPurchaseVirtualCurrencyPackageHandler(
            NullLogger<ClientStorefrontPurchaseVirtualCurrencyPackageHandler>.Instance);

        handler.HandleMessage(session, ReadPurchase(packageId: 1));

        Assert.Single(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencyAddAmount)));
        Assert.Single(GetMessages<ServerStorePurchaseVirtualCurrencyPackageResult>(sessionProxy));
        Assert.Single(GetMessages<ServerStoreCompleteOrderVirtualCurrencyPackageResult>(sessionProxy));
        Assert.Empty(GetMessages<ServerStoreError>(sessionProxy));
    }

    [Fact]
    public void Purchase_WithUnknownPackageId_ReturnsInvalidOffer()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out _);
        var handler = new ClientStorefrontPurchaseVirtualCurrencyPackageHandler(
            NullLogger<ClientStorefrontPurchaseVirtualCurrencyPackageHandler>.Instance);

        handler.HandleMessage(session, ReadPurchase(packageId: 99));

        ServerStoreError error = Assert.Single(GetMessages<ServerStoreError>(sessionProxy));
        Assert.Equal(StoreError.InvalidOffer, error.Error);
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        IAccountCurrencyManager currencyManager = RecordingDispatchProxy<IAccountCurrencyManager>.Create(out currencyProxy);

        accountProxy.SetProperty(nameof(IAccount.Id), 88001u);
        accountProxy.SetProperty(nameof(IAccount.CurrencyManager), currencyManager);
        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);

        return session;
    }

    private static ClientStorefrontPurchaseVirtualCurrencyPackage ReadPurchase(byte packageId)
    {
        using var stream = new MemoryStream([packageId]);
        using var reader = new GamePacketReader(stream);
        var purchase = new ClientStorefrontPurchaseVirtualCurrencyPackage();
        purchase.Read(reader);
        return purchase;
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
