using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Fortune;
using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Fortune;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
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

    [Fact]
    public void PersistedFlipGrantsTargetedItemBeforeAnAccountSaveAndCannotGrantAgainAfterReload()
    {
        using var fixture = new PersistedFortune();
        FortuneSessionManager manager = fixture.CreateManager();
        manager.Start(fixture.Session);
        manager.FlipCard(fixture.Session, CreateFlipCard(1u));

        using (AuthContext read = fixture.Database.CreateContext())
        {
            AccountInventoryModel item = Assert.Single(read.AccountInventory);
            Assert.Equal(22u, item.AccountItemId);
            Assert.Equal(9001ul, item.TargetCharacterId);
            Assert.Equal(TestRealmId, item.TargetRealmId);
            Assert.True(item.HasTargetPlayerIdentity);
            Assert.NotNull(fixture.Session.Account.InventoryManager.GetItem(item.InventoryId));
            AccountFortuneSessionModel cards = Assert.Single(read.AccountFortuneSession);
            Assert.True(cards.Card1Flipped);
            Assert.True(cards.Card1Granted);
        }
        object[] messages = GetEncryptedMessages(fixture.Messages).ToArray();
        Assert.IsType<ServerAccountItemCacheAdd>(messages[^3]);
        Assert.IsType<ServerAccountItemAdd>(messages[^2]);
        Assert.Equal(new[] { false, true, false }, Assert.IsType<ServerFortuneCardUpdate>(messages[^1]).CardFlipped);

        fixture.CreateManager().FlipCard(fixture.Session, CreateFlipCard(1u));
        using AuthContext afterRetry = fixture.Database.CreateContext();
        Assert.Single(afterRetry.AccountInventory);
        Assert.Single(GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneCardUpdate>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RejectedRevealLeavesInventoryAndCardUnchangedAndAllowsRetry(bool rejectGrant)
    {
        using var fixture = new PersistedFortune();
        FortuneSessionManager manager = fixture.CreateManager();
        manager.Start(fixture.Session);
        using AuthContext schema = fixture.Database.CreateContext();
        schema.Database.ExecuteSqlRaw(rejectGrant ? """
            CREATE TRIGGER reject_fortune_flip BEFORE INSERT ON account_inventory
            BEGIN SELECT RAISE(ABORT, 'rejected grant'); END
            """ : """
            CREATE TRIGGER reject_fortune_flip BEFORE UPDATE ON account_fortune_session
            WHEN NEW.card0Flipped = 1 BEGIN SELECT RAISE(ABORT, 'rejected reveal'); END
            """);

        manager.FlipCard(fixture.Session, CreateFlipCard(0u));

        Assert.False(schema.AccountFortuneSession.AsNoTracking().Single().Card0Flipped);
        Assert.Empty(schema.AccountInventory);
        Assert.Empty(fixture.Session.Account.InventoryManager);
        Assert.Empty(GetEncryptedMessages(fixture.Messages).OfType<ServerAccountItemAdd>());
        Assert.Empty(GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneCardUpdate>());

        schema.Database.ExecuteSqlRaw("DROP TRIGGER reject_fortune_flip");
        manager.FlipCard(fixture.Session, CreateFlipCard(0u));

        Assert.True(schema.AccountFortuneSession.AsNoTracking().Single().Card0Granted);
        Assert.Equal(11u, Assert.Single(schema.AccountInventory).AccountItemId);
        Assert.Single(GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneCardUpdate>());
    }

    [Fact]
    public void CachedDealCannotReplaceADifferentDurableDeal()
    {
        using var fixture = new PersistedFortune();
        FortuneSessionManager manager = fixture.CreateManager();
        manager.Start(fixture.Session);
        using AuthContext schema = fixture.Database.CreateContext();
        schema.Database.ExecuteSqlRaw("UPDATE account_fortune_session SET card0AccountItemId = 22");

        manager.FlipCard(fixture.Session, CreateFlipCard(0u));
        manager.FlipCard(fixture.Session, CreateFlipCard(0u));

        AccountFortuneSessionModel current = schema.AccountFortuneSession.AsNoTracking().Single();
        Assert.Equal(22u, current.Card0AccountItemId);
        Assert.False(current.Card0Flipped);
        Assert.Empty(schema.AccountInventory);
        Assert.Empty(GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneCardUpdate>());

        bool rejectCards = true;
        fixture.Messages.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (rejectCards && args[0] is ServerFortuneCards)
                throw new IOException("Status cards enqueue failed.");
            return null;
        });
        Assert.Throws<IOException>(() => manager.SendStatus(fixture.Session));
        manager.FlipCard(fixture.Session, CreateFlipCard(0u));
        Assert.Empty(schema.AccountInventory);

        rejectCards = false;
        manager.SendStatus(fixture.Session);
        Assert.Equal(22u, GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneCards>().Last().AccountItemId[0]);

        manager.FlipCard(fixture.Session, CreateFlipCard(0u));
        Assert.Equal(22u, Assert.Single(schema.AccountInventory).AccountItemId);
    }

    [Fact]
    public void CommittedFlipStopsPublishingIfCacheAddRebindsTheCharacter()
    {
        using var fixture = new PersistedFortune();
        FortuneSessionManager manager = fixture.CreateManager();
        manager.Start(fixture.Session);
        IPlayer replacement = RecordingDispatchProxy<IPlayer>.Create(out var character);
        character.SetProperty(nameof(IPlayer.CharacterId), 9002ul);
        fixture.Messages.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerAccountItemCacheAdd)
                fixture.Messages.SetProperty(nameof(IWorldSession.Player), replacement);
            return null;
        });

        manager.FlipCard(fixture.Session, CreateFlipCard(0u));

        using AuthContext read = fixture.Database.CreateContext();
        Assert.True(read.AccountFortuneSession.Single().Card0Granted);
        Assert.Equal(9001ul, Assert.Single(read.AccountInventory).TargetCharacterId);
        object[] messages = GetEncryptedMessages(fixture.Messages).ToArray();
        Assert.Single(messages.OfType<ServerAccountItemCacheAdd>());
        Assert.Empty(messages.OfType<ServerAccountItemAdd>());
        Assert.Empty(messages.OfType<ServerFortuneCardUpdate>());
    }

    [Fact]
    public void CommittedFlipKeepsTheGrantButSuppressesTheRevealWhenADealReplacesItDuringPublication()
    {
        using var fixture = new PersistedFortune();
        FortuneSessionManager manager = fixture.CreateManager();
        manager.Start(fixture.Session);
        fixture.Messages.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerAccountItemCacheAdd)
                manager.Start(fixture.Session);
            return null;
        });

        manager.FlipCard(fixture.Session, CreateFlipCard(0u));

        using AuthContext read = fixture.Database.CreateContext();
        Assert.False(read.AccountFortuneSession.Single().Card0Flipped);
        Assert.Equal(11u, Assert.Single(read.AccountInventory).AccountItemId);
        object[] messages = GetEncryptedMessages(fixture.Messages).ToArray();
        Assert.All(messages.OfType<ServerFortuneCards>().Last().CardFlipped, Assert.False);
        Assert.Single(messages.OfType<ServerAccountItemAdd>());
        Assert.Empty(messages.OfType<ServerFortuneCardUpdate>());
    }

    [Fact]
    public void PersistedFlipRejectsTheOldWorldSessionEpoch()
    {
        using var fixture = new PersistedFortune();
        FortuneSessionManager manager = fixture.CreateManager();
        manager.Start(fixture.Session);
        fixture.Owner.SetProperty(nameof(IAccount.WorldSessionEpoch), 1ul);
        using AuthContext context = fixture.Database.CreateContext();
        context.AccountWorldSession.Add(new AccountWorldSessionModel { AccountId = fixture.Session.Account.Id, Epoch = 2ul });
        context.SaveChanges();

        manager.FlipCard(fixture.Session, CreateFlipCard(0u));

        Assert.False(context.AccountFortuneSession.Single().Card0Flipped);
        Assert.Empty(context.AccountInventory);
        Assert.Empty(GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneCardUpdate>());
    }

    [Fact]
    public void PersistedStartCommitsWalletDebitAndDealBeforePublishing()
    {
        using var fixture = new PersistedFortune(coins: 2);
        fixture.Messages.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerFortuneCards { Operation: FortuneOperation.Update })
            {
                using AuthContext read = fixture.Database.CreateContext();
                Assert.Equal(1ul, read.AccountCurrency.Single().Amount);
                Assert.Equal(11u, read.AccountFortuneSession.Single().Card0AccountItemId);
            }
            return null;
        });
        fixture.CreateManager().Start(fixture.Session);

        Assert.Equal(1ul, fixture.Session.Account.CurrencyManager.GetCurrencyAmount(AccountCurrencyType.FortuneCoin));
        Assert.Single(GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneCards>());
    }

    [Fact]
    public void PersistedStartKeepsTheOriginalDealWhenTheDebitIsRejected()
    {
        using var fixture = new PersistedFortune(coins: 2);
        FortuneSessionManager manager = fixture.CreateManager();
        manager.Start(fixture.Session);
        manager.FlipCard(fixture.Session, CreateFlipCard(0u));
        using AuthContext read = fixture.Database.CreateContext();
        read.Database.ExecuteSqlRaw("""
            CREATE TRIGGER reject_fortune_debit BEFORE UPDATE ON account_currency
            BEGIN SELECT RAISE(ABORT, 'rejected debit'); END
            """);

        manager.Start(fixture.Session);

        Assert.True(read.AccountFortuneSession.AsNoTracking().Single().Card0Flipped);
        Assert.Equal(1ul, read.AccountCurrency.Single().Amount);
        Assert.Equal(1ul, fixture.Session.Account.CurrencyManager.GetCurrencyAmount(AccountCurrencyType.FortuneCoin));
        Assert.Single(GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneCards>());
    }

    [Fact]
    public void PersistedStartConsumesTheTargetedBundleWithItsDealAndKeepsChange()
    {
        using var fixture = new PersistedFortune(coins: 0, bundleTarget: 9001);
        FortuneSessionManager manager = fixture.CreateManager();
        using AuthContext read = fixture.Database.CreateContext();
        read.Database.ExecuteSqlRaw("""
            CREATE TRIGGER reject_fortune_deal BEFORE INSERT ON account_fortune_session
            BEGIN SELECT RAISE(ABORT, 'rejected deal'); END
            """);

        manager.Start(fixture.Session);

        Assert.Empty(read.AccountFortuneSession);
        Assert.Empty(read.AccountCurrency);
        Assert.NotNull(fixture.Session.Account.InventoryManager.GetItem(1));
        Assert.Empty(GetEncryptedMessages(fixture.Messages).OfType<ServerAccountItemDelete>());
        read.Database.ExecuteSqlRaw("DROP TRIGGER reject_fortune_deal");

        manager.Start(fixture.Session);

        Assert.Empty(read.AccountInventory);
        Assert.Equal(4ul, read.AccountCurrency.Single().Amount);
        Assert.Equal(4ul, fixture.Session.Account.CurrencyManager.GetCurrencyAmount(AccountCurrencyType.FortuneCoin));
        Assert.Null(fixture.Session.Account.InventoryManager.GetItem(1));
        Assert.Single(GetEncryptedMessages(fixture.Messages).OfType<ServerAccountItemDelete>());
        Assert.Single(GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneCards>());
    }

    [Fact]
    public void PersistedStartRejectsACachedBundleConsumedByAnotherWriter()
    {
        using var fixture = new PersistedFortune(coins: 0, bundleTarget: 9001);
        using AuthContext read = fixture.Database.CreateContext();
        read.Database.ExecuteSqlRaw("DELETE FROM account_inventory");

        fixture.CreateManager().Start(fixture.Session);

        Assert.Empty(read.AccountCurrency);
        Assert.Empty(read.AccountFortuneSession);
        Assert.Empty(GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneCards>());
    }

    [Fact]
    public void PersistedStartWaitsForAnUnflushedBundleBeforeConsumingIt()
    {
        using var fixture = new PersistedFortune(coins: 0);
        IAccountInventoryManager inventory = fixture.Session.Account.InventoryManager;
        inventory.AddItem(901, notify: false);

        fixture.CreateManager().Start(fixture.Session);

        using AuthContext read = fixture.Database.CreateContext();
        Assert.Empty(read.AccountInventory);
        Assert.Empty(read.AccountFortuneSession);
        fixture.CompletePlayerSave();

        inventory.Save(read);
        read.SaveChanges();
        Assert.Empty(read.AccountInventory);
        Assert.Equal(4ul, read.AccountCurrency.Single().Amount);
        Assert.Single(read.AccountFortuneSession);
        Assert.Single(read.AccountInventoryReceipt);
        Assert.Empty(inventory);
    }

    [Fact]
    public void PersistedStartSettlesAnAlreadyClaimedBundleBeforeSpendingItsCoins()
    {
        using var fixture = new PersistedFortune(coins: 1, bundleTarget: 9001);
        FortuneSessionManager manager = fixture.CreateManager();
        manager.Start(fixture.Session);
        Assert.Equal(AccountOperationResult.Ok, fixture.Session.Account.InventoryManager.TakeItem(fixture.Session.Player, 1));
        using AuthContext read = fixture.Database.CreateContext();
        Assert.Single(read.AccountInventory);
        Assert.Equal(0ul, read.AccountCurrency.AsNoTracking().Single().Amount);

        manager.Start(fixture.Session);
        manager.SendStatus(fixture.Session);

        Assert.Equal(0ul, read.AccountCurrency.AsNoTracking().Single().Amount);
        Assert.Equal(2, GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneCards>().Count());
        fixture.CompletePlayerSave();

        Assert.Empty(read.AccountInventory);
        Assert.Equal(4ul, read.AccountCurrency.AsNoTracking().Single().Amount);
        Assert.Equal(4ul, fixture.Session.Account.CurrencyManager.GetCurrencyAmount(AccountCurrencyType.FortuneCoin));
        Assert.Single(read.AccountFortuneSession);
        Assert.Equal(3, GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneCards>().Count());
    }

    [Fact]
    public void PersistedStartFindsAValidBundleAfterAnOrdinarySaveAndAnUnclaimableCandidate()
    {
        using var fixture = new PersistedFortune(coins: 0, bundleTarget: 9001, unclaimableFirst: true);
        IAccountCurrencyManager wallet = fixture.Session.Account.CurrencyManager;
        wallet.CurrencyAddAmount(AccountCurrencyType.FortuneCoin, 1);
        wallet.CurrencySubtractAmount(AccountCurrencyType.FortuneCoin, 1);

        fixture.CreateManager().Start(fixture.Session);
        fixture.CompletePlayerSave();

        using AuthContext read = fixture.Database.CreateContext();
        Assert.Equal(902u, Assert.Single(read.AccountInventory).AccountItemId);
        Assert.Equal(4ul, read.AccountCurrency.Single().Amount);
        Assert.Single(read.AccountFortuneSession);
        Assert.Single(GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneCards>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PersistedStartRollsBackIfATriggerChangesTheCommittedDealOrBundle(bool restoreBundle)
    {
        using var fixture = new PersistedFortune(coins: 0, bundleTarget: 9001);
        using AuthContext read = fixture.Database.CreateContext();
        read.Database.ExecuteSqlRaw(restoreBundle ? """
            CREATE TRIGGER rewrite_fortune AFTER DELETE ON account_inventory BEGIN
                INSERT INTO account_inventory(id,inventoryId,accountItemId,targetRealmId,targetCharacterId,hasTargetPlayerIdentity)
                VALUES(OLD.id,OLD.inventoryId,OLD.accountItemId,OLD.targetRealmId,OLD.targetCharacterId,OLD.hasTargetPlayerIdentity);
            END
            """ : """
            CREATE TRIGGER rewrite_fortune AFTER INSERT ON account_fortune_session BEGIN
                UPDATE account_fortune_session SET card0AccountItemId = 22 WHERE id = NEW.id;
            END
            """);

        fixture.CreateManager().Start(fixture.Session);

        Assert.Empty(read.AccountFortuneSession);
        Assert.Empty(read.AccountCurrency);
        Assert.Single(read.AccountInventory);
        Assert.NotNull(fixture.Session.Account.InventoryManager.GetItem(1));
        Assert.Empty(GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneCards>());
    }

    [Fact]
    public void OrdinaryClaimCannotUseABundleReservedByAFortuneDeal()
    {
        using var fixture = new PersistedFortune(coins: 0, bundleTarget: 9001);
        var inventory = (AccountInventoryManager)fixture.Session.Account.InventoryManager;
        using (AuthContext context = fixture.Database.CreateContext())
        {
            inventory.EnlistFortuneCoinBundle(context, fixture.Session.Player, inventory.GetItem(1), () => true);
            Assert.Equal(AccountOperationResult.InvalidInventoryItem, inventory.TakeItem(fixture.Session.Player, 1));
            Assert.Equal(0ul, fixture.Session.Account.CurrencyManager.GetCurrencyAmount(AccountCurrencyType.FortuneCoin));
        }
        Assert.Equal(AccountOperationResult.Ok, inventory.TakeItem(fixture.Session.Player, 1));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LostStartAcknowledgementAdoptsTheCommittedDealAndFunding(bool useBundle)
    {
        using var fixture = new PersistedFortune(coins: useBundle ? 0ul : 2ul, bundleTarget: useBundle ? 9001ul : null);
        using var fault = new CommitFault(fixture.ConnectionString);
        FortuneSessionManager manager = fixture.CreateManager();

        manager.Start(fixture.Session);

        Assert.Equal(1, fault.CommitFailures);
        using AuthContext read = fixture.Database.CreateContext();
        ulong expected = useBundle ? 4ul : 1ul;
        Assert.Equal(expected, read.AccountCurrency.Single().Amount);
        Assert.Equal(expected, fixture.Session.Account.CurrencyManager.GetCurrencyAmount(AccountCurrencyType.FortuneCoin));
        Assert.Empty(fixture.Session.Account.InventoryManager);
        Assert.Empty(read.AccountInventory);
        Assert.Single(read.AccountFortuneSession);
        Assert.Equal(useBundle ? 2 : 1, read.AccountCurrencyOperation.Count());
        Assert.Single(GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneCards>());
        Assert.Empty(GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneReset>());
    }

    [Fact]
    public void LostFlipAcknowledgementAdoptsTheExactRewardAndDoesNotPayAgain()
    {
        using var fixture = new PersistedFortune();
        FortuneSessionManager manager = fixture.CreateManager();
        manager.Start(fixture.Session);
        using var fault = new CommitFault(fixture.ConnectionString);

        manager.FlipCard(fixture.Session, CreateFlipCard(1u));

        Assert.Equal(1, fault.CommitFailures);
        using AuthContext read = fixture.Database.CreateContext();
        AccountInventoryModel item = Assert.Single(read.AccountInventory);
        Assert.Equal(22u, item.AccountItemId);
        Assert.Equal(9001ul, item.TargetCharacterId);
        Assert.NotNull(fixture.Session.Account.InventoryManager.GetItem(item.InventoryId));
        Assert.True(read.AccountFortuneSession.Single().Card1Granted);
        Assert.Single(GetEncryptedMessages(fixture.Messages).OfType<ServerAccountItemAdd>());
        Assert.Single(GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneCardUpdate>());
        Assert.Empty(GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneReset>());

        manager.FlipCard(fixture.Session, CreateFlipCard(1u));
        Assert.Single(read.AccountInventory.AsNoTracking());
        Assert.Single(GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneCardUpdate>());
    }

    [Fact]
    public void FailureBeforeCommitKeepsTheCoinAndAllowsANewDeal()
    {
        using var fixture = new PersistedFortune(coins: 2);
        using var fault = new CommitFault(fixture.ConnectionString, beforeCommit: true);
        FortuneSessionManager manager = fixture.CreateManager();

        manager.Start(fixture.Session);

        Assert.Equal(1, fault.CommitFailures);
        using AuthContext read = fixture.Database.CreateContext();
        Assert.Empty(read.AccountFortuneSession);
        Assert.Empty(read.AccountCurrencyOperation);
        Assert.Equal(2ul, read.AccountCurrency.AsNoTracking().Single().Amount);
        Assert.Single(GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneReset>());
        Assert.Empty(fixture.Messages.GetInvocations(nameof(IWorldSession.ForceDisconnect)));

        manager.Start(fixture.Session);
        Assert.Equal(1ul, read.AccountCurrency.AsNoTracking().Single().Amount);
        Assert.Single(GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneCards>());
    }

    [Fact]
    public void LostAcknowledgementWithAChangedWalletRequiresAccountReload()
    {
        using var fixture = new PersistedFortune(coins: 2);
        using var fault = new CommitFault(fixture.ConnectionString, onFault: () =>
        {
            using AuthContext writer = fixture.Database.CreateContext();
            writer.Database.ExecuteSqlRaw("UPDATE account_currency SET amount = 3 WHERE currencyId = 5");
        });

        fixture.CreateManager().Start(fixture.Session);

        Assert.Equal(1, fault.CommitFailures);
        using AuthContext read = fixture.Database.CreateContext();
        Assert.Equal(3ul, read.AccountCurrency.Single().Amount);
        Assert.Equal(1ul, read.AccountCurrencyOperation.Single().AfterAmount);
        Assert.Single(read.AccountFortuneSession);
        Assert.Empty(GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneCards>());
        Assert.Empty(GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneReset>());
        Assert.Single(fixture.Messages.GetInvocations(nameof(IWorldSession.ForceDisconnect)));
        Assert.False(fixture.Session.CanProcessIncomingPackets);
    }

    [Fact]
    public void AConflictingReceiptDoesNotMasqueradeAsARolledBackDeal()
    {
        using var fixture = new PersistedFortune(coins: 2);
        using var fault = new CommitFault(fixture.ConnectionString, onFault: () =>
        {
            using AuthContext writer = fixture.Database.CreateContext();
            writer.Database.ExecuteSqlRaw("UPDATE account_pending_item_delivery SET pendingItemId = 1");
        });

        fixture.CreateManager().Start(fixture.Session);

        Assert.Equal(1, fault.CommitFailures);
        using AuthContext read = fixture.Database.CreateContext();
        Assert.Equal(1ul, read.AccountPendingItemDelivery.Single().PendingItemId);
        Assert.Equal(1ul, read.AccountCurrency.Single().Amount);
        Assert.Single(read.AccountFortuneSession);
        Assert.Empty(GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneCards>());
        Assert.Empty(GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneReset>());
        Assert.Single(fixture.Messages.GetInvocations(nameof(IWorldSession.ForceDisconnect)));
    }

    [Fact]
    public void ARejectedDealWithASupersededEpochDoesNotPublishAReset()
    {
        using var fixture = new PersistedFortune(coins: 2);
        fixture.Owner.SetProperty(nameof(IAccount.WorldSessionEpoch), 1ul);
        using AuthContext seed = fixture.Database.CreateContext();
        seed.AccountWorldSession.Add(new AccountWorldSessionModel { AccountId = fixture.Session.Account.Id, Epoch = 1ul });
        seed.SaveChanges();
        using var fault = new CommitFault(fixture.ConnectionString, beforeCommit: true, onDisposeAfterFault: () =>
        {
            using AuthContext writer = fixture.Database.CreateContext();
            writer.AccountWorldSession.ExecuteUpdate(setters => setters.SetProperty(row => row.Epoch, 2ul));
        });

        fixture.CreateManager().Start(fixture.Session);

        Assert.Equal(1, fault.CommitFailures);
        using AuthContext read = fixture.Database.CreateContext();
        Assert.Equal(2ul, read.AccountWorldSession.Single().Epoch);
        Assert.Equal(2ul, read.AccountCurrency.Single().Amount);
        Assert.Empty(read.AccountFortuneSession);
        Assert.Empty(GetEncryptedMessages(fixture.Messages).OfType<ServerFortuneReset>());
        Assert.Single(fixture.Messages.GetInvocations(nameof(IWorldSession.ForceDisconnect)));
    }

    [Fact]
    public void AFailedLiveCompletionAfterCommitRequiresReload()
    {
        using var fixture = new PersistedFortune();
        using AuthContext context = fixture.Database.CreateContext();
        uint accountId = fixture.Session.Account.Id;
        context.EnlistSaveWork(unit => unit.AccountFortuneSession.Add(new AccountFortuneSessionModel { Id = accountId, Card0AccountItemId = 11 }),
            committed =>
            {
                Assert.True(committed);
                throw new IOException("The live deal could not be finalized.");
            }, accountId: accountId);

        var error = Assert.Throws<AuthCommitOutcomeUnknownException>(() => context.SaveChangesConfirmingCommit(() => true));
        Assert.IsType<IOException>(error.InnerException);

        using AuthContext read = fixture.Database.CreateContext();
        Assert.Equal(11u, read.AccountFortuneSession.Single().Card0AccountItemId);
    }

    private sealed class CommitFault : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object>>, IDisposable
    {
        private readonly string connectionString;
        private readonly string commitEvent;
        private readonly Action onFault;
        private readonly Action onDisposeAfterFault;
        private readonly IDisposable allListeners;
        private readonly List<IDisposable> subscriptions = [];
        private int commitFailures;
        private int disposedAfterFault;
        public int CommitFailures => Volatile.Read(ref commitFailures);

        public CommitFault(string connectionString, bool beforeCommit = false, Action onFault = null, Action onDisposeAfterFault = null)
        {
            this.connectionString = connectionString;
            this.onFault = onFault;
            this.onDisposeAfterFault = onDisposeAfterFault;
            commitEvent = beforeCommit ? RelationalEventId.TransactionCommitting.Name : RelationalEventId.TransactionCommitted.Name;
            allListeners = DiagnosticListener.AllListeners.Subscribe(this);
        }

        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == "Microsoft.EntityFrameworkCore")
                lock (subscriptions)
                    subscriptions.Add(listener.Subscribe(this, name => name == commitEvent ||
                        (onDisposeAfterFault != null && name == RelationalEventId.TransactionDisposed.Name)));
        }

        public void OnNext(KeyValuePair<string, object> item)
        {
            if (item.Value is not TransactionEventData { Context: AuthContext context } ||
                context.Database.GetConnectionString() != connectionString)
                return;
            if (item.Key == commitEvent && Interlocked.CompareExchange(ref commitFailures, 1, 0) == 0)
            {
                onFault?.Invoke();
                throw new IOException("Lost the Fortune Auth commit acknowledgement.");
            }
            if (item.Key == RelationalEventId.TransactionDisposed.Name && CommitFailures != 0 &&
                Interlocked.CompareExchange(ref disposedAfterFault, 1, 0) == 0)
                onDisposeAfterFault?.Invoke();
        }

        public void OnError(Exception error) { }
        public void OnCompleted() { }

        public void Dispose()
        {
            allListeners.Dispose();
            lock (subscriptions)
                foreach (IDisposable subscription in subscriptions)
                    subscription.Dispose();
        }
    }

    private sealed class PersistedFortune : IDisposable
    {
        private readonly Microsoft.Data.Sqlite.SqliteConnection connection;
        public string ConnectionString => connection.ConnectionString;

        public AuthDatabase Database { get; } = new();
        public IWorldSession Session { get; }
        public RecordingDispatchProxy<IWorldSession> Messages { get; }
        public RecordingDispatchProxy<IAccount> Owner { get; }
        private Action pendingPlayerSave;

        public PersistedFortune(ulong coins = 10, ulong? bundleTarget = null, bool unclaimableFirst = false)
        {
            string name = $"fortune-{Guid.NewGuid():N}";
            connection = new($"Data Source={name};Mode=Memory;Cache=Shared;Pooling=False");
            connection.Open();
            Database.Initialise(new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = connection.ConnectionString });
            using AuthContext context = Database.CreateContext();
            context.Database.EnsureCreated();
            var model = new AccountModel { Id = 42u, Email = "fortune@example.test" };
            if (coins != 0)
                model.AccountCurrency.Add(new AccountCurrencyModel { Id = model.Id, CurrencyId = (byte)AccountCurrencyType.FortuneCoin, Amount = coins });
            if (unclaimableFirst)
                model.AccountInventory.Add(new AccountInventoryModel { Id = model.Id, InventoryId = 1, AccountItemId = 902 });
            if (bundleTarget.HasValue)
                model.AccountInventory.Add(new AccountInventoryModel { Id = model.Id, InventoryId = unclaimableFirst ? 2ul : 1ul, AccountItemId = 901,
                    TargetCharacterId = bundleTarget.Value, TargetRealmId = TestRealmId, HasTargetPlayerIdentity = true });
            context.Account.Add(model);
            context.SaveChanges();

            Session = RecordingDispatchProxy<IWorldSession>.Create(out var messages);
            Messages = messages;
            IAccount account = RecordingDispatchProxy<IAccount>.Create(out var owner);
            Owner = owner;
            IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var character);
            character.SetProperty(nameof(IPlayer.CharacterId), 9001ul);
            character.SetProperty(nameof(IPlayer.Account), account);
            character.SetProperty(nameof(IPlayer.Identity), new Identity { RealmId = TestRealmId, Id = 9001ul });
            character.SetProperty(nameof(IPlayer.AchievementManager), RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _));
            character.SetMethodHandler(nameof(IPlayer.Save), args =>
            {
                pendingPlayerSave = (Action)args[0];
                return null;
            });
            owner.SetProperty(nameof(IAccount.Id), model.Id);
            owner.SetProperty(nameof(IAccount.Session), Session);
            GameTableManager tables = CreateGameTableManager(new AccountItemEntry { Id = 11 }, new AccountItemEntry { Id = 22 },
                new AccountItemEntry { Id = 33 },
                new AccountItemEntry { Id = 901, AccountCurrencyEnum = (uint)AccountCurrencyType.FortuneCoin, AccountCurrencyAmount = 5 },
                new AccountItemEntry { Id = 902, AccountCurrencyEnum = (uint)AccountCurrencyType.FortuneCoin, AccountCurrencyAmount = 5, Item2Id = 1 });
            var wallet = new AccountCurrencyManager(account, model, tables);
            var inventory = new AccountInventoryManager(account, model, null, gameTableManager: tables);
            owner.SetProperty(nameof(IAccount.CurrencyManager), wallet);
            owner.SetProperty(nameof(IAccount.InventoryManager), inventory);
            owner.SetMethodHandler(nameof(IAccount.Save), args =>
            {
                var save = (AuthContext)args[0];
                wallet.Save(save);
                inventory.Save(save);
                return null;
            });
            messages.SetProperty(nameof(IWorldSession.Account), account);
            messages.SetProperty(nameof(IWorldSession.Player), player);
        }

        public FortuneSessionManager CreateManager()
        {
            IDatabaseManager databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var database);
            database.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), Database);
            IRealmContext realm = RecordingDispatchProxy<IRealmContext>.Create(out var realmProxy);
            realmProxy.SetProperty(nameof(IRealmContext.RealmId), TestRealmId);
            return new FortuneSessionManager(new StubFortuneRewardPool(), realm, databases);
        }

        public void CompletePlayerSave()
        {
            Action callback = pendingPlayerSave;
            Assert.NotNull(callback);
            pendingPlayerSave = null;
            using AuthContext context = Database.CreateContext();
            Session.Account.Save(context);
            context.SaveChanges();
            callback();
        }

        public void Dispose() => connection.Dispose();
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
