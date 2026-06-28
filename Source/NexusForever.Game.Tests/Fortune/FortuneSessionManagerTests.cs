using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Fortune;
using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Fortune;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Fortune;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Fortune;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Tests.Fortune;

public class FortuneSessionManagerTests
{
    private const uint ClickEmptyResetCode = 3u;

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
        Assert.Empty(rewards.MoneyRewards);
        Assert.Empty(rewards.RewardMoneyProbabilities);

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
    public void Start_WithNonDisplayableCardRewardSendsResetWithoutSpending()
    {
        var rewardPool = new StubFortuneRewardPool(
            cardRewards:
            [
                new FortuneCardReward(11u, 101u, RewardRarity.Normal),
                new FortuneCardReward(26u, 0u, RewardRarity.Normal),
                new FortuneCardReward(33u, 303u, RewardRarity.Rare)
            ],
            displayableAccountItemIds: new HashSet<uint> { 11u, 33u });
        var manager = CreateManager(rewardPool);
        IWorldSession session = CreateSessionWithCurrency(42u, out var sessionProxy, canAffordFortuneCoin: true, out var currencyProxy);

        manager.Start(session);

        Assert.Empty(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencySubtractAmount)));
        ServerFortuneReset reset = GetEncryptedMessages(sessionProxy).OfType<ServerFortuneReset>().Single();
        Assert.Equal(ClickEmptyResetCode, reset.ResetCode);
    }

    [Fact]
    public void Start_WithClaimableFortuneCoinItemClaimsBundleBeforeDealingCards()
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            new AccountItemEntry
            {
                Id                    = 901u,
                AccountCurrencyEnum   = (uint)AccountCurrencyType.FortuneCoin,
                AccountCurrencyAmount = 5ul
            });

        var manager = CreateManager(out _);
        IWorldSession session = CreateSessionWithClaimableFortuneCoinItem(
            42u,
            gameTableManager,
            out var sessionProxy,
            out var currencyProxy,
            out AccountInventoryManager inventoryManager,
            out Func<ulong> getFortuneCoinBalance);

        manager.Start(session);

        RecordingDispatchProxy<IAccountCurrencyManager>.Invocation add = Assert.Single(
            currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencyAddAmount)));
        Assert.Equal(AccountCurrencyType.FortuneCoin, add.Arguments[0]);
        Assert.Equal(5ul, add.Arguments[1]);

        RecordingDispatchProxy<IAccountCurrencyManager>.Invocation subtract = Assert.Single(
            currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencySubtractAmount)));
        Assert.Equal(AccountCurrencyType.FortuneCoin, subtract.Arguments[0]);
        Assert.Equal(1ul, subtract.Arguments[1]);

        Assert.Equal(4ul, getFortuneCoinBalance());
        Assert.Null(inventoryManager.GetItem(1ul));

        ServerFortuneCards cards = GetEncryptedMessages(sessionProxy).OfType<ServerFortuneCards>().Single();
        Assert.Equal(FortuneOperation.Update, cards.Operation);
        Assert.Equal([11u, 22u, 33u], cards.AccountItemId);
    }

    [Fact]
    public void Start_WithMismatchedTargetFortuneCoinItemSendsResetWithoutClaimingBundle()
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            new AccountItemEntry
            {
                Id                    = 901u,
                AccountCurrencyEnum   = (uint)AccountCurrencyType.FortuneCoin,
                AccountCurrencyAmount = 5ul
            });

        var manager = CreateManager(out _);
        IWorldSession session = CreateSessionWithClaimableFortuneCoinItem(
            42u,
            gameTableManager,
            out var sessionProxy,
            out var currencyProxy,
            out AccountInventoryManager inventoryManager,
            out Func<ulong> getFortuneCoinBalance,
            new NetworkIdentity
            {
                RealmId = TestRealmId,
                Id      = 9002ul
            });

        manager.Start(session);

        Assert.Empty(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencyAddAmount)));
        Assert.Empty(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencySubtractAmount)));
        Assert.Equal(0ul, getFortuneCoinBalance());
        Assert.NotNull(inventoryManager.GetItem(1ul));

        ServerFortuneReset reset = GetEncryptedMessages(sessionProxy).OfType<ServerFortuneReset>().Single();
        Assert.Equal(ClickEmptyResetCode, reset.ResetCode);
    }

    [Fact]
    public void Start_WithoutFortuneCoinSendsResetClick()
    {
        var manager = CreateManager(out _);
        IWorldSession session = CreateSession(42u, out var sessionProxy, canAffordFortuneCoin: false);

        manager.Start(session);

        ServerFortuneReset reset = GetEncryptedMessages(sessionProxy).OfType<ServerFortuneReset>().Single();
        Assert.Equal(ClickEmptyResetCode, reset.ResetCode);
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
        NetworkIdentity targetIdentity = Assert.IsType<NetworkIdentity>(addItem.Arguments[1]);
        Assert.Equal(TestRealmId, targetIdentity.RealmId);
        Assert.Equal(9001ul, targetIdentity.Id);
        Assert.True((bool)addItem.Arguments[3]);

        ServerFortuneCardUpdate update = GetEncryptedMessages(sessionProxy).OfType<ServerFortuneCardUpdate>().Single();
        Assert.True(update.HasUpdate);
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
        Assert.True(firstUpdate.HasUpdate);
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
        Assert.Equal(ClickEmptyResetCode, reset.ResetCode);
    }

    [Fact]
    public void FlipCard_WithInvalidIndexSendsResetClick()
    {
        var manager = CreateManager(out _);
        IWorldSession session = CreateSession(42u, out var sessionProxy, canAffordFortuneCoin: true);

        manager.Start(session);
        manager.FlipCard(session, CreateFlipCard(3u));

        ServerFortuneReset reset = GetEncryptedMessages(sessionProxy).OfType<ServerFortuneReset>().Single();
        Assert.Equal(ClickEmptyResetCode, reset.ResetCode);
    }

    [Fact]
    public void FlipCard_WithoutInventoryManagerSendsResetWithoutFlipping()
    {
        var manager = CreateManager(out _);
        IWorldSession session = CreateSessionWithoutInventoryManager(42u, out var sessionProxy);

        manager.Start(session);
        manager.FlipCard(session, CreateFlipCard(0u));

        ServerFortuneReset reset = GetEncryptedMessages(sessionProxy).OfType<ServerFortuneReset>().Single();
        Assert.Equal(ClickEmptyResetCode, reset.ResetCode);
        Assert.Empty(GetEncryptedMessages(sessionProxy).OfType<ServerFortuneCardUpdate>());

        manager.SendStatus(session);

        ServerFortuneCards statusCards = GetEncryptedMessages(sessionProxy).OfType<ServerFortuneCards>().Last();
        Assert.Equal(FortuneOperation.Update, statusCards.Operation);
        Assert.All(statusCards.CardFlipped, Assert.False);
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
        Assert.Equal(ClickEmptyResetCode, reset.ResetCode);
    }

    private const ushort TestRealmId = 7;

    private static FortuneSessionManager CreateManager(out StubFortuneRewardPool rewardPool)
    {
        rewardPool = new StubFortuneRewardPool();
        return CreateManager(rewardPool);
    }

    private static FortuneSessionManager CreateManager(StubFortuneRewardPool rewardPool)
    {
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

    private static IWorldSession CreateSessionWithoutInventoryManager(
        uint accountId,
        out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        IAccountCurrencyManager currencyManager = RecordingDispatchProxy<IAccountCurrencyManager>.Create(out var currencyProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);

        accountProxy.SetProperty(nameof(IAccount.Id), accountId);
        accountProxy.SetProperty(nameof(IAccount.CurrencyManager), currencyManager);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 9001ul);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        currencyProxy.SetMethodReturn(nameof(IAccountCurrencyManager.CanAfford), true);

        return session;
    }

    private static IWorldSession CreateSessionWithClaimableFortuneCoinItem(
        uint accountId,
        IGameTableManager gameTableManager,
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy,
        out AccountInventoryManager inventoryManager,
        out Func<ulong> getFortuneCoinBalance,
        NetworkIdentity targetPlayerIdentity = null)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        IAccountCurrencyManager currencyManager = RecordingDispatchProxy<IAccountCurrencyManager>.Create(out currencyProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);

        ulong fortuneCoinBalance = 0ul;
        getFortuneCoinBalance = () => fortuneCoinBalance;
        currencyProxy.SetMethodHandler(nameof(IAccountCurrencyManager.CanAfford), args =>
        {
            return (AccountCurrencyType)args[0] == AccountCurrencyType.FortuneCoin
                && fortuneCoinBalance >= (ulong)args[1];
        });
        currencyProxy.SetMethodHandler(nameof(IAccountCurrencyManager.CurrencyAddAmount), args =>
        {
            if ((AccountCurrencyType)args[0] == AccountCurrencyType.FortuneCoin)
                fortuneCoinBalance += (ulong)args[1];
            return null;
        });
        currencyProxy.SetMethodHandler(nameof(IAccountCurrencyManager.CurrencySubtractAmount), args =>
        {
            if ((AccountCurrencyType)args[0] == AccountCurrencyType.FortuneCoin)
                fortuneCoinBalance -= (ulong)args[1];
            return null;
        });

        accountProxy.SetProperty(nameof(IAccount.Id), accountId);
        accountProxy.SetProperty(nameof(IAccount.Session), session);
        accountProxy.SetProperty(nameof(IAccount.CurrencyManager), currencyManager);
        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);

        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 9001ul);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetProperty(nameof(IPlayer.Identity), new Identity
        {
            RealmId = TestRealmId,
            Id      = 9001ul
        });
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        var model = new AccountModel
        {
            Id = accountId
        };
        model.AccountInventory.Add(new AccountInventoryModel
        {
            Id                      = accountId,
            InventoryId             = 1ul,
            AccountItemId           = 901u,
            ClaimState              = (byte)AccountItemClaimState.CanClaim,
            HasTargetPlayerIdentity = targetPlayerIdentity?.Id != 0ul,
            TargetRealmId           = targetPlayerIdentity?.RealmId ?? 0,
            TargetCharacterId       = targetPlayerIdentity?.Id ?? 0ul
        });

        inventoryManager = new AccountInventoryManager(account, model, null, gameTableManager: gameTableManager);
        accountProxy.SetProperty(nameof(IAccount.InventoryManager), inventoryManager);

        return session;
    }

    private static GameTableManager CreateGameTableManager(params AccountItemEntry[] accountItems)
    {
        var gameTableManager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.AccountItem), CreateGameTable(accountItems));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.AccountCurrencyType), CreateGameTable(CreateDefaultAccountCurrencyTypeEntries()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.AccountItemCooldownGroup), CreateGameTable<AccountItemCooldownGroupEntry>());
        SetAutoProperty(gameTableManager, nameof(GameTableManager.DailyLoginReward), CreateGameTable<DailyLoginRewardEntry>());

        return gameTableManager;
    }

    private static AccountCurrencyTypeEntry[] CreateDefaultAccountCurrencyTypeEntries()
    {
        return
        [
            new AccountCurrencyTypeEntry
            {
                Id = (uint)AccountCurrencyType.FortuneCoin
            }
        ];
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0u : entries.Max(GetEntryId) + 1u
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1u)).ToArray();
        for (int i = 0; i < entries.Count; i++)
            lookup[GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        return (uint)typeof(T).GetField("Id", BindingFlags.Instance | BindingFlags.Public)!.GetValue(entry)!;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
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
