using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.CREDDExchange;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Account;

namespace NexusForever.Game.Tests.Account.Inventory;

public class AccountTerminalHandlerTests
{
    [Fact]
    public void DailyLoginClaim_ReturnsRequestDailyLoginRewardsOperationResult()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IAccountInventoryManager.ClaimDailyLoginReward), AccountOperationResult.Ok);

        var handler = new ClientDailyLoginClaimRewardHandler(NullLogger<ClientDailyLoginClaimRewardHandler>.Instance);
        handler.HandleMessage(session, new ClientDailyLoginClaimReward());

        ServerAccountOperationResult result = Assert.Single(GetMessages<ServerAccountOperationResult>(sessionProxy));
        Assert.Equal(AccountOperation.RequestDailyLoginRewards, result.Operation);
        Assert.Equal(AccountOperationResult.Ok, result.Result);
    }

    [Fact]
    public void DailyLoginClaim_WithoutInventoryManagerReturnsGenericFail()
    {
        IWorldSession session = CreateSessionWithoutInventory(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        var handler = new ClientDailyLoginClaimRewardHandler(NullLogger<ClientDailyLoginClaimRewardHandler>.Instance);

        handler.HandleMessage(session, new ClientDailyLoginClaimReward());

        ServerAccountOperationResult result = Assert.Single(GetMessages<ServerAccountOperationResult>(sessionProxy));
        Assert.Equal(AccountOperation.RequestDailyLoginRewards, result.Operation);
        Assert.Equal(AccountOperationResult.GenericFail, result.Result);
    }

    [Fact]
    public void RedeemCoupon_WithKnownCode_ReturnsOk()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IAccountInventoryManager.CanAddItem), true);

        var handler = new ClientAccountRedeemCouponHandler(NullLogger<ClientAccountRedeemCouponHandler>.Instance);
        handler.HandleMessage(session, ReadCoupon("WELCOME"));

        ServerAccountOperationResult result = Assert.Single(GetMessages<ServerAccountOperationResult>(sessionProxy));
        Assert.Equal(AccountOperation.RedeemCoupon, result.Operation);
        Assert.Equal(AccountOperationResult.Ok, result.Result);
        Assert.Single(inventoryProxy.GetInvocations(nameof(IAccountInventoryManager.AddItem)));
    }

    [Fact]
    public void RedeemCoupon_WithoutInventoryManagerReturnsGenericFail()
    {
        IWorldSession session = CreateSessionWithoutInventory(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        var handler = new ClientAccountRedeemCouponHandler(NullLogger<ClientAccountRedeemCouponHandler>.Instance);

        handler.HandleMessage(session, ReadCoupon("WELCOME"));

        ServerAccountOperationResult result = Assert.Single(GetMessages<ServerAccountOperationResult>(sessionProxy));
        Assert.Equal(AccountOperation.RedeemCoupon, result.Operation);
        Assert.Equal(AccountOperationResult.GenericFail, result.Result);
    }

    [Fact]
    public void CreddRedeem_WithSufficientCredd_ReturnsOkAndRedeemResult()
    {
        IWorldSession session = CreateCreddSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, creddAmount: 5000ul);
        var handler = new ClientCREDDRedeemHandler(NullLogger<ClientCREDDRedeemHandler>.Instance);

        handler.HandleMessage(session, ReadCreddRedeem(2ul));

        ServerCREDDRedeemResult redeemResult = Assert.Single(GetMessages<ServerCREDDRedeemResult>(sessionProxy));
        Assert.Equal(CREDDRedeemResultType.Ok, (CREDDRedeemResultType)redeemResult.Value);

        ServerAccountOperationResult operationResult = Assert.Single(GetMessages<ServerAccountOperationResult>(sessionProxy));
        Assert.Equal(AccountOperation.CREDDRedeem, operationResult.Operation);
        Assert.Equal(AccountOperationResult.Ok, operationResult.Result);
    }

    [Fact]
    public void CreddRedeem_WithZeroAmount_ThrowsInvalidPacketValueException()
    {
        IWorldSession session = CreateCreddSession(out _, creddAmount: 5000ul);
        var handler = new ClientCREDDRedeemHandler(NullLogger<ClientCREDDRedeemHandler>.Instance);

        Assert.Throws<InvalidPacketValueException>(() => handler.HandleMessage(session, ReadCreddRedeem(0ul)));
    }

    [Fact]
    public void RedeemCoupon_WithUnknownCode_ReturnsInvalidCoupon()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out _);

        var handler = new ClientAccountRedeemCouponHandler(NullLogger<ClientAccountRedeemCouponHandler>.Instance);
        handler.HandleMessage(session, ReadCoupon("NOTREAL"));

        ServerAccountOperationResult result = Assert.Single(GetMessages<ServerAccountOperationResult>(sessionProxy));
        Assert.Equal(AccountOperation.RedeemCoupon, result.Operation);
        Assert.Equal(AccountOperationResult.InvalidCoupon, result.Result);
    }

    private static IWorldSession CreateCreddSession(
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        ulong creddAmount)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IAccountCurrencyManager accountCurrencyManager = RecordingDispatchProxy<IAccountCurrencyManager>.Create(out RecordingDispatchProxy<IAccountCurrencyManager> accountCurrencyProxy);
        ICurrencyManager playerCurrencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out RecordingDispatchProxy<ICurrencyManager> playerCurrencyProxy);
        IAccountEntitlementManager entitlementManager = RecordingDispatchProxy<IAccountEntitlementManager>.Create(out _);

        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        accountProxy.SetProperty(nameof(IAccount.CurrencyManager), accountCurrencyManager);
        accountProxy.SetProperty(nameof(IAccount.EntitlementManager), entitlementManager);
        accountCurrencyProxy.SetMethodReturn(nameof(IAccountCurrencyManager.CanAfford), true);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), playerCurrencyManager);
        playerCurrencyProxy.SetMethodReturn(nameof(ICurrencyManager.CurrencyAddAmount), true);

        return session;
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        IAccountInventoryManager inventory = RecordingDispatchProxy<IAccountInventoryManager>.Create(out inventoryProxy);

        accountProxy.SetProperty(nameof(IAccount.InventoryManager), inventory);
        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);

        return session;
    }

    private static IWorldSession CreateSessionWithoutInventory(out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);

        accountProxy.SetProperty(nameof(IAccount.InventoryManager), null);
        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);

        return session;
    }

    private static ClientCREDDRedeem ReadCreddRedeem(ulong creddAmount)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(creddAmount);
            writer.FlushBits();
        }

        using var packetStream = new MemoryStream(stream.ToArray());
        using var reader = new GamePacketReader(packetStream);
        var packet = new ClientCREDDRedeem();
        packet.Read(reader);
        return packet;
    }

    private static ClientAccountRedeemCoupon ReadCoupon(string code)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.WriteStringWide(code);
            writer.FlushBits();
        }

        using var packetStream = new MemoryStream(stream.ToArray());
        using var reader = new GamePacketReader(packetStream);
        var packet = new ClientAccountRedeemCoupon();
        packet.Read(reader);
        return packet;
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
