using System.Reflection;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Mail;
using NexusForever.Game.Tests.Challenges;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Entity;

public sealed class BankUnreservedCurrencyRecoveryTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public BankUnreservedCurrencyRecoveryTests() { }
    internal BankUnreservedCurrencyRecoveryTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Theory]
    [InlineData("move")]
    [InlineData("attachment")]
    [InlineData("other-wallet")]
    [InlineData("split")] [InlineData("use")] [InlineData("delete")]
    [InlineData("split", "bank-rollback")] [InlineData("use", "ordinary-lost")]
    [InlineData("other-wallet", "publish-reserved")]
    [InlineData("move", "publish-add")] [InlineData("other-wallet", "publish-direct")]
    [InlineData("move", "publish-new")] [InlineData("move", "publish-subtract")]
    [InlineData("attachment", "failure-publish-add")] [InlineData("move", "terminal-publish-new")]
    [InlineData("new-wallet")]
    [InlineData("move", "ordinary-rollback")] [InlineData("attachment", "ordinary-lost")]
    [InlineData("other-wallet", "ordinary-rollback")] [InlineData("new-wallet", "ordinary-lost")]
    [InlineData("move", "bank-rollback")] [InlineData("attachment", "before-bank")] [InlineData("attachment", "bank-lost")]
    [InlineData("move", "retire")] [InlineData("attachment", "replace")]
    [InlineData("move", "sql-gap")]
    public async Task PublicBankFlowPersistsUnreservedCurrencyAcceptedDuringAuthBeforeEnding(string operation, string phase = "commit")
    {
        using var f = new Fixture(options); f.Seed();
        var account = (RecordingDispatchProxy<IAccount>)(object)f.Source.Player.Account;
        account.SetProperty(nameof(IAccount.Id), 12u);
        // This explicit entitled caller exercises bank access; no persistent or
        // default entitlement is granted by the fixture or production change.
        var entitlement = RecordingDispatchProxy<IAccountEntitlement>.Create(out var unlock);
        unlock.SetProperty(nameof(IAccountEntitlement.Amount), 1u);
        var entitlements = RecordingDispatchProxy<IAccountEntitlementManager>.Create(out var permissions);
        permissions.SetMethodHandler(nameof(IAccountEntitlementManager.GetEntitlement), args =>
            (EntitlementType)args[0] == EntitlementType.SharedRealmBankUnlock ? entitlement : null);
        account.SetProperty(nameof(IAccount.EntitlementManager), entitlements);
        var tables = (RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables;
        tables.SetProperty(nameof(IGameTableManager.CurrencyType), PlayerBindPointTests.Table(
            new CurrencyTypeEntry { Id = 1, CapAmount = 1000 }, new CurrencyTypeEntry { Id = 2, CapAmount = 1000 }));
        tables.SetProperty(nameof(IGameTableManager.GameFormula), PlayerBindPointTests.Table(new GameFormulaEntry { Id = 237, Datafloat0 = 5 }));
        var database = new CharacterDatabase();
        using (var seed = f.Context())
        {
            string connection = seed.Database.GetDbConnection().ConnectionString;
            seed.Character.Single().AccountId = 12;
            seed.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 });
            if (operation == "other-wallet") seed.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 2, Amount = 40 });
            seed.RealmBankItem.Add(new() { Id = 900, AccountId = 12, RealmId = 1, ItemId = 7001, BagIndex = 3, StackCount = 17,
                Charges = 2, Durability = .75f, ExpirationTimeLeft = 1200, MicrochipIds = "91,92", RuneSlots = "10:9001,7:0" });
            if (operation == "attachment")
            {
                seed.CharacterMail.Add(new() { Id = 600, RecipientId = 42, SenderId = 200, SenderType = (byte)SenderType.Player,
                    Subject = "Currency-free attachment", Message = "Later intent", CreateTime = DateTime.UtcNow.AddMinutes(-1) });
                seed.Item.Add(new() { Id = 904, ItemId = 7001, Location = (ushort)InventoryLocation.None, StackCount = 2,
                    Charges = 2, Durability = .75f, ExpirationTimeLeft = 1200, MicrochipIds = "91,92", RuneSlots = "10:9001,7:0" });
                seed.CharacterMailAttachment.Add(new() { Id = 600, Index = 3, ItemGuid = 904 });
            }
            await seed.SaveChangesAsync();
            database.Initialise(new DatabaseConnectionString { Provider = seed.Database.IsSqlite() ? DatabaseProvider.Sqlite : DatabaseProvider.MySql, ConnectionString = connection });
        }
        var info = RecordingDispatchProxy<IItemInfo>.Create(out var ip);
        ip.SetProperty(nameof(IItemInfo.Id), 7001u); ip.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 7001, MaxStackCount = 20, MaxCharges = 3 });
        var items = RecordingDispatchProxy<IItemManager>.Create(out var itemProxy); itemProxy.SetMethodReturn(nameof(IItemManager.GetItemInfo), info);
        itemProxy.SetProperty(nameof(IItemManager.NextItemId), 905ul);
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var dp); dp.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var bankFault = new CommitFault(phase == "bank-lost");
        var bank = new RealmBankManager(databases, items, async command =>
        {
            entered.SetResult(); await release.Task;
            if (phase is "before-bank" or "failure-publish-add") throw new IOException("Owned failure before currency-free bank enrollment.");
            if (phase is "bank-rollback" or "bank-lost")
                typeof(RealmBankManager.BankSaveCommand).GetField("contexts", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(command, (Func<CharacterContext>)(() => f.Context(bankFault)));
            await command.ExecuteAsync();
            if (phase == "terminal-publish-new")
            {
                using var foreign = f.Context(); foreign.Item.Where(row => row.Id == 900).ExecuteUpdate(set => set.SetProperty(row => row.Durability, .9f));
            }
        });
        var events = new EventQueue(); var session = RecordingDispatchProxy<IWorldSession>.Create(out var sp);
        sp.SetProperty(nameof(IWorldSession.Events), events); account.SetProperty(nameof(IAccount.Session), session);
        var model = Assert.Single(await database.GetCharacters(12));
        var ordinary = new OrdinaryDatabase(f, phase);
        using var actor = new RewardNormalSaveSnapshotTests.Actor(f, actorModel: model, actorSession: session, actorEvents: events,
            characterDatabase: ordinary, inventoryFactory: (owner, _) => BankDeferredCurrencyRecoveryTests.Inventory(owner, items, f.Source.Tables, bank));
        sp.SetProperty(nameof(IWorldSession.Player), actor.Player);
        var currency = new CurrencyManager(f.Source.Tables); currency.Initialise(actor.Player, model); actor.Set(nameof(Player.CurrencyManager), currency);
        await actor.Load(); bank.EnsureLoaded(actor.Player);
        var inventory = Assert.IsType<Inventory>(actor.Player.Inventory);
        Exception publicationReentry = null; bool attemptedReentry = false;
        var publishedCredits = new List<ulong>(); var publishedRenown = new List<ulong>();
        sp.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerItemMove or ServerMailTakeAttachment or ServerBuybackItemUpdated or ServerItemError or ServerItemAdd or ServerItemStackCountUpdate)
            {
                using var durable = f.Context();
                publishedCredits.Add(durable.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
                if (phase.Contains("publish", StringComparison.Ordinal) && !attemptedReentry)
                {
                    attemptedReentry = true;
                    publicationReentry = Record.Exception(() =>
                    {
                        if (phase.EndsWith("direct", StringComparison.Ordinal)) currency.Single(row => row.Id == CurrencyType.Renown).Amount += 7;
                        else if (phase.EndsWith("subtract", StringComparison.Ordinal)) currency.CurrencySubtractAmount(CurrencyType.Credits, 7);
                        else currency.CurrencyAddAmount(phase.EndsWith("new", StringComparison.Ordinal) ? CurrencyType.Renown : CurrencyType.Credits, 7);
                    });
                }
                if (operation is "other-wallet" or "new-wallet") publishedRenown.Add(durable.CharacterCurrency.Single(row => row.CurrencyId == 2).Amount);
            }
            return null;
        });
        if (operation == "attachment")
        {
            var mailbox = RecordingDispatchProxy<IMailboxEntity>.Create(out _);
            typeof(GridEntity).GetField("visibleEntities", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(actor.Player,
                new Dictionary<uint, IGridEntity> { [1] = mailbox });
            var manager = new MailManager(actor.Player, model, realmId: 1, itemManager: items, gameTableManager: f.Source.Tables, deliveryContexts: () => f.Context());
            actor.Set(nameof(Player.MailManager), manager); manager.MailTakeAttachment(600, 0, 1);
        }
        else if (operation == "split") inventory.ItemSplit(900, new ItemLocation { Location = InventoryLocation.Inventory, BagIndex = 0 }, 3);
        else if (operation == "use") Assert.True(inventory.ItemUse(inventory.GetItem(900ul)));
        else if (operation == "delete") Assert.NotNull(inventory.ItemDelete(new ItemLocation { Location = InventoryLocation.RealmBank, BagIndex = 3 }, 3, ItemUpdateReason.NoReason));
        else if (operation == "move") inventory.ItemMove(inventory.GetItem(900ul), InventoryLocation.Inventory, 0);
        else Assert.True(inventory.SellRealmBankItem(new ItemLocation { Location = InventoryLocation.RealmBank, BagIndex = 3 }, inventory.GetItem(900ul), 3,
            new[] { (CurrencyType.Credits, 21ul) }, new BuybackManager()));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
        currency.CurrencyAddAmount(CurrencyType.Credits, 5);
        CurrencyType laterType = operation is "other-wallet" or "new-wallet" ? CurrencyType.Renown : CurrencyType.Credits;
        int replacementIndex = 0;
        actor.OnAuth = () =>
        {
            actor.OnAuth = null;
            if (phase != "sql-gap") currency.CurrencyAddAmount(laterType, 7);
            if (phase == "retire") actor.Cleanup();
            if (phase == "replace")
            {
                replacementIndex = sp.Invocations.Count;
                sp.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
            }
        };
        release.SetResult();
        var drain = actor.Drain();
        if (phase == "sql-gap")
        {
            try { await ordinary.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15)); currency.CurrencyAddAmount(laterType, 7); }
            finally { ordinary.Release.TrySetResult(); }
        }
        try { await drain; } catch (Exception error) { throw new InvalidOperationException("Unreserved bank drain failed. " + ordinary.LastFailure, error); }
        using var read = f.Context();
        if (phase.Contains("publish", StringComparison.Ordinal))
        {
            Assert.True(attemptedReentry); Assert.IsType<InvalidOperationException>(publicationReentry);
            if (phase.EndsWith("new", StringComparison.Ordinal)) Assert.DoesNotContain(currency, row => row.Id == CurrencyType.Renown);
        }
        if (phase == "terminal-publish-new")
        {
            Assert.True(inventory.RequiresAuthoritativeReload); Assert.Empty(read.BankReconciliationReceipt);
            Assert.Equal(100ul, read.CharacterCurrency.Single().Amount); Assert.Equal(.9f, read.Item.Single().Durability); return;
        }
        ulong expectedCredits = operation is "other-wallet" or "new-wallet" ? 126ul : 112ul;
        Assert.Equal(expectedCredits, read.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
        if (operation == "other-wallet") Assert.Equal(47ul, read.CharacterCurrency.Single(row => row.CurrencyId == 2).Amount);
        if (operation == "new-wallet") Assert.Equal(7ul, read.CharacterCurrency.Single(row => row.CurrencyId == 2).Amount);
        Assert.Equal(operation is "other-wallet" or "new-wallet" ? 2 : 1, read.CharacterCurrency.Count());
        Assert.Equal(2, read.BankReconciliationReceipt.Count());
        if (phase is "retire" or "replace") Assert.Empty(publishedCredits);
        else
        {
            Assert.NotEmpty(publishedCredits); Assert.All(publishedCredits, amount => Assert.Equal(expectedCredits, amount));
            Assert.All(publishedRenown, amount => Assert.Equal(operation == "new-wallet" ? 7ul : 47ul, amount));
        }
        if (phase == "replace")
        {
            Assert.DoesNotContain(sp.Invocations.Skip(replacementIndex), call => call.MethodName == nameof(IWorldSession.ForceDisconnect));
            Assert.DoesNotContain(sp.Invocations.Skip(replacementIndex), call => call.MethodName == "set_CanProcessIncomingPackets" && Equals(call.Arguments[0], true));
        }
        Assert.All(currency.Cast<Currency>(), row => Assert.False(row.NeedsPersistenceSave));
        Assert.Equal(1u, read.CharacterQuestObjective.Single().Progress); // no quest edit requests an incidental extra save
        Assert.Empty(read.CharacterEntitlement); Assert.False(inventory.RequiresAuthoritativeReload);
        if (operation == "move")
        {
            if (phase == "bank-rollback") { Assert.Single(read.RealmBankItem); Assert.Empty(read.Item); }
            else { Assert.Empty(read.RealmBankItem); Assert.Equal(42ul, read.Item.Single(row => row.Id == 900).OwnerId); }
        }
        if (operation == "attachment")
        {
            if (phase is "before-bank" or "failure-publish-add") { Assert.Single(read.CharacterMailAttachment); Assert.Null(read.Item.Single(row => row.Id == 904).OwnerId); }
            else { Assert.Empty(read.CharacterMailAttachment); Assert.Equal(42ul, read.Item.Single(row => row.Id == 904).OwnerId); }
        }
        if (operation == "other-wallet") Assert.Equal(14u, read.RealmBankItem.Single().StackCount);
        if (operation == "split")
        {
            Assert.Equal(phase == "bank-rollback" ? 17u : 14u, read.RealmBankItem.Single().StackCount);
            if (phase == "bank-rollback") Assert.Empty(read.Item);
            else { var split = Assert.Single(read.Item); Assert.Equal(905ul, split.Id); Assert.Equal(3u, split.StackCount); Assert.Equal(42ul, split.OwnerId); }
        }
        if (operation == "use")
        {
            Assert.Equal(16u, read.RealmBankItem.Single().StackCount);
            var achievements = (RecordingDispatchProxy<ICharacterAchievementManager>)(object)actor.Player.AchievementManager;
            Assert.Single(achievements.Invocations, call => call.MethodName == nameof(ICharacterAchievementManager.CheckAchievements)
                && call.Arguments[1] is AchievementType.ItemConsume);
        }
        if (operation == "delete") Assert.Equal(14u, read.RealmBankItem.Single().StackCount);
        if (operation != "move" || phase == "bank-rollback")
        {
            var original = Assert.Single(read.RealmBankItem);
            Assert.Equal(2u, original.Charges); Assert.Equal(.75f, original.Durability); Assert.Equal(1200u, original.ExpirationTimeLeft);
        }
    }
    private sealed class OrdinaryDatabase(Fixture f, string phase) : CharacterDatabase
    {
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal string LastFailure;
        private readonly CommitFault fault = new(phase == "ordinary-lost");
        private HoldCommit hold;
        public override async Task SaveBankReconciliation(ulong character, Action<CharacterContext> action, BankSaveReconciliation receipt)
        {
            try { await CharacterAtomicSave.ExecuteBankReconciliation(() => phase == "sql-gap" ? f.Context(hold ??= new(Entered, Release))
                : phase.StartsWith("ordinary-", StringComparison.Ordinal) ? f.Context(fault) : f.Context(), character, action, receipt); }
            catch (Exception error) { LastFailure = error.ToString(); throw; }
        }
    }
    private sealed class CommitFault(bool after) : DbTransactionInterceptor
    {
        private int fired;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (!after && Interlocked.Exchange(ref fired, 1) == 0) throw new IOException("Owned unreserved currency ordinary rollback.");
            return new(result);
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => after && Interlocked.Exchange(ref fired, 1) == 0 ? Task.FromException(new IOException("Owned unreserved currency lost acknowledgement.")) : Task.CompletedTask;
    }
    private sealed class HoldCommit(TaskCompletionSource entered, TaskCompletionSource release) : DbTransactionInterceptor
    {
        private int fired;
        public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref fired, 1) == 0) { entered.TrySetResult(); await release.Task; }
            return result;
        }
    }

}
