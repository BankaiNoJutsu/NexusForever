using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Network;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.CREDDExchange;
using NexusForever.WorldServer.Account;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Account;

namespace NexusForever.Game.Tests.Account.Inventory;

public class CREDDExchangeHandlerTests
{
    private const uint HistoryAccountId = 772201u;
    private const ulong HistoryCharacterId = 882201ul;
    private const uint InfoAccountId = 772202u;
    private const ulong InfoCharacterId = 882202ul;

    [Fact]
    public void RequestInfo_ReturnsNonEmptyPriceBucketsWhenOrdersExist()
    {
        ICREDDExchangeService creddExchangeService = CreateCREDDExchangeService();

        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            accountId: InfoAccountId,
            characterId: InfoCharacterId,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy);
        currencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);

        var submitHandler = new ClientCREDDExchangeBuyOrderSubmitHandler(
            NullLogger<ClientCREDDExchangeBuyOrderSubmitHandler>.Instance,
            creddExchangeService);
        submitHandler.HandleMessage(session, ReadBuyOrderSubmit(12000ul, submitFlag: true));

        var infoHandler = new ClientCREDDExchangeRequestInfoHandler(
            NullLogger<ClientCREDDExchangeRequestInfoHandler>.Instance,
            creddExchangeService);
        infoHandler.HandleMessage(session, new ClientCREDDExchangeRequestInfo());

        ServerCREDDExchangeInfoResults info = GetMessages<ServerCREDDExchangeInfoResults>(sessionProxy).Last();
        Assert.Equal(1u, info.BuyOrderCount);
        Assert.Equal(0u, info.SellOrderCount);
        Assert.Equal(12000ul, info.BuyOrderPrices[0]);
        Assert.Equal(1u, info.OwnedOrderCount);

        ServerCREDDExchangeOrderCacheRows cache = GetMessages<ServerCREDDExchangeOrderCacheRows>(sessionProxy).Last();
        ServerCREDDExchangeOrderCacheRows.Row row = Assert.Single(cache.Rows);
        Assert.Equal(12000u, row.CreditAmount);
        Assert.Equal(1u, row.SideFlag);
    }

    [Fact]
    public void RequestInfo_WithoutOrders_SkipsOrderCacheRows()
    {
        ICREDDExchangeService creddExchangeService = CreateCREDDExchangeService();

        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        var handler = new ClientCREDDExchangeRequestInfoHandler(
            NullLogger<ClientCREDDExchangeRequestInfoHandler>.Instance,
            creddExchangeService);

        handler.HandleMessage(session, new ClientCREDDExchangeRequestInfo());

        ServerCREDDExchangeInfoResults info = Assert.Single(GetMessages<ServerCREDDExchangeInfoResults>(sessionProxy));
        using var infoStream = new MemoryStream(WritePacket(info));
        Assert.Equal((int)ServerCREDDExchangeInfoResults.PayloadLength, infoStream.Length);
        Assert.Empty(GetMessages<ServerCREDDExchangeOrderCacheRows>(sessionProxy));

        ServerAccountOperationResult result = Assert.Single(GetMessages<ServerAccountOperationResult>(sessionProxy));
        Assert.Equal(AccountOperation.GetCREDDExchangeInfo, result.Operation);
        Assert.Equal(AccountOperationResult.Ok, result.Result);
    }

    [Fact]
    public void RequestHistory_ReturnsEmptyHistoryAndOperationResult()
    {
        ICREDDExchangeService creddExchangeService = CreateCREDDExchangeService();

        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        var handler = new ClientCREDDExchangeRequestHistoryHandler(
            NullLogger<ClientCREDDExchangeRequestHistoryHandler>.Instance,
            creddExchangeService);

        handler.HandleMessage(session, new ClientCREDDExchangeRequestHistory());

        ServerCREDDOperationHistory history = Assert.Single(GetMessages<ServerCREDDOperationHistory>(sessionProxy));
        Assert.Empty(history.Rows);

        ServerAccountOperationResult result = Assert.Single(GetMessages<ServerAccountOperationResult>(sessionProxy));
        Assert.Equal(AccountOperation.GetCREDDExchangeInfo, result.Operation);
        Assert.Equal(AccountOperationResult.Ok, result.Result);
    }

    [Fact]
    public void RequestHistory_ReturnsTransientSubmittedOrderHistory()
    {
        ICREDDExchangeService creddExchangeService = CreateCREDDExchangeService();

        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            accountId: HistoryAccountId,
            characterId: HistoryCharacterId,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy);
        currencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);

        var submitHandler = new ClientCREDDExchangeBuyOrderSubmitHandler(
            NullLogger<ClientCREDDExchangeBuyOrderSubmitHandler>.Instance,
            creddExchangeService);
        submitHandler.HandleMessage(session, ReadBuyOrderSubmit(123456ul, submitFlag: true));

        var historyHandler = new ClientCREDDExchangeRequestHistoryHandler(
            NullLogger<ClientCREDDExchangeRequestHistoryHandler>.Instance,
            creddExchangeService);
        historyHandler.HandleMessage(session, new ClientCREDDExchangeRequestHistory());

        ServerCREDDOperationHistory history = GetMessages<ServerCREDDOperationHistory>(sessionProxy).Last();
        ServerUnresolvedAccountIdentityRowListPayload.Row row = Assert.Single(history.Rows);

        Assert.Equal((uint)AccountOperation.BuyCREDD, row.Operation);
        Assert.True(row.IsInitiator);
        Assert.Equal(0u, row.LogAgeMinutes);
        Assert.Equal(1, row.Identity0.RealmId);
        Assert.Equal(HistoryCharacterId, row.Identity0.Id);
        Assert.Equal(123456ul, row.MoneyAmount);
    }

    private static IWorldSession CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        return CreateSession(out sessionProxy, accountId: 7001u, characterId: 8001ul, out _);
    }

    private static ICREDDExchangeService CreateCREDDExchangeService()
    {
        IDatabaseManager databaseManager = RecordingDispatchProxy<IDatabaseManager>.Create(out _);
        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(out _);
        return new CREDDExchangeService(databaseManager, playerManager);
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        uint accountId,
        ulong characterId,
        out RecordingDispatchProxy<ICurrencyManager> currencyProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out currencyProxy);
        IAccountEntitlementManager entitlementManager = RecordingDispatchProxy<IAccountEntitlementManager>.Create(out _);
        IAccountCurrencyManager accountCurrencyManager = RecordingDispatchProxy<IAccountCurrencyManager>.Create(out RecordingDispatchProxy<IAccountCurrencyManager> accountCurrencyProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);
        accountProxy.SetProperty(nameof(IAccount.Id), accountId);
        accountProxy.SetProperty(nameof(IAccount.EntitlementManager), entitlementManager);
        accountProxy.SetProperty(nameof(IAccount.CurrencyManager), accountCurrencyManager);
        accountCurrencyProxy.SetMethodReturn(nameof(IAccountCurrencyManager.CanAfford), true);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 123u);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        playerProxy.SetProperty(nameof(IPlayer.Identity), new Identity
        {
            RealmId = 1,
            Id      = characterId
        });
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);

        return session;
    }

    private static ClientCREDDExchangeBuyOrderSubmit ReadBuyOrderSubmit(ulong creditAmount, bool submitFlag)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(creditAmount);
            writer.Write(submitFlag);
            writer.FlushBits();
        }

        using var packetStream = new MemoryStream(stream.ToArray());
        using var reader = new GamePacketReader(packetStream);
        var submit = new ClientCREDDExchangeBuyOrderSubmit();
        submit.Read(reader);
        return submit;
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

    private static byte[] WritePacket<T>(T message) where T : IWritable
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        message.Write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }
}
