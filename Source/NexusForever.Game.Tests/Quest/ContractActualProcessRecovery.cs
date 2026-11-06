using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest.Reward;
using NexusForever.Game.Tests.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;

namespace NexusForever.Game.Tests.Quest;

// Explicit child-process probe. The parent kills only the recorded process
// generation at a real transaction boundary, then starts independent recovery.
public static class ContractActualProcessRecovery
{
    private static readonly JsonSerializerOptions json = new() { WriteIndented = true };
    private static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
    private static void Write(string path, object value) => File.WriteAllText(path, JsonSerializer.Serialize(value, json));

    public static async Task RunAsync(string mode, string recoveryRoot, string caseDirectory, string window)
    {
        recoveryRoot = Path.GetFullPath(recoveryRoot); caseDirectory = Path.GetFullPath(caseDirectory);
        Directory.CreateDirectory(caseDirectory);
        var (manifest, _) = ContractActualCatalogQualification.VerifyInputs(recoveryRoot);
        var tables = await ContractActualCatalogQualification.LoadTablesAsync(recoveryRoot);
        var catalog = new ContractRewardAwardCatalog(tables);
        Assert.Equal(726, catalog.Awards.Count);
        var character = new DbContextOptionsBuilder<CharacterContext>().UseSqlite($"Data Source={Path.Combine(caseDirectory, "character.sqlite")};Pooling=False").Options;
        var auth = new DbContextOptionsBuilder<AuthContext>().UseSqlite($"Data Source={Path.Combine(caseDirectory, "auth.sqlite")};Pooling=False").Options;
        if (mode == "produce")
        {
            Assert.False(File.Exists(Path.Combine(caseDirectory, "producer.json")));
            bool account = window.StartsWith("account-", StringComparison.Ordinal);
            bool nonstackable = window == "nonstackable-after-character";
            ContractRewardAward award = account
                ? catalog.Awards.First(award => award.AccountCurrency?.Amount == (window == "account-after-ack" ? 7ul : 5ul))
                : nonstackable ? catalog.Awards.First(award => award.Item != null && award.CharacterCurrency == null
                    && !new ItemInfo(tables.Item.GetEntry(award.Item.ItemId), tables).IsStackable())
                : catalog.Awards.First(award => award.RewardRowId == 69 && award.Choice == 0);
            using var f = new ContractClaimTransactionTests.Fixture(character, auth, award.RewardRowId, award.Choice, tables);
            string DirectoryOf(object fixture) => (string)fixture.GetType().GetField("directory", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(fixture);
            Write(Path.Combine(caseDirectory, "producer.json"), new
            {
                Window = window, ProcessId = Environment.ProcessId, StartedUtc = Process.GetCurrentProcess().StartTime.ToUniversalTime(),
                Exe = Environment.ProcessPath, ManifestSha256 = Convert.ToHexStringLower(SHA256.HashData(manifest)),
                ClaimId = f.Command.Identity, award.RewardRowId, award.Choice, award.TrackId, award.Ordinal,
                QuoteJson = f.Quote.Json, QuoteSha256 = Hash(f.Quote.Json),
                ItemId = award.Item?.ItemId, ItemCount = award.Item?.Count, Credit = award.CharacterCurrency?.Amount ?? 0,
                AccountAmount = award.AccountCurrency?.Amount ?? 0,
                OwnedTemporaryDirectories = new[] { DirectoryOf(f), DirectoryOf(f.Inventory) }
            });
            var pause = new CommitPause(caseDirectory, window);
            f.CharacterFault = pause; f.AuthFault = pause;
            using var held = f.Reserve();
            Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, (await f.Execute(held)).Outcome);
            if (held != null) Assert.True(held.PublishCommitted());
            if (account) Assert.Equal(ContractAccountDeliveryOutcome.Delivered, (await f.Sender().DeliverAsync(f.Command.Identity)).Outcome);
            Assert.Equal("control", window);
            Write(Path.Combine(caseDirectory, "producer-complete.json"), new
            { Completed = true, ItemIds = f.Inventory.Bag.Select(item => item.Guid).ToArray() });
            return;
        }
        Assert.Contains(mode, new[] { "recover", "spend", "verify-spent", "replay-spent" });
        using var original = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(caseDirectory, "producer.json")));
        var source = original.RootElement;
        Assert.Equal(window, source.GetProperty("Window").GetString());
        Assert.Equal(source.GetProperty("ManifestSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(manifest)));
        string claimId = source.GetProperty("ClaimId").GetString();
        ulong accountAmount = source.GetProperty("AccountAmount").GetUInt64(), credit = source.GetProperty("Credit").GetUInt64();
        bool committed = window != "character-before";
        bool spent = File.Exists(Path.Combine(caseDirectory, "spent.json"));
        Assert.Equal(mode is "verify-spent" or "replay-spent", spent);
        bool replayed = false;
        string replayTemporaryDirectory = null;
        if (mode == "replay-spent")
        {
            Assert.True(committed);
            var quote = ContractRewardClaimQuote.Restore(catalog, source.GetProperty("QuoteJson").GetString());
            var definition = catalog.Catalog.GetTrack(quote.Award.TrackId);
            var selection = ContractRewardSelection.Restore(catalog.Catalog, quote.SelectionJson);
            var before = new ContractTrackState(42, definition.Id, quote.CycleId, quote.Points, quote.Revision, true)
            {
                SelectionInitialized = true,
                Slots = Array.AsReadOnly(definition.Slots.Select(slot => new ContractRewardSlotState(slot.Ordinal,
                    selection.Rewards[slot.Ordinal], slot.RequiredPoints, selection.Json)).ToArray())
            };
            DateTime created;
            using (var originalReceipt = new CharacterContext(character))
                created = DateTime.SpecifyKind(originalReceipt.CharacterContractRewardClaim.Single().CreatedUtc, DateTimeKind.Utc);
            var command = CharacterChallengeRewardCommand.ClaimContract(42, 27, 1, claimId, created, catalog, [before], quote,
                quote.Award.CharacterCurrency == null ? null : new(true, 1234));
            IItemInfo info = quote.Award.Item == null ? null : new ItemInfo(tables.Item.GetEntry(quote.Award.Item.ItemId), tables);
            using var inventory = info == null ? null : new InventoryRewardDeliveryTests.Fixture(3, [], providerOptions: character,
                contract: true, itemId: info.Id, actualInfo: info, loadExisting: true);
            if (inventory != null)
                replayTemporaryDirectory = (string)inventory.GetType().GetField("directory", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(inventory);
            using var reservation = inventory?.Reserve(quote.Award.Item.Count);
            var executor = new CharacterChallengeRewardExecutor(() => new(character), new CharacterChallengeRewardCatalog(tables), tables);
            var replay = await executor.ExecuteAsync(command, reservation);
            Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, replay.Outcome);
            Assert.False(Assert.IsType<ContractClaimStage>(replay.Operation).Applied);
            Assert.False(replay.Changed);
            if (reservation != null) Assert.Equal(InventoryRewardReservationState.Prepared, reservation.State);
            replayed = true;
        }
        if (committed && accountAmount != 0)
        {
            var sender = new ContractAccountDelivery(() => new(character), () => new(auth), catalog, 1);
            Assert.Equal(ContractAccountDeliveryOutcome.Delivered, (await sender.DeliverAsync(claimId)).Outcome);
            Assert.Equal(ContractAccountDeliveryOutcome.Delivered, (await sender.DeliverAsync(claimId)).Outcome);
        }
        using var read = new CharacterContext(character);
        using var tx = read.Database.BeginTransaction(IsolationLevel.Serializable);
        var state = ContractRewardSnapshot.Validate(ContractRewardSnapshot.Read(read, 42, () => catalog.Catalog), 42, catalog.Catalog);
        var track = Assert.Single(state);
        Assert.Equal(committed ? accountAmount == 0 ? 2ul : 3ul : 1ul, track.Revision);
        var claims = read.CharacterContractRewardClaim.AsNoTracking().ToArray();
        Assert.Equal(committed ? 1 : 0, claims.Length);
        var slot = track.Slots[source.GetProperty("Ordinal").GetByte()];
        Assert.Equal(committed, slot.Claimed);
        object receiptIdentity = null;
        if (committed)
        {
            var receipt = Assert.Single(claims);
            Assert.Equal(claimId, receipt.ClaimId); Assert.Equal(source.GetProperty("QuoteJson").GetString(), receipt.QuoteJson);
            Assert.Equal(27u, receipt.AccountId);
            receiptIdentity = new { receipt.ClaimId, receipt.AccountId, receipt.RewardRowId, receipt.Choice, receipt.CreatedUtc,
                QuoteSha256 = Hash(receipt.QuoteJson), DeliverySha256 = Hash(receipt.DeliveryJson), receipt.CharacterCurrencyJson };
        }
        Assert.Empty(read.CharacterChallengeRewardClaim); Assert.Empty(read.CharacterChallengeMedalReceipt);
        Assert.Equal(committed && !spent ? 1234ul + credit : 1234ul, read.CharacterCurrency.Single().Amount);
        var itemRows = read.Item.AsNoTracking().ToArray();
        bool itemAward = source.GetProperty("ItemId").ValueKind != JsonValueKind.Null;
        Assert.Equal(committed && !spent && itemAward ? 1 : 0, itemRows.Length);
        var manager = RecordingDispatchProxy<IItemManager>.Create(out var proxy);
        proxy.SetMethodHandler(nameof(IItemManager.GetItemInfo), args => new ItemInfo(tables.Item.GetEntry((uint)args[0]), tables));
        foreach (var row in itemRows)
        {
            Assert.Equal(source.GetProperty("ItemId").GetUInt32(), row.ItemId);
            Assert.Equal(source.GetProperty("ItemCount").GetUInt32(), row.StackCount); Assert.True(row.Soulbound);
            var item = new NexusForever.Game.Entity.Item(row, manager);
            Assert.IsType<ItemInfo>(item.Info); Assert.Equal(row.StackCount, item.StackCount);
        }
        var outboxes = read.CharacterContractAccountOutbox.AsNoTracking().ToArray();
        Assert.Equal(committed && accountAmount != 0 ? 1 : 0, outboxes.Length);
        if (outboxes.Length != 0) Assert.True(outboxes[0].Delivered);
        using var wallet = new AuthContext(auth);
        Assert.Equal(committed && !spent ? 100ul + accountAmount : 100ul, wallet.AccountCurrency.Single().Amount);
        var operations = wallet.AccountCurrencyOperation.AsNoTracking().ToArray();
        Assert.Equal(committed && accountAmount != 0 ? 1 : 0, operations.Length);
        tx.Commit(); tx.Dispose();
        if (mode == "spend")
        {
            Assert.True(committed); Assert.False(spent);
            read.Item.ExecuteDelete();
            if (credit != 0) read.CharacterCurrency.ExecuteUpdate(set => set.SetProperty(row => row.Amount, 1234ul));
            if (accountAmount != 0) wallet.AccountCurrency.ExecuteUpdate(set => set.SetProperty(row => row.Amount, 100ul));
            Write(Path.Combine(caseDirectory, "spent.json"), new { Spent = true, ItemRows = itemRows.Length, credit, accountAmount });
        }
        Write(Path.Combine(caseDirectory, mode + "-result.json"), new
        {
            ProcessId = Environment.ProcessId, Mode = mode, window, Committed = committed, Spent = spent,
            ReplayedOriginalClaim = replayed, ReplayTemporaryDirectory = replayTemporaryDirectory,
            ReceiptIdentity = receiptIdentity, ReceiptIdentitySha256 = receiptIdentity == null ? null : Hash(JsonSerializer.Serialize(receiptIdentity)),
            OriginalItemIds = itemRows.Select(item => item.Id).ToArray(), ItemRows = itemRows.Length,
            AccountOperations = operations.Select(operation => operation.OperationKey).ToArray(), TrackRevision = track.Revision,
            ReaderSha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(GameTable<>).Assembly.Location))),
            GameSha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(ItemInfo).Assembly.Location))),
            ProbeSha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(ContractActualProcessRecovery).Assembly.Location)))
        });
    }

    private sealed class CommitPause(string directory, string window) : DbTransactionInterceptor
    {
        private int fired;
        private bool Target(DbContext context, bool after)
        {
            if (window == "control") return false;
            if (window is "character-before" or "item-after-character" or "nonstackable-after-character" or "account-after-character")
                return after == (window != "character-before") && context is CharacterContext character
                    && character.ChangeTracker.Entries<NexusForever.Database.Character.Model.CharacterContractRewardClaimModel>().Any();
            if (window is "account-before-auth" or "account-after-auth")
                return after == (window == "account-after-auth") && context is AuthContext auth
                    && auth.ChangeTracker.Entries<AccountCurrencyOperationModel>().Any();
            return window == "account-after-ack" && after && context is CharacterContext ack
                && ack.ChangeTracker.Entries<NexusForever.Database.Character.Model.CharacterContractAccountOutboxModel>().Any(entry => entry.Entity.Delivered);
        }
        private void Pause(DbContext context, bool after)
        {
            if (!Target(context, after) || Interlocked.CompareExchange(ref fired, 1, 0) != 0) return;
            Write(Path.Combine(directory, "gate.json"), new
            {
                Window = window, AfterCommit = after, ProcessId = Environment.ProcessId,
                Claims = context is CharacterContext character ? character.ChangeTracker.Entries<NexusForever.Database.Character.Model.CharacterContractRewardClaimModel>()
                    .Select(entry => new { entry.Entity.ClaimId, QuoteSha256 = Hash(entry.Entity.QuoteJson), DeliverySha256 = Hash(entry.Entity.DeliveryJson) }).ToArray() : null,
                ItemIds = context is CharacterContext items ? items.ChangeTracker.Entries<NexusForever.Database.Character.Model.ItemModel>()
                    .Select(entry => entry.Entity.Id).ToArray() : null
            });
            Thread.Sleep(Timeout.Infinite);
        }
        public override InterceptionResult TransactionCommitting(System.Data.Common.DbTransaction transaction, TransactionEventData data, InterceptionResult result)
        { Pause(data.Context, false); return result; }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(System.Data.Common.DbTransaction transaction, TransactionEventData data, InterceptionResult result, CancellationToken cancellationToken = default)
            => new(TransactionCommitting(transaction, data, result));
        public override void TransactionCommitted(System.Data.Common.DbTransaction transaction, TransactionEndEventData data) => Pause(data.Context, true);
        public override Task TransactionCommittedAsync(System.Data.Common.DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
        { Pause(data.Context, true); return Task.CompletedTask; }
    }
}
