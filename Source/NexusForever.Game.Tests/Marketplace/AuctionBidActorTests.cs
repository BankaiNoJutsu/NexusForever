using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Collections;
using System.Reflection;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Option;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Tests.Challenges;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Option;
using static NexusForever.Game.Marketplace.GlobalMarketplaceManager;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Marketplace;

public sealed class AuctionBidActorTests
{
    [Theory]
    [InlineData(false, false, false, false, false, false)] [InlineData(true, false, false, false, false, false)]
    [InlineData(false, true, false, false, false, false)] [InlineData(true, true, false, false, false, false)]
    [InlineData(false, false, true, false, false, false)] [InlineData(true, false, true, false, false, false)]
    [InlineData(false, false, true, true, false, false)] [InlineData(true, false, true, true, false, false)]
    [InlineData(false, false, false, false, true, false)] [InlineData(true, false, false, false, true, false)]
    [InlineData(true, true, false, false, true, false)] [InlineData(true, false, false, true, true, false)]
    [InlineData(true, false, false, false, false, true)]
    public async Task RealPlayerPreflightAndReconciliationPreserveTheOrdinaryGraphAndLaterCurrency(bool commit, bool retire, bool loseReply, bool laterDuringSave, bool rollbackOnce, bool failFactory)
        => await RunReconciliation(commit, retire, loseReply, laterDuringSave, rollbackOnce, failFactory);

    [Theory]
    [InlineData(false, "before")] [InlineData(true, "before")]
    [InlineData(false, "after")] [InlineData(true, "after")]
    [InlineData(false, "flush")] [InlineData(true, "flush")]
    public async Task EnrollmentFailureRetainsTheOriginalFrozenGraphAndLaterIntent(bool commit, string stage)
        => await RunReconciliation(commit, false, false, false, false, false, failEnrollment: stage);

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task LegacyCreatesUpdatesAndDeletesSurviveAProvenReconciliationRollback(bool commit)
        => await RunReconciliation(commit, false, false, false, true, false, legacyRows: true);

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NewMaskedIntentOnTheOriginalActorWinsOverTheRetainedAttempt(bool commit)
        => await RunReconciliation(commit, false, false, false, true, false, newerMaskedIntent: true);

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PartialEnrollmentFollowedByCommitFailureRetainsAllEarlierWriteMasks(bool commit)
        => await RunReconciliation(commit, false, false, false, true, false, failEnrollment: "after", legacyRows: true);

    private static async Task RunReconciliation(bool commit, bool retire, bool loseReply, bool laterDuringSave,
        bool rollbackOnce, bool failFactory, string failEnrollment = null, bool legacyRows = false, bool newerMaskedIntent = false)
    {
        using var f = new Fixture(null); f.Seed();
        var tables = (RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables;
        tables.SetProperty(nameof(IGameTableManager.CurrencyType), PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1, CapAmount = 1000 }));
        ((RecordingDispatchProxy<IAccount>)(object)f.Source.Player.Account).SetProperty(nameof(IAccount.Id), 12u);
        AuctionRow auction; BidItem item;
        var database = new CharacterDatabase();
        using (var seed = f.Context())
        {
            seed.Character.Single(row => row.Id == 42).AccountId = 12;
            seed.Character.Add(new() { Id = 100, AccountId = 13, Name = "Bid seller", OriginalName = "Bid seller" });
            seed.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 });
            if (legacyRows)
            {
                seed.CharacterKeybinding.AddRange(new CharacterKeybindingModel { Id = 42, InputActionId = 1, Code00 = 10 },
                    new CharacterKeybindingModel { Id = 42, InputActionId = 2, Code00 = 10 });
                seed.CharacterTradeskill.Add(new() { Id = 42, TradeskillId = (uint)TradeskillType.Armorer, TradeskillXp = 50, IsActive = 1 });
                seed.CharacterSchematic.Add(new() { Id = 42, TradeskillSchematic2Id = 901 });
            }
            var escrow = new ItemModel { Id = 1000, ItemId = 7001, StackCount = 1, Location = (ushort)InventoryLocation.None };
            seed.Item.Add(escrow);
            var listing = new MarketplaceAuctionModel { Id = 1, OwnerCharacterId = 100, ItemId = 1000, Item2Id = 7001,
                Quantity = 1, MinimumBid = 1, ExpirationTime = (ulong)DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds() };
            seed.MarketplaceAuction.Add(listing); await seed.SaveChangesAsync();
            auction = AuctionRow.Capture(listing); item = BidItem.Capture(escrow);
            database.Initialise(new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = seed.Database.GetDbConnection().ConnectionString });
        }
        var characterDatabase = new ReconciliationDatabase(f, loseReply, rollbackOnce, failFactory, failEnrollment);
        using var actor = new RewardNormalSaveSnapshotTests.Actor(f, characterDatabase: characterDatabase);
        if (newerMaskedIntent)
            characterDatabase.AfterRollback = () => actor.Events.EnqueueThreadSafe(new NexusForever.Shared.Game.Events.PredicateEvent(() => true,
                () => actor.Player.InputKeySet = (InputSets)2));
        if (legacyRows)
        {
            typeof(Player).GetField("gameTableManager", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(actor.Player, f.Source.Tables);
            tables.SetProperty(nameof(IGameTableManager.TradeskillTier), PlayerBindPointTests.Table<TradeskillTierEntry>());
            tables.SetProperty(nameof(IGameTableManager.TradeskillSchematic2), PlayerBindPointTests.Table(
                new TradeskillSchematic2Entry { Id = 901, TradeSkillId = (uint)TradeskillType.Armorer },
                new TradeskillSchematic2Entry { Id = 902, TradeSkillId = (uint)TradeskillType.Armorer }));
            using var load = f.Context();
            var bindings = new CharacterModel { Id = 42 };
            foreach (var binding in load.CharacterKeybinding.AsNoTracking()) bindings.Keybinding.Add(binding);
            actor.Set(nameof(Player.KeybindingManager), new CharacterKeybindingManager(actor.Player, bindings));
            AddState("TradeskillState", "tradeskills", TradeskillType.Armorer, load.CharacterTradeskill.AsNoTracking().Single());
            AddState("SchematicState", "schematics", 901u, load.CharacterSchematic.AsNoTracking().Single());
            void AddState(string type, string field, object key, object model)
            {
                var state = typeof(Player).GetNestedType(type, BindingFlags.NonPublic)!
                    .GetMethod("FromModel", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, [model]);
                ((IDictionary)typeof(Player).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(actor.Player)!)[key] = state;
            }
        }
        var manager = new CurrencyManager(f.Source.Tables);
        manager.Initialise(actor.Player, Assert.Single(await database.GetCharacters(12)));
        actor.Set(nameof(Player.CurrencyManager), manager); await actor.Load();
        int authSaves = 0;
        actor.OnAuth = () =>
        {
            if (++authSaves == 2 && laterDuringSave) manager.CurrencyAddAmount(CurrencyType.Credits, 3);
        };
        actor.Quest.Single().Progress = 2; manager.CurrencyAddAmount(CurrencyType.Credits, 20);
        Player.AuctionBidActor prepared = null; int failed = 0;
        actor.Player.PrepareAuctionBid(new object(), value => { prepared = value; value.Capture(10, true); }, () => failed++);
        await actor.Drain(); Assert.NotNull(prepared); Assert.Equal(0, failed);
        using (var preflight = f.Context())
        {
            Assert.Equal(120ul, Assert.Single(preflight.CharacterCurrency).Amount);
            Assert.Equal(2u, Assert.Single(preflight.CharacterQuestObjective).Progress);
        }
        Assert.False(actor.Player.Session.CanProcessIncomingPackets);
        using (var forbidden = f.Context()) Assert.Throws<InvalidOperationException>(() => actor.Player.Save(forbidden));
        manager.CurrencyAddAmount(CurrencyType.Credits, 5); actor.Quest.Single().Progress = 3;
        actor.Player.InputKeySet = (InputSets)1;
        if (legacyRows)
        {
            actor.Player.KeybindingManager.Update(new BiInputKeySet { CharacterId = 42,
                Bindings = [new() { InputActionId = 2, Code00 = 20 }, new() { InputActionId = 3, Code00 = 30 }] });
            Assert.True(actor.Player.LearnTradeskill(TradeskillType.Weaponsmith, TradeskillType.Armorer));
            Assert.False(actor.Player.LearnSchematic(901, true));
            Assert.True(actor.Player.LearnSchematic(902, true));
        }
        Assert.Equal(120ul, Assert.Single(manager).Amount);
        if (retire)
        {
            actor.Cleanup(); Assert.True(prepared.Retired);
            Assert.Empty(actor.CleanupProxy.GetInvocations(nameof(ICleanupManager.AddPlayer)));
        }
        if (!commit)
        {
            using var trigger = f.Context();
            trigger.Database.ExecuteSqlRaw("CREATE TRIGGER reject_bid BEFORE UPDATE ON marketplace_auction BEGIN SELECT RAISE(ABORT,'bid rollback'); END;");
        }
        var command = new AuctionSettlementCommand(database.CreateContext, prepared.Database, Guid.NewGuid().ToString("N"), DateTime.UtcNow,
            auction, 42, 10, item, [new(42, 12), new(100, 13)], [prepared.Wallet.Snapshot]);
        var outcome = await Task.Run(command.ExecuteAsync); Assert.Equal(new BidOutcome(commit, true), outcome);
        bool completed = false;
        prepared.Reconcile(outcome.Committed, command.ReceiptId, command.PayloadSHA256, () => completed = true, () => failed++);
        await actor.Drain();
        using var final = f.Context();
        ulong expected = (commit ? 115ul : 125ul) + (laterDuringSave ? 3ul : 0ul);
        Assert.Equal(expected, Assert.Single(final.CharacterCurrency).Amount);
        Assert.Equal(3u, Assert.Single(final.CharacterQuestObjective).Progress);
        Assert.Equal(newerMaskedIntent ? (sbyte)2 : (sbyte)1, final.Character.Single(row => row.Id == 42).InputKeySet);
        if (legacyRows)
        {
            Assert.Equal(new ushort[] { 2, 3 }, final.CharacterKeybinding.OrderBy(row => row.InputActionId).Select(row => row.InputActionId).ToArray());
            Assert.Equal(new uint[] { 20, 30 }, final.CharacterKeybinding.OrderBy(row => row.InputActionId).Select(row => row.Code00).ToArray());
            Assert.Equal(0u, final.CharacterTradeskill.Single(row => row.TradeskillId == (uint)TradeskillType.Armorer).IsActive);
            Assert.Equal(50u, final.CharacterTradeskill.Single(row => row.TradeskillId == (uint)TradeskillType.Armorer).TradeskillXp);
            Assert.Equal(1u, final.CharacterTradeskill.Single(row => row.TradeskillId == (uint)TradeskillType.Weaponsmith).IsActive);
            Assert.Equal(2, final.CharacterSchematic.Count()); Assert.All(final.CharacterSchematic, row => Assert.True(row.Discovered));
        }
        Assert.Equal(commit ? 10ul : 0ul, Assert.Single(final.MarketplaceAuction).CurrentBid);
        Assert.Equal((commit ? 2 : 1) + (laterDuringSave ? 1 : 0), final.MarketplaceBidReceipt.Count());
        Assert.True(completed); Assert.Equal(0, failed);
        Assert.Equal(1 + (laterDuringSave ? 1 : 0) + (rollbackOnce ? 1 : 0) + (failEnrollment != null ? 1 : 0), characterDatabase.Enrollments);
        Assert.Equal(2 + (laterDuringSave ? 1 : 0) + (retire ? 1 : 0), authSaves);
        if (failFactory) Assert.Equal(2, characterDatabase.FactoryCalls);
        Assert.Equal(loseReply ? 1 : 0, characterDatabase.RepliesLost);
        Assert.Equal(rollbackOnce ? 1 : 0, characterDatabase.Rollbacks);
        Assert.Equal(2 + (laterDuringSave ? 1 : 0), ((RecordingDispatchProxy<ICharacterAchievementManager>)(object)actor.Player.AchievementManager)
            .GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)).Count);
        if (!retire)
        {
            var rewards = f.Source.SessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(call => call.Arguments[0]).OfType<ServerCombatReward>().ToArray();
            Assert.Equal(expected, rewards.Last().NewValue);
        }
        Assert.False(manager.HasPendingContractCurrency); Assert.False(manager.RequiresContractCurrencyReload);
        if (retire) Assert.Single(actor.CleanupProxy.GetInvocations(nameof(ICleanupManager.RemovePlayer)));
    }

    private sealed class ReconciliationDatabase(Fixture fixture, bool loseReply, bool rollbackOnce, bool failFactory, string failEnrollment) : CharacterDatabase
    {
        private readonly LoseReply fault = new();
        private readonly RollbackOnce rollback = new();
        internal int Enrollments;
        internal int FactoryCalls;
        internal int RepliesLost => fault.Lost;
        internal int Rollbacks => rollback.Failed;
        internal Action AfterRollback { set => rollback.AfterFailure = value; }
        public override Task SaveBidReconciliation(ulong characterId, Action<CharacterContext> action, MarketplaceBidReconciliation receipt)
            => CharacterAtomicSave.ExecuteBidReconciliation(Create, characterId,
                context =>
                {
                    Enrollments++;
                    if (failEnrollment == "before" && Enrollments == 1) throw new IOException("Enrollment failed before manager staging.");
                    action(context);
                    if (failEnrollment == "flush" && Enrollments == 1) context.SaveChanges(); // Still forbidden before sealing.
                    if (failEnrollment == "after" && Enrollments == 1) throw new IOException("Enrollment failed before the complete graph was sealed.");
                }, receipt);
        private CharacterContext Create()
        {
            if (Interlocked.Increment(ref FactoryCalls) == 1 && failFactory) throw new IOException("Temporary context factory failure before actor enrollment.");
            return rollbackOnce ? fixture.Context(rollback) : loseReply ? fixture.Context(fault) : fixture.Context();
        }
    }

    private sealed class RollbackOnce : DbTransactionInterceptor
    {
        internal int Failed;
        internal Action AfterFailure;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (Interlocked.CompareExchange(ref Failed, 1, 0) == 0)
            { AfterFailure?.Invoke(); throw new IOException("Bid ordinary graph failed before commit."); }
            return new(result);
        }
    }

    private sealed class LoseReply : DbTransactionInterceptor
    {
        internal int Lost;
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
        {
            if (Interlocked.CompareExchange(ref Lost, 1, 0) == 0)
                return Task.FromException(new IOException("Bid ordinary graph committed; acknowledgement lost."));
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void AdmissionLeaseSurvivesCleanupAndCannotReleaseAnotherOperation()
    {
        var cleanup = new CleanupManager();
        var account = RecordingDispatchProxy<IAccount>.Create(out var ap); ap.SetProperty(nameof(IAccount.Id), 12u);
        var original = RecordingDispatchProxy<IPlayer>.Create(out var pp);
        pp.SetProperty(nameof(IPlayer.Account), account); pp.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        Assert.True(cleanup.TryAcquireAccounts([12, 13, 12], out var first));
        Assert.True(cleanup.IsAccountLocked(account)); Assert.False(cleanup.TryAcquireAccounts([13, 14], out _));
        cleanup.AddPlayer(original); first.Dispose(); Assert.True(cleanup.IsAccountLocked(account));
        cleanup.RemovePlayer(original); Assert.False(cleanup.IsAccountLocked(account));
        Assert.True(cleanup.TryAcquireAccounts([12], out var second)); first.Dispose(); Assert.True(cleanup.IsAccountLocked(account));
        second.Dispose(); Assert.False(cleanup.IsAccountLocked(account));
    }
}
