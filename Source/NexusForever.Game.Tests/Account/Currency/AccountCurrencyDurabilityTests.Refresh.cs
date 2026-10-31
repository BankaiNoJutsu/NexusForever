using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Message;
using NexusForever.Network.Packet;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Account.Currency;

public sealed partial class AccountCurrencyDurabilityTests
{
    [Fact]
    public async Task AuthoritativeRefreshMergesPendingAndLaterSemanticIntentWithoutWritingOrPublishingOnTheWorker()
    {
        using var f = new Fixture(providerOptions); f.Wallet.AddAmount(10);
        await ExternalWallet(f, 107);
        using var refresh = f.Wallet.CaptureRefresh(); await refresh.ReadAsync(() => f.Context());
        Assert.Equal(110ul, f.Wallet.Amount); Assert.Empty(f.SessionProxy.GetInvocations("EnqueueMessageEncrypted"));
        f.Wallet.SubtractAmount(4); f.Wallet.AddAmount(2);
        await using (var blocked = f.Context()) Assert.Throws<InvalidOperationException>(() => f.Wallet.Save(blocked));
        Assert.True(refresh.TryAdopt()); Assert.False(refresh.TryAdopt()); Assert.Equal(115ul, f.Wallet.Amount);
        using (var read = f.Context()) { Assert.Equal(107ul, read.AccountCurrency.Single().Amount); Assert.Empty(read.AccountCurrencyOperation); }
        refresh.Dispose(); await f.Save(); await f.Save();
        using var final = f.Context(); Assert.Equal(115ul, final.AccountCurrency.Single().Amount); Assert.Equal(3, final.AccountCurrencyOperation.Count());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RefreshObservesUnknownSaveReceiptsAndNeverReplaysAlreadyCommittedOperations(bool lostCommit)
    {
        using var f = new Fixture(providerOptions); f.Wallet.AddAmount(10);
        await using (var failed = f.Context(new CommitFailure(lostCommit)))
        { f.Wallet.Save(failed); await Assert.ThrowsAsync<InvalidOperationException>(() => failed.SaveChangesAsync()); }
        await ExternalWallet(f, lostCommit ? 117ul : 107ul);
        using var refresh = f.Wallet.CaptureRefresh(); await refresh.ReadAsync(() => f.Context());
        f.Wallet.SubtractAmount(3); Assert.True(refresh.TryAdopt()); Assert.Equal(114ul, f.Wallet.Amount);
        refresh.Dispose(); await f.Save(); await f.Save();
        using var read = f.Context(); Assert.Equal(114ul, read.AccountCurrency.Single().Amount); Assert.Equal(2, read.AccountCurrencyOperation.Count());
    }

    [Fact]
    public async Task DisposingAReadWhileItsWorkerIsStillPendingKeepsAllIntentAndCannotAdoptALateResult()
    {
        using var f = new Fixture(providerOptions); f.Wallet.AddAmount(10); await ExternalWallet(f, 107);
        using var entered = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
        using var refresh = f.Wallet.CaptureRefresh();
        Task worker = refresh.ReadAsync(() => { entered.Set(); if (!release.Wait(TimeSpan.FromSeconds(20))) throw new TimeoutException(); return f.Context(); });
        try { Assert.True(entered.Wait(TimeSpan.FromSeconds(20))); f.Wallet.AddAmount(3); refresh.Dispose(); }
        finally { release.Set(); }
        await worker; Assert.False(refresh.TryAdopt()); Assert.Equal(113ul, f.Wallet.Amount);
        await f.Save(); using var read = f.Context(); Assert.Equal(120ul, read.AccountCurrency.Single().Amount); Assert.Equal(2, read.AccountCurrencyOperation.Count());
    }

    [Fact]
    public async Task ARequiredContractCreditIsObservedWithoutRegrantEvenWhenItsBalanceWasLegitimatelySpent()
    {
        using var f = new Fixture(providerOptions); var required = Credit(); await ApplyCredit(f, required); await ExternalWallet(f, 3);
        f.Wallet.AddAmount(2); using var refresh = f.Wallet.CaptureRefresh(required); await refresh.ReadAsync(() => f.Context());
        Assert.True(refresh.TryAdopt()); Assert.Equal(5ul, f.Wallet.Amount); refresh.Dispose(); await f.Save();
        using var read = f.Context(); Assert.Equal(5ul, read.AccountCurrency.Single().Amount); Assert.Equal(2, read.AccountCurrencyOperation.Count());
        Assert.Equal(100ul, read.AccountCurrencyOperation.Single(row => row.OperationKey == required.Key).BeforeAmount);
    }

    [Theory]
    [InlineData("missing")] [InlineData("account")] [InlineData("currency")] [InlineData("amount")]
    [InlineData("direction")] [InlineData("source")] [InlineData("arithmetic")]
    public async Task MissingOrChangedRequiredCreditProofQuarantinesTheWalletWithoutAcknowledgingPendingIntent(string fault)
    {
        using var f = new Fixture(providerOptions); var required = Credit();
        if (fault != "missing")
        {
            await ApplyCredit(f, required);
            using var context = f.Context(); var row = context.AccountCurrencyOperation.Single();
            switch (fault)
            {
                case "account": row.AccountId++; break; case "currency": row.CurrencyId++; break;
                case "amount": row.Amount++; break; case "direction": row.Subtract = true; break;
                case "source": row.SourceJson += " "; break; case "arithmetic": row.AfterAmount++; break;
            }
            await context.SaveChangesAsync();
        }
        f.Wallet.AddAmount(10); using var refresh = f.Wallet.CaptureRefresh(required);
        await Assert.ThrowsAnyAsync<Exception>(() => refresh.ReadAsync(() => f.Context()));
        Assert.False(refresh.TryAdopt()); Assert.Equal(110ul, f.Wallet.Amount); Assert.False(f.Wallet.CanAfford(1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Save());
        using var read = f.Context(); Assert.Equal(fault == "missing" ? 100ul : 105ul, read.AccountCurrency.Single().Amount);
        Assert.Equal(fault == "missing" ? 0 : 1, read.AccountCurrencyOperation.Count());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RefreshRejectsADisappearedWalletOrAccountRatherThanInterpretingDeletionAsZero(bool deleteAccount)
    {
        using var f = new Fixture(providerOptions);
        using (var context = f.Context())
        { if (deleteAccount) await context.Account.ExecuteDeleteAsync(); else await context.AccountCurrency.ExecuteDeleteAsync(); }
        using var refresh = f.Wallet.CaptureRefresh(); await Assert.ThrowsAsync<InvalidOperationException>(() => refresh.ReadAsync(() => f.Context()));
        Assert.False(refresh.TryAdopt()); Assert.Equal(100ul, f.Wallet.Amount);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RefreshCannotClampAnUnderflowOrOverflowCausedByAnExternalWalletChange(bool overflow)
    {
        using var f = new Fixture(providerOptions);
        if (overflow) f.Wallet.AddAmount(10); else f.Wallet.SubtractAmount(90);
        await ExternalWallet(f, overflow ? ulong.MaxValue : 1ul);
        using var refresh = f.Wallet.CaptureRefresh(); await refresh.ReadAsync(() => f.Context());
        Assert.False(refresh.TryAdopt()); Assert.Equal(overflow ? 110ul : 10ul, f.Wallet.Amount);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Save());
    }

    [Fact]
    public async Task RefreshCannotStealAnUnsettledSaveAndAForeignTrackedContextCannotSupplyAProof()
    {
        using var f = new Fixture(providerOptions); f.Wallet.AddAmount(1);
        await using (var pending = f.Context()) { f.Wallet.Save(pending); Assert.Throws<InvalidOperationException>(() => f.Wallet.CaptureRefresh()); }
        using var refresh = f.Wallet.CaptureRefresh();
        await Assert.ThrowsAsync<InvalidOperationException>(() => refresh.ReadAsync(() => { var context = f.Context(); context.Account.Single(); return context; }));
        Assert.False(refresh.TryAdopt());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ManagerPublishesTheAdoptedWalletExactlyOnceAndKeepsLaterIntentForNormalSave(bool existing)
    {
        using var f = new Fixture(providerOptions, existing: existing); var manager = RefreshManager(f, existing);
        manager.CurrencyAddAmount(AccountCurrencyType.ProtostarPromissoryNote, 3);
        await ExternalWallet(f, existing ? 105ul : 5ul, !existing);
        int messages = f.SessionProxy.GetInvocations("EnqueueMessageEncrypted").Count;
        using var refresh = manager.BeginWalletRefresh(AccountCurrencyType.ProtostarPromissoryNote, () => f.Context()); await refresh.Completion;
        Assert.Equal(existing ? 103ul : 3ul, manager.GetCurrencyAmount(AccountCurrencyType.ProtostarPromissoryNote));
        Assert.Equal(messages, f.SessionProxy.GetInvocations("EnqueueMessageEncrypted").Count);
        manager.CurrencySubtractAmount(AccountCurrencyType.ProtostarPromissoryNote, 2);
        await using (var blocked = f.Context()) Assert.Throws<InvalidOperationException>(() => manager.Save(blocked));
        Assert.True(refresh.TryPublish()); Assert.False(refresh.TryPublish());
        Assert.Equal(existing ? 106ul : 6ul, manager.GetCurrencyAmount(AccountCurrencyType.ProtostarPromissoryNote));
        var last = f.SessionProxy.GetInvocations("EnqueueMessageEncrypted").TakeLast(2).ToArray();
        Assert.Equal(existing ? 106ul : 6ul, Assert.IsType<ServerAccountCurrencyGrant>(last[0].Arguments[0]).AccountCurrency.Amount);
        Assert.IsType<ServerWalletUpdate>(last[1].Arguments[0]);
        await using (var save = f.Context()) { manager.Save(save); await save.SaveChangesAsync(); }
        using var read = f.Context(); Assert.Equal(existing ? 106ul : 6ul, read.AccountCurrency.Single().Amount);
        Assert.Equal(existing ? 2 : 3, read.AccountCurrencyOperation.Count()); Assert.Empty(read.AccountEntitlement);
    }

    [Theory]
    [InlineData("session")] [InlineData("account")] [InlineData("epoch")] [InlineData("manager")]
    public async Task CompletedManagerRefreshNeverPublishesToAChangedActorOwner(string change)
    {
        using var f = new Fixture(providerOptions); var manager = RefreshManager(f); await ExternalWallet(f, 105);
        using var refresh = manager.BeginWalletRefresh(AccountCurrencyType.ProtostarPromissoryNote, () => f.Context()); await refresh.Completion;
        switch (change)
        {
            case "session": f.AccountProxy.SetProperty(nameof(IAccount.Session), RecordingDispatchProxy<IGameSession>.Create(out _)); break;
            case "account": f.AccountProxy.SetProperty(nameof(IAccount.Id), 78u); break;
            case "epoch": f.AccountProxy.SetProperty(nameof(IAccount.WorldSessionEpoch), 1ul); break;
            case "manager": f.AccountProxy.SetProperty(nameof(IAccount.CurrencyManager), RefreshManager(f)); break;
        }
        Assert.False(refresh.TryPublish()); Assert.Equal(100ul, manager.GetCurrencyAmount(AccountCurrencyType.ProtostarPromissoryNote));
        Assert.Empty(f.SessionProxy.GetInvocations("EnqueueMessageEncrypted"));
        await using var save = f.Context(); Assert.Throws<InvalidOperationException>(() => manager.Save(save));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeDisconnectBeforeOrDuringPublicationStopsLaterPackets(bool during)
    {
        using var f = new Fixture(providerOptions); var session = new RefreshSession(); f.AccountProxy.SetProperty(nameof(IAccount.Session), session);
        var manager = RefreshManager(f); await ExternalWallet(f, 105);
        using var refresh = manager.BeginWalletRefresh(AccountCurrencyType.ProtostarPromissoryNote, () => f.Context()); await refresh.Completion;
        if (during) session.OnSend = session.ForceDisconnect; else session.ForceDisconnect();
        Assert.False(refresh.TryPublish()); Assert.Equal(during ? 1 : 0, session.Messages.Count);
        await using var save = f.Context(); Assert.Throws<InvalidOperationException>(() => manager.Save(save));
    }

    [Fact]
    public async Task ReentrantPublicationPreservesNewIntentAndRefusesNestedRefreshOrSave()
    {
        using var f = new Fixture(providerOptions); var manager = RefreshManager(f); await ExternalWallet(f, 105);
        using var refresh = manager.BeginWalletRefresh(AccountCurrencyType.ProtostarPromissoryNote, () => f.Context()); await refresh.Completion;
        bool once = false;
        f.SessionProxy.SetMethodHandler("EnqueueMessageEncrypted", arguments =>
        {
            if (once) return null; once = true;
            using var context = f.Context(); Assert.Throws<InvalidOperationException>(() => manager.Save(context));
            Assert.Throws<InvalidOperationException>(() => manager.BeginWalletRefresh(AccountCurrencyType.ProtostarPromissoryNote, () => f.Context()));
            Assert.False(refresh.TryPublish()); refresh.Dispose(); manager.CurrencySubtractAmount(AccountCurrencyType.ProtostarPromissoryNote, 2); return null;
        });
        Assert.True(refresh.TryPublish()); Assert.Equal(103ul, manager.GetCurrencyAmount(AccountCurrencyType.ProtostarPromissoryNote));
        f.SessionProxy.SetMethodHandler("EnqueueMessageEncrypted", _ => null);
        await using (var save = f.Context()) { manager.Save(save); await save.SaveChangesAsync(); }
        using var read = f.Context(); Assert.Equal(103ul, read.AccountCurrency.Single().Amount); Assert.Single(read.AccountCurrencyOperation);
    }

    [Fact]
    public void OrdinaryRefreshCannotTakeOverTheExistingCosmicLoyaltyJournal()
    {
        using var f = new Fixture(providerOptions, AccountCurrencyType.CosmicReward);
        f.AccountProxy.SetProperty(nameof(IAccount.LoyaltyManager), RecordingDispatchProxy<IAccountLoyaltyManager>.Create(out _));
        Assert.Throws<InvalidOperationException>(() => f.Wallet.CaptureRefresh());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task WalletRefreshRetainsItsDatabaseAcrossFactoriesAndPriorSaves(bool previouslySaved)
    {
        using var f = new Fixture(providerOptions); using var foreign = new Fixture(null);
        if (previouslySaved) { f.Wallet.AddAmount(1); await f.Save(); }
        using var refresh = f.Wallet.CaptureRefresh(); int calls = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => refresh.ReadAsync(() =>
            previouslySaved || Interlocked.Increment(ref calls) > 1 ? foreign.Context() : f.Context()));
        Assert.False(refresh.TryAdopt()); Assert.Equal(previouslySaved ? 101ul : 100ul, f.Wallet.Amount);
        using var original = f.Context(); Assert.Equal(previouslySaved ? 101ul : 100ul, original.AccountCurrency.Single().Amount);
        using var other = foreign.Context(); Assert.Equal(100ul, other.AccountCurrency.Single().Amount); Assert.Empty(other.AccountCurrencyOperation);
    }

    [Fact]
    public async Task AChangedReceiptForTheActorsOwnUnknownCommitCannotAcknowledgeItsCapturedOperation()
    {
        using var f = new Fixture(providerOptions); f.Wallet.AddAmount(10);
        await using (var failed = f.Context(new CommitFailure(true)))
        { f.Wallet.Save(failed); await Assert.ThrowsAsync<InvalidOperationException>(() => failed.SaveChangesAsync()); }
        using (var changed = f.Context()) { changed.AccountCurrencyOperation.Single().SourceJson = "changed"; await changed.SaveChangesAsync(); }
        using var refresh = f.Wallet.CaptureRefresh(); await Assert.ThrowsAsync<InvalidOperationException>(() => refresh.ReadAsync(() => f.Context()));
        Assert.False(refresh.TryAdopt()); await Assert.ThrowsAsync<InvalidOperationException>(() => f.Save());
        using var read = f.Context(); Assert.Equal(110ul, read.AccountCurrency.Single().Amount); Assert.Single(read.AccountCurrencyOperation);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ManagerRequiresTheActualCreditReceiptBeforePublishingItsWallet(bool exists)
    {
        using var f = new Fixture(providerOptions); var manager = RefreshManager(f); var required = Credit();
        if (exists) await ApplyCredit(f, required);
        using var refresh = manager.BeginWalletRefresh(required.Currency, () => f.Context(), required);
        if (exists) await refresh.Completion; else await Assert.ThrowsAsync<InvalidOperationException>(() => refresh.Completion);
        Assert.Equal(exists, refresh.TryPublish()); Assert.Equal(exists ? 105ul : 100ul, manager.GetCurrencyAmount(required.Currency));
        Assert.Equal(exists ? 2 : 0, f.SessionProxy.GetInvocations("EnqueueMessageEncrypted").Count);
        using var read = f.Context(); Assert.Equal(exists ? 1 : 0, read.AccountCurrencyOperation.Count());
    }

    [Fact]
    public async Task RepeatedRefreshOfAnAbsentWalletRetainsItsUnsavedCreationAndDoesNotInventACredit()
    {
        using var f = new Fixture(providerOptions, existing: false); var manager = RefreshManager(f, false);
        for (int repeat = 0; repeat < 2; repeat++)
        {
            using var refresh = manager.BeginWalletRefresh(AccountCurrencyType.ProtostarPromissoryNote, () => f.Context());
            await refresh.Completion; Assert.True(refresh.TryPublish());
            using var before = f.Context(); Assert.Empty(before.AccountCurrency); Assert.Empty(before.AccountCurrencyOperation);
        }
        await using (var save = f.Context()) { manager.Save(save); await save.SaveChangesAsync(); }
        using var read = f.Context(); Assert.Equal(0ul, read.AccountCurrency.Single().Amount); Assert.Single(read.AccountCurrencyOperation);
    }

    private static AccountCurrencyManager RefreshManager(Fixture f, bool existing = true)
    {
        var model = new AccountModel { Id = 77 };
        if (existing) model.AccountCurrency.Add(new() { Id = 77, CurrencyId = (byte)AccountCurrencyType.ProtostarPromissoryNote, Amount = 100 });
        var manager = new AccountCurrencyManager(f.Account, model, f.Tables); f.AccountProxy.SetProperty(nameof(IAccount.CurrencyManager), manager); return manager;
    }
    private static async Task ExternalWallet(Fixture f, ulong amount, bool create = false)
    {
        await using var context = f.Context();
        if (create) { context.AccountCurrency.Add(new() { Id = 77, CurrencyId = (byte)AccountCurrencyType.ProtostarPromissoryNote, Amount = amount }); await context.SaveChangesAsync(); }
        else await context.AccountCurrency.ExecuteUpdateAsync(set => set.SetProperty(row => row.Amount, amount));
    }
    private static AccountWalletOperation Credit() => new("contract:1:" + Guid.NewGuid().ToString("N"), 77, AccountCurrencyType.ProtostarPromissoryNote, 5, false, "{\"version\":1}");
    private static async Task ApplyCredit(Fixture f, AccountWalletOperation operation)
    {
        await using var context = f.Context(); AccountCurrencyOperationRepository repository = null;
        context.EnlistSaveWork(unit => { repository = new(unit, 77, operation.Currency, [operation]); repository.Stage(); }, _ => { }, () => repository.CanCommit());
        await context.SaveChangesAsync();
    }
    private sealed class RefreshSession : NetworkSession, IGameSession
    {
        internal readonly List<IWritable> Messages = [];
        internal Action OnSend;
        public bool CanProcessIncomingPackets { get; set; }
        public bool CanProcessOutgoingPackets { get; set; }
        public void EnqueueMessage(IWritable message) => EnqueueMessageEncrypted(message);
        public void EnqueueMessageEncrypted(IWritable message) { Messages.Add(message); OnSend?.Invoke(); }
        public void EnqueueMessageEncrypted(uint opcode, string hex) => throw new NotSupportedException();
        public void HandlePacket(ClientGamePacket packet) => throw new NotSupportedException();
        public void FlushPackets() { }
        protected override uint OnData(byte[] buffer, int offset, int count) => 0;
    }
}
