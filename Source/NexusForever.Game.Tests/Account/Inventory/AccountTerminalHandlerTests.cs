using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Account.Entitlement;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
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
        IWorldSession session = CreateCreddSession(out RecordingDispatchProxy<IWorldSession> sessionProxy);
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
        IWorldSession session = CreateCreddSession(out _);
        var handler = new ClientCREDDRedeemHandler(NullLogger<ClientCREDDRedeemHandler>.Instance);

        Assert.Throws<InvalidPacketValueException>(() => handler.HandleMessage(session, ReadCreddRedeem(0ul)));
    }

    [Theory]
    [InlineData(5000ul, 3500ul, 2ul, false, false)]
    [InlineData(5000ul, 3000ul, 2ul, false, true)]
    [InlineData(0ul, ulong.MaxValue - 999ul, 1ul, false, false)]
    [InlineData(0ul, 0ul, ulong.MaxValue / 1000ul + 1ul, false, false)]
    [InlineData(0ul, 0ul, 1ul, true, false)]
    public void CreddRedeem_OnlyDebitsWhenTheFullCreditCanBeDelivered(ulong cap, ulong before, ulong requested, bool missingTable, bool fits)
    {
        IWorldSession session = CreateCreddSession(out var packets);
        var actor = (RecordingDispatchProxy<IPlayer>)(object)session.Player;
        var wallet = (RecordingDispatchProxy<IAccountCurrencyManager>)(object)session.Account.CurrencyManager;
        var entitlements = (RecordingDispatchProxy<IAccountEntitlementManager>)(object)session.Account.EntitlementManager;
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var data);
        if (!missingTable)
            data.SetProperty(nameof(IGameTableManager.CurrencyType), PlayerBindPointTests.Table(
                new CurrencyTypeEntry { Id = (uint)CurrencyType.Credits, CapAmount = cap }));
        var character = new CharacterModel { Id = 42 };
        if (before != 0)
            character.Currency.Add(new CharacterCurrencyModel { Id = 42, CurrencyId = (byte)CurrencyType.Credits, Amount = before });
        var credits = new CurrencyManager(tables);
        credits.Initialise(session.Player, character);
        actor.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        actor.SetProperty(nameof(IPlayer.CurrencyManager), credits);
        actor.SetProperty(nameof(IPlayer.AchievementManager), RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _));

        new ClientCREDDRedeemHandler(NullLogger<ClientCREDDRedeemHandler>.Instance).HandleMessage(session, ReadCreddRedeem(requested));

        var debits = wallet.GetInvocations(nameof(IAccountCurrencyManager.CurrencySubtractAmount));
        var uses = entitlements.GetInvocations(nameof(IAccountEntitlementManager.UpdateEntitlement));
        if (fits)
        {
            Assert.Equal(requested, Assert.Single(debits).Arguments[1]);
            Assert.Equal(EntitlementType.CREDDUsage, Assert.Single(uses).Arguments[0]);
            Assert.Equal(cap, Assert.Single(credits).Amount);
        }
        else
        {
            Assert.Empty(debits);
            Assert.Empty(uses);
            Assert.Equal(before, credits.SingleOrDefault()?.Amount ?? 0ul);
        }
        Assert.Equal(fits ? CREDDRedeemResultType.Ok : CREDDRedeemResultType.GenericFail,
            (CREDDRedeemResultType)Assert.Single(GetMessages<ServerCREDDRedeemResult>(packets)).Value);
    }

    [Fact]
    public void CreddRedeem_RejectsACharacterFromADifferentAccount()
    {
        IWorldSession session = CreateCreddSession(out var packets);
        var actor = (RecordingDispatchProxy<IPlayer>)(object)session.Player;
        actor.SetProperty(nameof(IPlayer.Account), RecordingDispatchProxy<IAccount>.Create(out _));

        new ClientCREDDRedeemHandler(NullLogger<ClientCREDDRedeemHandler>.Instance).HandleMessage(session, ReadCreddRedeem(1ul));

        var wallet = (RecordingDispatchProxy<IAccountCurrencyManager>)(object)session.Account.CurrencyManager;
        Assert.Empty(wallet.GetInvocations(nameof(IAccountCurrencyManager.CurrencySubtractAmount)));
        Assert.Equal(CREDDRedeemResultType.GenericFail,
            (CREDDRedeemResultType)Assert.Single(GetMessages<ServerCREDDRedeemResult>(packets)).Value);
    }

    [Theory]
    [InlineData(null, 0u, false)]
    [InlineData(1u, 1u, false)]
    [InlineData(5u, 1u, true)]
    public void CreddRedeem_RequiresAnAvailableUsageIncrement(uint? cap, uint before, bool fits)
    {
        IWorldSession session = CreateCreddSession(out var packets);
        var account = (RecordingDispatchProxy<IAccount>)(object)session.Account;
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var data);
        var model = new AccountModel();
        if (cap.HasValue)
        {
            data.SetProperty(nameof(IGameTableManager.Entitlement), PlayerBindPointTests.Table(
                new EntitlementEntry { Id = (uint)EntitlementType.CREDDUsage, MaxCount = cap.Value }));
            model.AccountEntitlement.Add(new AccountEntitlementModel { EntitlementId = (byte)EntitlementType.CREDDUsage, Amount = before });
        }
        var entitlements = new AccountEntitlementManager(session.Account, model, null, tables);
        account.SetProperty(nameof(IAccount.EntitlementManager), entitlements);

        new ClientCREDDRedeemHandler(NullLogger<ClientCREDDRedeemHandler>.Instance).HandleMessage(session, ReadCreddRedeem(1ul));

        var wallet = (RecordingDispatchProxy<IAccountCurrencyManager>)(object)session.Account.CurrencyManager;
        var credits = (RecordingDispatchProxy<ICurrencyManager>)(object)session.Player.CurrencyManager;
        Assert.Equal(fits ? 1 : 0, wallet.GetInvocations(nameof(IAccountCurrencyManager.CurrencySubtractAmount)).Count);
        Assert.Equal(fits ? 1 : 0, credits.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)).Count);
        Assert.Equal(fits ? before + 1u : before, entitlements.GetEntitlement(EntitlementType.CREDDUsage)?.Amount ?? 0u);
        Assert.Equal(fits ? CREDDRedeemResultType.Ok : CREDDRedeemResultType.GenericFail,
            (CREDDRedeemResultType)Assert.Single(GetMessages<ServerCREDDRedeemResult>(packets)).Value);
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

    private static IWorldSession CreateCreddSession(out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IAccountCurrencyManager accountCurrencyManager = RecordingDispatchProxy<IAccountCurrencyManager>.Create(out RecordingDispatchProxy<IAccountCurrencyManager> accountCurrencyProxy);
        ICurrencyManager playerCurrencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out RecordingDispatchProxy<ICurrencyManager> playerCurrencyProxy);
        IAccountEntitlementManager entitlementManager = RecordingDispatchProxy<IAccountEntitlementManager>.Create(out var entitlementProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        accountProxy.SetProperty(nameof(IAccount.CurrencyManager), accountCurrencyManager);
        accountProxy.SetProperty(nameof(IAccount.EntitlementManager), entitlementManager);
        accountProxy.SetProperty(nameof(IAccount.Session), session);
        accountCurrencyProxy.SetMethodReturn(nameof(IAccountCurrencyManager.CanAfford), true);
        entitlementProxy.SetMethodReturn(nameof(IAccountEntitlementManager.CanIncreaseEntitlement), true);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), playerCurrencyManager);
        playerCurrencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanCreditFullAmount), true);

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
