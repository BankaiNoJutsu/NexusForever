using System.Reflection;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Mail;
using NexusForever.Game.Static.Mail;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Static;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.Challenges;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Vendor;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Entity;

public sealed class BankDeferredCurrencyRecoveryTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public BankDeferredCurrencyRecoveryTests() { }
    internal BankDeferredCurrencyRecoveryTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Theory]
    [InlineData("commit")]
    [InlineData("lost")]
    [InlineData("rollback")]
    [InlineData("replacement")]
    [InlineData("unsettled-split")]
    public async Task VendorBuybackReconcilesPaymentAndItemBeforeAcknowledgingTheOriginalOffer(string outcome)
    {
        bool split = outcome == "unsettled-split";
        using var f = new Fixture(options);
        f.Seed();
        var database = new CharacterDatabase();
        using (var seed = f.Context())
        {
            database.Initialise(new DatabaseConnectionString
            {
                Provider = seed.Database.IsSqlite() ? DatabaseProvider.Sqlite : DatabaseProvider.MySql,
                ConnectionString = seed.Database.GetDbConnection().ConnectionString
            });
            seed.Character.Single().AccountId = 12;
            seed.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 85 });
            seed.Item.Add(new() { Id = 904, OwnerId = 42, ItemId = 7001, Location = (ushort)InventoryLocation.Inventory,
                BagIndex = 0, StackCount = 2, Charges = 1, Durability = .75f, MicrochipIds = "91", RuneSlots = "10:9001" });
            await seed.SaveChangesAsync();
        }
        var account = (RecordingDispatchProxy<IAccount>)(object)f.Source.Player.Account;
        account.SetProperty(nameof(IAccount.Id), 12u);
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var network);
        var events = new EventQueue();
        network.SetProperty(nameof(IWorldSession.Events), events);
        account.SetProperty(nameof(IAccount.Session), session);
        ((RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables).SetProperty(nameof(IGameTableManager.CurrencyType),
            PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1, CapAmount = 1000 }));
        var info = RecordingDispatchProxy<IItemInfo>.Create(out var definition);
        definition.SetProperty(nameof(IItemInfo.Id), 7001u);
        definition.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 7001, MaxStackCount = 20, MaxCharges = 3 });
        var items = RecordingDispatchProxy<IItemManager>.Create(out var itemManager);
        itemManager.SetMethodReturn(nameof(IItemManager.GetItemInfo), info);
        itemManager.SetProperty(nameof(IItemManager.NextItemId), 905ul);
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var dbs);
        dbs.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int attempts = 0;
        var bank = new RealmBankManager(databases, items, async command =>
        {
            int attempt = ++attempts;
            await release.Task;
            if (attempt == 1 && outcome is "lost" or "rollback")
            {
                var fault = new CommitFault(outcome == "lost");
                typeof(RealmBankManager.BankSaveCommand).GetField("contexts", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(command, (Func<CharacterContext>)(() => f.Context(fault)));
            }
            await command.ExecuteAsync();
        });
        var model = Assert.Single(await database.GetCharacters(12));
        var ordinary = new OrdinaryDatabase(f, outcome);
        using var actor = new RewardNormalSaveSnapshotTests.Actor(f, actorModel: model, actorSession: session, actorEvents: events,
            characterDatabase: ordinary, inventoryFactory: (owner, _) => Inventory(owner, items, f.Source.Tables, bank));
        network.SetProperty(nameof(IWorldSession.Player), actor.Player);
        var wallet = new CurrencyManager(f.Source.Tables);
        wallet.Initialise(actor.Player, model);
        actor.Set(nameof(Player.CurrencyManager), wallet);
        await actor.Load();
        var inventory = Assert.IsType<Inventory>(actor.Player.Inventory);
        var location = new ItemLocation { Location = InventoryLocation.Inventory, BagIndex = 0 };
        var source = new NexusForever.Game.Entity.Item(Assert.Single(model.Item), items, f.Source.Tables);
        inventory.LoadItem(source, location.Location, location.BagIndex);
        var sold = inventory.ItemDelete(location, split ? 1u : 2u, ItemUpdateReason.Vendor);
        Assert.NotNull(sold);
        wallet.CurrencyAddAmount(CurrencyType.Credits, 15);
        var buyback = new BuybackManager();
        buyback.AddItem(actor.Player, sold, sold.StackCount, [(CurrencyType.Credits, 15)]);
        var notices = new List<object>();
        network.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerBuybackItemRemoved or ServerCombatReward or ServerItemAdd or ServerItemError)
                notices.Add(args[0]);
            return null;
        });
        using var reader = new NexusForever.Network.GamePacketReader(new MemoryStream(new byte[4]));
        var request = new ClientBuybackItemFromVendor();
        request.Read(reader);
        var handler = new ClientBuybackItemFromVendorHandler(buyback);

        handler.HandleMessage(session, request);
        try
        {
            Assert.Equal(0, attempts);
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
            {
                while (attempts == 0)
                {
                    events.Update(.01);
                    await Task.Delay(5, timeout.Token);
                }
            }
            using (var predecessor = f.Context())
            {
                if (split) Assert.Equal(1u, Assert.Single(predecessor.Item).StackCount);
                else Assert.Empty(predecessor.Item);
                Assert.Equal(100ul, predecessor.CharacterCurrency.Single().Amount);
            }
            Assert.Equal(1, attempts);
            Assert.False(session.CanProcessIncomingPackets);
            Assert.Equal(100ul, Assert.Single(wallet).Amount);
            Assert.Empty(notices);
            handler.HandleMessage(session, request);
            Assert.Equal(1, attempts);
            if (outcome == "replacement")
                network.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
        }
        finally { release.TrySetResult(); }
        await actor.Drain();

        if (outcome == "rollback")
        {
            using var rolledBack = f.Context();
            Assert.Empty(rolledBack.Item);
            Assert.Equal(100ul, rolledBack.CharacterCurrency.Single().Amount);
            Assert.Same(sold, buyback.GetItem(actor.Player, 0)?.Item);
            Assert.Null(inventory.GetItem(sold.Guid));
            Assert.Single(notices.OfType<ServerItemError>());
            Assert.Empty(notices.OfType<ServerBuybackItemRemoved>());
            Assert.Empty(notices.OfType<ServerItemAdd>());
            notices.Clear();
            handler.HandleMessage(session, request);
            await actor.Drain();
        }

        using var read = f.Context();
        var restored = read.Item.Single(item => item.Id == sold.Guid);
        Assert.Equal(split ? 2 : 1, read.Item.Count());
        Assert.Equal(sold.Guid, restored.Id);
        Assert.Equal(split ? 1u : 2u, restored.StackCount);
        if (split) Assert.Equal(1u, read.Item.Single(item => item.Id == source.Guid).StackCount);
        else
        {
            Assert.Equal("91", restored.MicrochipIds);
            Assert.Equal("10:9001", restored.RuneSlots);
        }
        Assert.Equal(85ul, read.CharacterCurrency.Single().Amount);
        Assert.Null(buyback.GetItem(actor.Player, 0));
        if (outcome == "replacement")
        {
            Assert.Equal(1, attempts);
            Assert.Empty(notices);
            Assert.False(session.CanProcessIncomingPackets);
            Assert.Empty(network.GetInvocations(nameof(IWorldSession.ForceDisconnect)));
            return;
        }
        Assert.Equal(85ul, Assert.Single(wallet).Amount);
        Assert.Equal(outcome == "rollback" ? 2 : 1, attempts);
        Assert.Equal(85ul, Assert.Single(notices.OfType<ServerCombatReward>()).NewValue);
        Assert.Single(notices.OfType<ServerItemAdd>());
        Assert.Equal(0u, Assert.Single(notices.OfType<ServerBuybackItemRemoved>()).UniqueId);
    }

    [Theory]
    [InlineData("before-sql")] [InlineData("rollback")]
    [InlineData("commit")] [InlineData("lost")]
    [InlineData("ordinary-rollback")] [InlineData("ordinary-lost")] [InlineData("ordinary-flush")]
    [InlineData("later")] [InlineData("ordinary-later-rollback")]
    [InlineData("metadata")] [InlineData("metadata-rollback")]
    [InlineData("retire")] [InlineData("foreign")]
    [InlineData("probe-retry")] [InlineData("ordinary-foreign")] [InlineData("ordinary-foreign-replacement")]
    [InlineData("replacement")] [InlineData("packet-replacement")] [InlineData("packet-rollback")]
    [InlineData("metadata", "selection")] [InlineData("metadata-rollback", "selection")]
    [InlineData("before-sql", "cash")] [InlineData("rollback", "cash")] [InlineData("commit", "cash")] [InlineData("lost", "cash")]
    [InlineData("before-sql", "cod")] [InlineData("rollback", "cod")] [InlineData("commit", "cod")] [InlineData("lost", "cod")]
    [InlineData("before-sql", "selection")] [InlineData("rollback", "selection")] [InlineData("commit", "selection")] [InlineData("lost", "selection")]
    public async Task PublicBankOperationReconcilesItsCompleteOutcomeAndLaterOriginalActorIntent(string failure, string operation = "sale")
    {
        bool rollback = failure is "before-sql" or "rollback" or "metadata-rollback" or "packet-rollback";
        bool later = failure is "later" or "ordinary-later-rollback", metadata = failure.StartsWith("metadata", StringComparison.Ordinal);
        using var f = new Fixture(options); f.Seed();
        var account = (RecordingDispatchProxy<IAccount>)(object)f.Source.Player.Account; account.SetProperty(nameof(IAccount.Id), 12u);
        ((RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables).SetProperty(nameof(IGameTableManager.CurrencyType),
            PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1, CapAmount = 1000 }));
        bool mailOperation = operation != "sale";
        int recipientLookups = 0, replacementIndex = 0; bool? notifiedBeforeOrdinarySave = null;
        ((RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables).SetProperty(nameof(IGameTableManager.GameFormula),
            PlayerBindPointTests.Table(new GameFormulaEntry { Id = 237, Datafloat0 = 5 }));
        var database = new CharacterDatabase();
        using (var seed = f.Context())
        {
            string connection = seed.Database.GetDbConnection().ConnectionString;
            seed.Character.Single().AccountId = 12;
            seed.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 });
            seed.RealmBankItem.Add(new() { Id = 900, AccountId = 12, RealmId = 1, ItemId = 7001, BagIndex = 3, StackCount = 17,
                Charges = 2, Durability = .75f, ExpirationTimeLeft = 1200, MicrochipIds = "91,92", RuneSlots = "10:9001,7:0" });
            if (mailOperation)
            {
                seed.Character.Add(new() { Id = 200, AccountId = 13, Name = "Sender", OriginalName = "Sender", CreateTime = DateTime.UtcNow, PathActivatedTimestamp = DateTime.UtcNow });
                seed.CharacterMail.Add(new() { Id = 600, RecipientId = 42, SenderId = 200, SenderType = (byte)SenderType.Player,
                    Subject = "Bank reconciliation", Message = "Accepted later intent", CurrencyType = 1, CurrencyAmount = 21,
                    IsCashOnDelivery = Convert.ToByte(operation == "cod"), CreateTime = DateTime.UtcNow.AddMinutes(-1) });
                if (operation == "selection")
                {
                    seed.Item.Add(new() { Id = 904, ItemId = 7001, Location = (ushort)InventoryLocation.None, StackCount = 2,
                        Charges = 2, Durability = .75f, ExpirationTimeLeft = 1200, MicrochipIds = "91,92", RuneSlots = "10:9001,7:0" });
                    seed.CharacterMailAttachment.Add(new() { Id = 600, Index = 3, ItemGuid = 904 });
                }
            }
            await seed.SaveChangesAsync();
            database.Initialise(new DatabaseConnectionString { Provider = seed.Database.IsSqlite() ? DatabaseProvider.Sqlite : DatabaseProvider.MySql,
                ConnectionString = connection });
            if (failure is "rollback" or "metadata-rollback") seed.Database.ExecuteSqlRaw(seed.Database.IsSqlite()
                ? $"CREATE TRIGGER reject_bank_deferred BEFORE UPDATE ON {(mailOperation ? "character_currency" : "realm_bank_item")} BEGIN SELECT RAISE(ABORT, 'owned bank sale rollback'); END;"
                : $"CREATE TRIGGER reject_bank_deferred BEFORE UPDATE ON {(mailOperation ? "character_currency" : "realm_bank_item")} FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='owned bank sale rollback'");
        }
        var info = RecordingDispatchProxy<IItemInfo>.Create(out var ip); ip.SetProperty(nameof(IItemInfo.Id), 7001u);
        ip.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 7001, MaxStackCount = 20, MaxCharges = 3 });
        var items = RecordingDispatchProxy<IItemManager>.Create(out var itemProxy); itemProxy.SetMethodReturn(nameof(IItemManager.GetItemInfo), info);
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var dp); dp.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var bank = new RealmBankManager(databases, items, async command =>
        {
            entered.SetResult(); await release.Task;
            if (failure is "before-sql" or "packet-rollback") throw new IOException("Owned known failure before bank SQL.");
            var factory = typeof(RealmBankManager.BankSaveCommand).GetField("contexts", BindingFlags.Instance | BindingFlags.NonPublic)!;
            if (failure == "lost")
            {
                var fault = new CommitFault(true); factory.SetValue(command, (Func<CharacterContext>)(() => f.Context(fault)));
            }
            if (failure == "probe-retry")
            {
                var original = (Func<CharacterContext>)factory.GetValue(command)!; int calls = 0;
                factory.SetValue(command, (Func<CharacterContext>)(() => ++calls == 3 ? throw new IOException("Owned one-shot bank observation failure.") : original()));
            }
            try { await command.ExecuteAsync(); }
            finally
            {
                if (failure == "metadata-rollback" || mailOperation && failure == "rollback") { using var clear = f.Context(); clear.Database.ExecuteSqlRaw("DROP TRIGGER reject_bank_deferred"); }
            }
            if (failure == "foreign") { using var foreign = f.Context(); await foreign.RealmBankItem.ExecuteUpdateAsync(set => set.SetProperty(row => row.Durability, .9f)); }
        });
        var events = new EventQueue(); var session = RecordingDispatchProxy<IWorldSession>.Create(out var sp);
        sp.SetProperty(nameof(IWorldSession.Events), events); account.SetProperty(nameof(IAccount.Session), session);
        var ordinary = new OrdinaryDatabase(f, failure);
        using (var configure = f.Context()) ordinary.Initialise(new DatabaseConnectionString
        { Provider = configure.Database.IsSqlite() ? DatabaseProvider.Sqlite : DatabaseProvider.MySql, ConnectionString = configure.Database.GetDbConnection().ConnectionString });
        var model = Assert.Single(await database.GetCharacters(12));
        using var actor = new RewardNormalSaveSnapshotTests.Actor(f, actorModel: model, actorSession: session, actorEvents: events,
            characterDatabase: ordinary, inventoryFactory: (owner, loaded) => Inventory(owner, items, f.Source.Tables, bank));
        sp.SetProperty(nameof(IWorldSession.Player), actor.Player);
        var currency = new CurrencyManager(f.Source.Tables); currency.Initialise(actor.Player, model); actor.Set(nameof(Player.CurrencyManager), currency);
        await actor.Load(); bank.EnsureLoaded(actor.Player);
        var inventory = Assert.IsType<Inventory>(actor.Player.Inventory); var item = inventory.GetItem(900ul); var buyback = new BuybackManager();
        if (mailOperation)
        {
            var assets = RecordingDispatchProxy<IAssetManager>.Create(out var ap); ap.SetMethodReturnFactory("get_NextMailId", () => 701ul);
            var sender = RecordingDispatchProxy<ICharacter>.Create(out var senderProxy); senderProxy.SetProperty(nameof(ICharacter.CharacterId), 200ul);
            var characters = RecordingDispatchProxy<ICharacterManager>.Create(out var cp); cp.SetMethodReturn(nameof(ICharacterManager.GetCharacter), sender);
            var players = RecordingDispatchProxy<IPlayerManager>.Create(out var playersProxy);
            playersProxy.SetMethodReturnFactory(nameof(IPlayerManager.GetPlayer), () => { recipientLookups++; return null; });
            if (operation == "cod") actor.OnAuth = () => { actor.OnAuth = null; notifiedBeforeOrdinarySave = recipientLookups != 0; };
            var mailbox = RecordingDispatchProxy<IMailboxEntity>.Create(out _);
            typeof(GridEntity).GetField("visibleEntities", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(actor.Player,
                new Dictionary<uint, IGridEntity> { [1] = mailbox });
            var manager = new MailManager(actor.Player, model, assets, characters, 1, players, items, f.Source.Tables, deliveryContexts: () => f.Context());
            actor.Set(nameof(Player.MailManager), manager);
            if (operation == "cod") manager.MailPayCod(600);
            else if (operation == "selection") manager.MailTakeAllFromSelection([600], 1);
            else manager.MailTakeCash(600, 1);
        }
        else Assert.True(inventory.SellRealmBankItem(new ItemLocation { Location = InventoryLocation.RealmBank, BagIndex = 3 }, item, 3,
            new[] { (CurrencyType.Credits, 21ul) }, buyback));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
        currency.CurrencyAddAmount(CurrencyType.Credits, 5); actor.Quest.Single().Progress = 3;
        Assert.Equal(100ul, Assert.Single(currency).Amount);
        if (metadata)
        {
            item.Charges = 1; item.Durability = .5f;
            if (operation == "selection")
            {
                inventory.GetItem(904ul).Charges = 1;
            }
        }
        if (later) actor.OnAuth = () => { actor.OnAuth = null; currency.CurrencyAddAmount(CurrencyType.Credits, 7); actor.Quest.Single().Progress = 4; };
        if (failure is "ordinary-foreign" or "ordinary-foreign-replacement") actor.OnAuth = () =>
        {
            actor.OnAuth = null; using var foreign = f.Context(); foreign.RealmBankItem.ExecuteUpdate(set => set.SetProperty(row => row.Durability, .9f));
            if (failure == "ordinary-foreign-replacement")
            {
                replacementIndex = sp.Invocations.Count;
                sp.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
            }
        };
        if (failure == "retire") actor.Cleanup();
        if (failure == "replacement") sp.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
        if (failure.StartsWith("packet-", StringComparison.Ordinal)) sp.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerCombatReward) sp.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
            return null;
        });
        release.SetResult();
        try { await actor.Drain(); } catch (Exception error) { throw new InvalidOperationException("Bank drain failed. Last ordinary failure: " + ordinary.LastFailure, error); }
        using var stored = f.Context();
        if (failure is "foreign" or "ordinary-foreign" or "ordinary-foreign-replacement")
        {
            Assert.Equal(121ul, stored.CharacterCurrency.Single().Amount); Assert.Equal(.9f, stored.RealmBankItem.Single().Durability);
            Assert.True(inventory.RequiresAuthoritativeReload); Assert.Empty(stored.BankReconciliationReceipt);
            if (failure == "ordinary-foreign-replacement")
            {
                Assert.DoesNotContain(sp.Invocations.Skip(replacementIndex), call => call.MethodName == nameof(IWorldSession.ForceDisconnect));
                Assert.DoesNotContain(sp.Invocations.Skip(replacementIndex), call => call.MethodName == "set_CanProcessIncomingPackets" && Equals(call.Arguments[0], true));
            }
            return;
        }
        ulong expected = (rollback ? 100ul : operation == "cod" ? 79ul : 121ul) + 5ul + (later ? 7ul : 0ul);
        Assert.Equal(expected, stored.CharacterCurrency.Single().Amount);
        Assert.Equal(later ? 4u : 3u, stored.CharacterQuestObjective.Single().Progress); Assert.Equal(rollback || mailOperation ? 17u : 14u, stored.RealmBankItem.Single().StackCount);
        if (failure.StartsWith("packet-", StringComparison.Ordinal))
        {
            var packets = sp.Invocations.Where(call => call.MethodName == nameof(IWorldSession.EnqueueMessageEncrypted)).SelectMany(call => call.Arguments).ToArray();
            Assert.Single(packets.OfType<ServerCombatReward>()); Assert.Empty(packets.OfType<ServerItemError>());
            Assert.True(inventory.RequiresAuthoritativeReload); Assert.Null(buyback.GetItem(actor.Player, 0));
            Assert.Single(stored.BankReconciliationReceipt); return;
        }
        Assert.False(inventory.RequiresAuthoritativeReload);
        if (rollback || failure is "retire" or "replacement" || mailOperation) Assert.Null(buyback.GetItem(actor.Player, 0)); else Assert.Equal(3u, buyback.GetItem(actor.Player, 0).Quantity);
        Assert.Equal(expected, Assert.Single(currency).Amount); Assert.Empty(stored.CharacterEntitlement);
        if (metadata) { Assert.Equal((uint)1, stored.RealmBankItem.Single().Charges); Assert.Equal(.5f, stored.RealmBankItem.Single().Durability); }
        if (mailOperation)
        {
            Assert.Equal(rollback ? (byte)0 : (byte)1, stored.CharacterMail.Single(row => row.Id == 600).HasPaidOrCollectedCurrency);
            Assert.Equal(operation == "cod" && !rollback ? 2 : 1, stored.CharacterMail.Count());
            if (operation == "cod") { Assert.False(notifiedBeforeOrdinarySave); Assert.Equal(rollback ? 0 : 1, recipientLookups); }
            if (operation == "selection")
            {
                Assert.Equal(rollback ? 1 : 0, stored.CharacterMailAttachment.Count());
                if (metadata) Assert.Equal(1u, stored.Item.Single(row => row.Id == 904).Charges);
                Assert.Equal(rollback ? null : (ulong?)42, stored.Item.Single(row => row.Id == 904).OwnerId);
                Assert.Equal(rollback ? InventoryLocation.None : InventoryLocation.Inventory, (InventoryLocation)stored.Item.Single(row => row.Id == 904).Location);
            }
            var results = sp.Invocations.Where(call => call.MethodName == nameof(IWorldSession.EnqueueMessageEncrypted))
                .SelectMany(call => call.Arguments).OfType<ServerMailResult>().ToArray();
            Assert.Single(results); Assert.Equal(rollback ? GenericError.DbFailure : GenericError.Ok, results[0].Result);
        }
        // Updating quest progress during Auth requests its own ordinary save;
        // the bank then drains the later wallet intent before publication.
        Assert.Equal(later ? 3 : 1, stored.BankReconciliationReceipt.Count());
        if (failure != "retire") await actor.Player.SaveDirect();
    }

    private sealed class OrdinaryDatabase(Fixture fixture, string failure) : CharacterDatabase
    {
        internal string LastFailure;
        private readonly FlushFault flush = new();
        private readonly CommitFault fault = new(failure is "ordinary-lost");
        public override async Task SaveBankReconciliation(ulong character, Action<CharacterContext> action, BankSaveReconciliation receipt)
        {
            try { await CharacterAtomicSave.ExecuteBankReconciliation(() => failure == "ordinary-flush" ? fixture.Context(flush)
                : failure.StartsWith("ordinary-", StringComparison.Ordinal) ? fixture.Context(fault) : fixture.Context(), character, action, receipt); }
            catch (Exception error) { LastFailure = error.ToString(); throw; }
        }
    }
    private sealed class FlushFault : SaveChangesInterceptor
    {
        private int fired;
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => Interlocked.Exchange(ref fired, 1) == 0 ? throw new IOException("Owned ordinary failure before flush.") : new(result);
    }
    private sealed class CommitFault(bool after) : DbTransactionInterceptor
    {
        private int fired;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (!after && Interlocked.Exchange(ref fired, 1) == 0) throw new IOException("Owned bank ordinary save rollback.");
            return new(result);
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => after && Interlocked.Exchange(ref fired, 1) == 0 ? Task.FromException(new IOException("Owned bank acknowledgement loss.")) : Task.CompletedTask;
    }

    internal static Inventory Inventory(Player owner, IItemManager items, IGameTableManager tables, RealmBankManager bank)
    {
        var inventory = (Inventory)RuntimeHelpers.GetUninitializedObject(typeof(Inventory));
        void Set(string name, object value) => typeof(Inventory).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(inventory, value);
        Set("characterId", owner.CharacterId); Set("player", owner); Set("itemManager", items); Set("gameTableManager", tables); Set("realmBankManager", bank);
        Set("bags", new Dictionary<InventoryLocation, IBag> { [InventoryLocation.Inventory] = new Bag(InventoryLocation.Inventory, 20, items),
            [InventoryLocation.RealmBank] = new Bag(InventoryLocation.RealmBank, 20, items) });
        Set("deletedItems", new List<IItem>()); return inventory;
    }
}
