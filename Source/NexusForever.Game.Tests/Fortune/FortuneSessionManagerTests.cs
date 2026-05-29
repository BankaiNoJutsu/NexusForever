using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Fortune;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Fortune;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Fortune;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Fortune;

namespace NexusForever.Game.Tests.Fortune;

public class FortuneSessionManagerTests
{
    private const uint ClickEmptyResetValue = 3u;

    [Fact]
    public void SendStatus_WithNoSessionSendsRewardCatalogAndResetCards()
    {
        var manager = CreateManager(out StubFortuneRewardPool rewardPool);
        IWorldSession session = CreateSession(42u, out var sessionProxy);

        manager.SendStatus(session);

        object[] messages = GetEncryptedMessages(sessionProxy).ToArray();
        ServerFortuneRewards rewards = Assert.IsType<ServerFortuneRewards>(messages[0]);
        FortuneRewardCatalog catalog = rewardPool.GetRewardCatalog();
        Assert.Equal(catalog.Item2IdRewards, rewards.Item2IdRewards);
        Assert.Equal(catalog.RewardItemProbabilities, rewards.RewardItemProbabilities);

        ServerFortuneCards cards = Assert.IsType<ServerFortuneCards>(messages[1]);
        Assert.Equal(FortuneOperation.Reset, cards.Operation);
        Assert.All(cards.Rarity, rarity => Assert.Equal(RewardRarity.Normal, rarity));
        Assert.All(cards.AccountItemId, itemId => Assert.Equal(0u, itemId));
        Assert.All(cards.CardFlipped, Assert.False);
    }

    [Fact]
    public void Start_WithFortuneCoinDeductsCostAndDealsCards()
    {
        var manager = CreateManager(out _);
        IWorldSession session = CreateSessionWithCurrency(42u, out var sessionProxy, canAffordFortuneCoin: true, out var currencyProxy);

        manager.Start(session);

        RecordingDispatchProxy<IAccountCurrencyManager>.Invocation subtract = Assert.Single(
            currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencySubtractAmount)));
        Assert.Equal(AccountCurrencyType.FortuneCoin, subtract.Arguments[0]);
        Assert.Equal(1ul, subtract.Arguments[1]);

        ServerFortuneCards cards = GetEncryptedMessages(sessionProxy).OfType<ServerFortuneCards>().Single();
        Assert.Equal(FortuneOperation.Update, cards.Operation);
        Assert.Equal([11u, 22u, 33u], cards.AccountItemId);
        Assert.Equal([RewardRarity.Normal, RewardRarity.Rare, RewardRarity.Epic], cards.Rarity);
        Assert.All(cards.CardFlipped, Assert.False);
    }

    [Fact]
    public void Start_WithoutFortuneCoinSendsResetClick()
    {
        var manager = CreateManager(out _);
        IWorldSession session = CreateSession(42u, out var sessionProxy, canAffordFortuneCoin: false);

        manager.Start(session);

        ServerFortuneReset reset = GetEncryptedMessages(sessionProxy).OfType<ServerFortuneReset>().Single();
        Assert.Equal(ClickEmptyResetValue, reset.Unknown);
    }

    [Fact]
    public void FlipCard_AfterStartGrantsAccountItemAndUpdatesSelectedCard()
    {
        var manager = CreateManager(out _);
        IWorldSession session = CreateSessionWithInventory(42u, out var sessionProxy, canAffordFortuneCoin: true, out var inventoryProxy);

        manager.Start(session);
        manager.FlipCard(session, CreateFlipCard(1u));

        RecordingDispatchProxy<IAccountInventoryManager>.Invocation addItem = Assert.Single(
            inventoryProxy.GetInvocations(nameof(IAccountInventoryManager.AddItem)));
        Assert.Equal(22u, addItem.Arguments[0]);

        ServerFortuneCardUpdate update = GetEncryptedMessages(sessionProxy).OfType<ServerFortuneCardUpdate>().Single();
        Assert.Equal(FortuneOperation.Update, update.Operation);
        Assert.Equal([false, true, false], update.CardFlipped);
    }

    [Fact]
    public void SendStatus_AfterFlipSendsCurrentCardState()
    {
        var manager = CreateManager(out _);
        IWorldSession session = CreateSession(42u, out var sessionProxy, canAffordFortuneCoin: true);

        manager.Start(session);
        manager.FlipCard(session, CreateFlipCard(2u));
        manager.SendStatus(session);

        ServerFortuneCards statusCards = GetEncryptedMessages(sessionProxy).OfType<ServerFortuneCards>().Last();
        Assert.Equal(FortuneOperation.Update, statusCards.Operation);
        Assert.Equal([false, false, true], statusCards.CardFlipped);
        Assert.Equal([11u, 22u, 33u], statusCards.AccountItemId);
    }

    [Fact]
    public void SendStatus_UsesAccountScopedSessionState()
    {
        var manager = CreateManager(out _);
        IWorldSession firstSession = CreateSession(42u, out var firstSessionProxy, canAffordFortuneCoin: true);
        IWorldSession secondSession = CreateSession(77u, out var secondSessionProxy);

        manager.Start(firstSession);
        manager.FlipCard(firstSession, CreateFlipCard(0u));
        manager.SendStatus(secondSession);

        ServerFortuneCardUpdate firstUpdate = GetEncryptedMessages(firstSessionProxy).OfType<ServerFortuneCardUpdate>().Single();
        Assert.Equal([true, false, false], firstUpdate.CardFlipped);
        ServerFortuneCards secondCards = GetEncryptedMessages(secondSessionProxy).OfType<ServerFortuneCards>().Single();
        Assert.Equal(FortuneOperation.Reset, secondCards.Operation);
        Assert.All(secondCards.CardFlipped, Assert.False);
    }

    [Fact]
    public void Start_ReplacesExistingSessionForAccount()
    {
        var manager = CreateManager(out _);
        IWorldSession session = CreateSession(42u, out var sessionProxy, canAffordFortuneCoin: true);

        manager.Start(session);
        manager.FlipCard(session, CreateFlipCard(0u));
        manager.Start(session);

        ServerFortuneCards restartedCards = GetEncryptedMessages(sessionProxy)
            .OfType<ServerFortuneCards>()
            .Last();
        Assert.Equal(FortuneOperation.Update, restartedCards.Operation);
        Assert.All(restartedCards.CardFlipped, Assert.False);
    }

    [Fact]
    public void FlipCard_WithoutStartedSessionSendsResetClick()
    {
        var manager = CreateManager(out _);
        IWorldSession session = CreateSession(42u, out var sessionProxy);

        manager.FlipCard(session, CreateFlipCard(0u));

        ServerFortuneReset reset = GetEncryptedMessages(sessionProxy).OfType<ServerFortuneReset>().Single();
        Assert.Equal(ClickEmptyResetValue, reset.Unknown);
    }

    [Fact]
    public void FlipCard_WithInvalidIndexSendsResetClick()
    {
        var manager = CreateManager(out _);
        IWorldSession session = CreateSession(42u, out var sessionProxy, canAffordFortuneCoin: true);

        manager.Start(session);
        manager.FlipCard(session, CreateFlipCard(3u));

        ServerFortuneReset reset = GetEncryptedMessages(sessionProxy).OfType<ServerFortuneReset>().Single();
        Assert.Equal(ClickEmptyResetValue, reset.Unknown);
    }

    [Fact]
    public void FlipCard_AlreadyFlippedCardSendsResetClick()
    {
        var manager = CreateManager(out _);
        IWorldSession session = CreateSession(42u, out var sessionProxy, canAffordFortuneCoin: true);

        manager.Start(session);
        manager.FlipCard(session, CreateFlipCard(1u));
        manager.FlipCard(session, CreateFlipCard(1u));

        ServerFortuneReset reset = GetEncryptedMessages(sessionProxy).OfType<ServerFortuneReset>().Last();
        Assert.Equal(ClickEmptyResetValue, reset.Unknown);
    }

    private const ushort TestRealmId = 7;

    private static FortuneSessionManager CreateManager(out StubFortuneRewardPool rewardPool)
    {
        rewardPool = new StubFortuneRewardPool();
        IRealmContext realmContext = RecordingDispatchProxy<IRealmContext>.Create(out var realmProxy);
        realmProxy.SetProperty(nameof(IRealmContext.RealmId), TestRealmId);
        return new FortuneSessionManager(rewardPool, realmContext);
    }

    private static IWorldSession CreateSession(
        uint accountId,
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        bool canAffordFortuneCoin = false)
    {
        return BuildSession(accountId, out sessionProxy, canAffordFortuneCoin, out _, out _);
    }

    private static IWorldSession CreateSessionWithInventory(
        uint accountId,
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        bool canAffordFortuneCoin,
        out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy)
    {
        return BuildSession(accountId, out sessionProxy, canAffordFortuneCoin, out _, out inventoryProxy);
    }

    private static IWorldSession CreateSessionWithCurrency(
        uint accountId,
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        bool canAffordFortuneCoin,
        out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy)
    {
        return BuildSession(accountId, out sessionProxy, canAffordFortuneCoin, out currencyProxy, out _);
    }

    private static IWorldSession BuildSession(
        uint accountId,
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        bool canAffordFortuneCoin,
        out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy,
        out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        IAccountCurrencyManager currencyManager = RecordingDispatchProxy<IAccountCurrencyManager>.Create(out currencyProxy);
        IAccountInventoryManager inventoryManager = RecordingDispatchProxy<IAccountInventoryManager>.Create(out inventoryProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);

        accountProxy.SetProperty(nameof(IAccount.Id), accountId);
        accountProxy.SetProperty(nameof(IAccount.CurrencyManager), currencyManager);
        accountProxy.SetProperty(nameof(IAccount.InventoryManager), inventoryManager);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 9001ul);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        currencyProxy.SetMethodReturn(nameof(IAccountCurrencyManager.CanAfford), canAffordFortuneCoin);

        return session;
    }

    private static IEnumerable<object> GetEncryptedMessages(RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0]);
    }

    private static ClientFortuneFlipCard CreateFlipCard(uint selectedCardIndex)
    {
        var flipCard = (ClientFortuneFlipCard)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ClientFortuneFlipCard));
        typeof(ClientFortuneFlipCard)
            .GetProperty(nameof(ClientFortuneFlipCard.SelectedCardIndex))!
            .SetValue(flipCard, selectedCardIndex);
        return flipCard;
    }
}
