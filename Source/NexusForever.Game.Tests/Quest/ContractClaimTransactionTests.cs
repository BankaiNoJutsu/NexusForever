using System.Data;
using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Reward;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Reward;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Quest.Reward;
using NexusForever.Game.Tests.Account.Reward;
using NexusForever.Game.Tests.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Quest;

public sealed partial class ContractClaimTransactionTests
{
    private readonly DbContextOptions<CharacterContext> characterOptions;
    private readonly DbContextOptions<AuthContext> authOptions;
    public ContractClaimTransactionTests() { }
    internal ContractClaimTransactionTests(DbContextOptions<CharacterContext> character, DbContextOptions<AuthContext> auth)
    { characterOptions = character; authOptions = auth; }

    [Fact]
    public async Task TheRealQuestOwnerPublishesOnlyValidatedClaimedBitsWithTheCommittedSnapshotIdentity()
    {
        using var f = new Fixture(characterOptions, authOptions, 69, 0); using var delivery = f.Reserve();
        var result = await f.Execute(delivery); Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, result.Outcome);
        var player = f.Inventory.Player; var presentation = new RewardTrackPresentation(player);
        f.Inventory.PlayerProxy.SetProperty(nameof(IPlayer.RewardTrackPresentation), presentation);
        var quests = new QuestManager(player, new() { Id = 42 }, gameTableManager: f.Tables);
        f.Inventory.PlayerProxy.SetProperty(nameof(IPlayer.QuestManager), quests);
        Assert.True(presentation.PublishAccountTracks([])); Assert.True(presentation.PublishCharacterTracks([], new object()));
        quests.ExpectContractRewardTracks(true); quests.AdoptContractRewards(result.State.ContractTracks); Assert.True(quests.PublishContractRewards());
        var messages = f.Inventory.SessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).OfType<ServerRewardTracksReload>().ToArray();
        var row = Assert.Single(messages.Last().RewardsTracks); Assert.NotEqual(0, row.ClaimedRewards.GetBuffer()[f.Quote.Award.Ordinal / 8] & (1 << (f.Quote.Award.Ordinal % 8)));
        Assert.True(presentation.IsPublishedContractSnapshot(quests.CommittedContractTracks));
    }

    public static IEnumerable<object[]> RepresentativeChoices()
    {
        var emitted = new HashSet<(uint, uint)>();
        foreach (uint track in new uint[] { 4, 5 })
        {
            var rows = LoyaltyCatalogBuild16042Fixture.Rewards().Where(row => row.RewardTrackId == track).ToArray();
            uint first = rows.First(row => (row.Flags & 1u) == 0).Id, final = rows.Single(row => (row.Flags & 1u) != 0).Id;
            for (uint choice = 0; choice < 3; ++choice)
            {
                if (emitted.Add((first, choice))) yield return [first, choice];
                if (emitted.Add((final, choice))) yield return [final, choice];
            }
            foreach (uint account in new uint[] { 2485, 2486 })
            {
                uint row = rows.First(row => row.RewardChoiceId02 == account).Id;
                if (emitted.Add((row, 2))) yield return [row, 2u];
            }
        }
    }

    [Theory]
    [InlineData("wallet")] [InlineData("currency-witness")] [InlineData("claim")] [InlineData("slot")] [InlineData("outbox")]
    public async Task SuppressingAnyClaimComponentRollsBackItemsCreditsAndTheCompensationIntent(string fault)
    {
        using var f = new Fixture(characterOptions, authOptions, fault == "outbox" ? Fixture.CompensationRow(2485) : 69u, fault == "outbox" ? 2u : 0u);
        using var delivery = f.Reserve();
        await using (var context = f.Character())
        await using (var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            var repository = new CharacterChallengeRewardRepository(context, 42, new(f.Tables), f.Tables, f.Command.Meets);
            repository.ClaimContract(f.Command, delivery); await context.SaveChangesAsync();
            if (fault == "wallet") await context.CharacterCurrency.Where(row => row.Id == 42).ExecuteUpdateAsync(set => set.SetProperty(row => row.Amount, 1234ul));
            if (fault == "currency-witness") await context.CharacterContractRewardClaim.ExecuteUpdateAsync(set => set.SetProperty(row => row.CharacterCurrencyJson, "{}"));
            if (fault == "claim") await context.CharacterContractRewardClaim.ExecuteDeleteAsync();
            if (fault == "slot") await context.CharacterContractRewardSlot.Where(row => row.RewardRowId == f.Quote.Award.RewardRowId).ExecuteUpdateAsync(set => set.SetProperty(row => row.Claimed, false));
            if (fault == "outbox") await context.CharacterContractAccountOutbox.ExecuteDeleteAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(() => tx.CommitAsync()); await tx.RollbackAsync();
        }
        if (delivery != null)
        {
            await using var fresh = f.Character(); await using var tx = await fresh.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            Assert.Equal(InventoryRewardReservationState.ReadyToRetry, delivery.ResolveOutcome(fresh)); Assert.True(delivery.Cancel());
        }
        await using var read = f.Character(); Assert.Empty(read.CharacterContractRewardClaim); Assert.Empty(read.CharacterContractAccountOutbox);
        Assert.Equal(1234ul, read.CharacterCurrency.Single().Amount); Assert.All(read.CharacterContractRewardSlot, row => Assert.False(row.Claimed));
    }

    [Theory]
    [InlineData("owner")] [InlineData("revision")] [InlineData("points")] [InlineData("wallet")]
    public async Task AClaimCannotRebaseAnUnpublishedDifferentOwnerOrChangedWalletBaseline(string fault)
    {
        using var f = new Fixture(characterOptions, authOptions, 69, 0); using var delivery = f.Reserve();
        await using (var external = f.Character())
        {
            if (fault == "owner") await external.Character.ExecuteUpdateAsync(set => set.SetProperty(row => row.AccountId, 28u));
            if (fault == "revision") await external.CharacterContractRewardTrack.ExecuteUpdateAsync(set => set.SetProperty(row => row.Revision, 9ul));
            if (fault == "points") await external.CharacterContractRewardTrack.ExecuteUpdateAsync(set => set.SetProperty(row => row.Points, 5018u));
            if (fault == "wallet") await external.CharacterCurrency.ExecuteUpdateAsync(set => set.SetProperty(row => row.Amount, 1235ul));
        }
        Assert.NotEqual(CharacterChallengeRewardExecutionOutcome.Committed, (await f.Execute(delivery)).Outcome);
        await using var read = f.Character(); Assert.Empty(read.CharacterContractRewardClaim); Assert.Empty(read.CharacterContractAccountOutbox);
        Assert.Equal(fault == "wallet" ? 1235ul : 1234ul, read.CharacterCurrency.Single().Amount);
        if (delivery.State == InventoryRewardReservationState.Prepared) Assert.True(delivery.Cancel());
    }

    [Theory]
    [InlineData(0ul, 2501000ul)] [InlineData(1005ul, 1005ul)] [InlineData(1000ul, 1000ul)]
    public void FinalCreditWitnessPreservesTheExistingCurrencyCapWithoutWrappingOrInventingAddedMoney(ulong cap, ulong after)
    {
        var proof = ContractCurrencyDelivery.Capture(new(NexusForever.Game.Static.Entity.CurrencyType.Credits, 2500000, cap), new(true, 1000));
        Assert.Equal(after, proof.AfterAmount); Assert.Equal(after - 1000, proof.Granted);
        Assert.Equal(proof, ContractCurrencyDelivery.Restore(new(NexusForever.Game.Static.Entity.CurrencyType.Credits, 2500000, cap), proof.Json));
    }

    [Fact]
    public async Task AConflictingGlobalAuthReceiptNeverRecreditsOrAcknowledgesTheCharacterIntent()
    {
        using var f = new Fixture(characterOptions, authOptions, Fixture.CompensationRow(2485), 2); await f.Execute();
        await using (var source = f.Character())
        await using (var auth = f.Auth())
        {
            var outbox = source.CharacterContractAccountOutbox.Single(); var intent = ContractAccountIntent.Restore(f.Awards, outbox.PayloadJson);
            auth.AccountCurrencyOperation.Add(new() { OperationKey = intent.OperationKey, AccountId = 28, CurrencyId = 14, Amount = 5, BeforeAmount = 100, AfterAmount = 105, SourceJson = intent.Json }); await auth.SaveChangesAsync();
        }
        Assert.Equal(ContractAccountDeliveryOutcome.Conflict, (await f.Sender().DeliverAsync(f.Command.Identity)).Outcome);
        await using var read = f.Auth(); Assert.Equal(100ul, read.AccountCurrency.Single().Amount); Assert.Single(read.AccountCurrencyOperation);
        await using var character = f.Character(); Assert.False(character.CharacterContractAccountOutbox.Single().Delivered);
    }

    [Theory]
    [MemberData(nameof(RepresentativeChoices))]
    public async Task ActualItemCreditAndCompensationChoicesShareOneCanonicalClaimWithoutChallengeHistory(uint reward, uint choice)
    {
        using var f = new Fixture(characterOptions, authOptions, reward, choice);
        using var delivery = f.Reserve(); var result = await f.Execute(delivery);
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, result.Outcome); var stage = Assert.IsType<ContractClaimStage>(result.Operation); Assert.True(stage.Applied);
        var track = result.State.ContractTracks.Single(row => row.TrackId == f.Quote.Award.TrackId); var slot = track.Slots[f.Quote.Award.Ordinal];
        Assert.True(slot.Claimed); Assert.Equal(f.Command.Identity, slot.Claim.ClaimId); Assert.Equal(f.Before.Points, track.Points); Assert.Equal(7ul, track.CycleId); Assert.Equal(2ul, track.Revision);
        await using var read = f.Character(); Assert.Empty(read.CharacterChallengeRewardClaim); Assert.Empty(read.CharacterChallengeMedalReceipt);
        var receipt = Assert.Single(read.CharacterContractRewardClaim); Assert.Equal(f.Quote.Json, receipt.QuoteJson); Assert.Equal(27u, receipt.AccountId);
        if (f.Quote.Award.Item != null)
        {
            Assert.Equal(InventoryRewardReservationState.Committed, delivery.State); Assert.True(delivery.PublishCommitted()); Assert.False(delivery.PublishCommitted());
            Assert.Equal(13ul + f.Quote.Award.Item.Count, read.Item.AsEnumerable().Aggregate(0ul, (sum, row) => sum + row.StackCount)); Assert.Empty(read.CharacterContractAccountOutbox);
        }
        else
        {
            Assert.Null(delivery); Assert.Single(read.CharacterContractAccountOutbox); Assert.Equal(13ul, read.Item.AsEnumerable().Aggregate(0ul, (sum, row) => sum + row.StackCount));
        }
        Assert.Equal(f.Quote.Award.CharacterCurrency == null ? 1234ul : 2501234ul, read.CharacterCurrency.Single().Amount);
        var reload = await f.Load(); Assert.True(reload.ContractTracks.Single(row => row.TrackId == f.Quote.Award.TrackId).Slots[f.Quote.Award.Ordinal].Claimed);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task FailedOrUnknownClaimCommitNeverReplaysTheSelectionCreditOrItemDelivery(bool account, bool after)
    {
        uint reward = account ? Fixture.CompensationRow(2485) : 69u;
        using var f = new Fixture(characterOptions, authOptions, reward, account ? 2u : 0u); using var delivery = f.Reserve();
        f.CharacterFault = new CommitFault(after); var result = await f.Execute(delivery);
        Assert.Equal(after ? CharacterChallengeRewardExecutionOutcome.Committed : CharacterChallengeRewardExecutionOutcome.Failed, result.Outcome);
        Assert.True(result.Recovered); await using var read = f.Character(); Assert.Equal(after ? 1 : 0, read.CharacterContractRewardClaim.Count());
        Assert.Equal(after && !account ? 2501234ul : 1234ul, read.CharacterCurrency.Single().Amount);
        Assert.Equal(after && account ? 1 : 0, read.CharacterContractAccountOutbox.Count());
        if (delivery != null) { if (after) Assert.True(delivery.PublishCommitted()); else Assert.True(delivery.Cancel()); }
    }

    [Fact]
    public async Task ReplayingTheRetainedClaimIsReadOnlyAndAnotherOperationCannotReplaceItsChoice()
    {
        using var f = new Fixture(characterOptions, authOptions, 69, 0); using var delivery = f.Reserve();
        Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, (await f.Execute(delivery)).Outcome); Assert.True(delivery.PublishCommitted());
        var duplicate = await f.Execute(delivery); Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, duplicate.Outcome);
        Assert.False(Assert.IsType<ContractClaimStage>(duplicate.Operation).Applied); Assert.False(duplicate.Changed);
        var foreign = CharacterChallengeRewardCommand.ClaimContract(42, 27, 1, Guid.NewGuid().ToString("N"), DateTime.UtcNow, f.Awards, [f.Before], f.Quote, new(true, 1234));
        var failed = await f.Executor.ExecuteAsync(foreign); Assert.NotEqual(CharacterChallengeRewardExecutionOutcome.Committed, failed.Outcome);
        await using var read = f.Character(); Assert.Single(read.CharacterContractRewardClaim); Assert.Equal(2501234ul, read.CharacterCurrency.Single().Amount);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CompensationIsDeliveredOnceAndItsAcknowledgementAdvancesThePublishedTrackRevision(bool seven)
    {
        using var f = new Fixture(characterOptions, authOptions, Fixture.CompensationRow(seven ? 2486u : 2485u), 2); Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, (await f.Execute()).Outcome);
        var sender = f.Sender(); Assert.Equal(ContractAccountDeliveryOutcome.Delivered, (await sender.DeliverAsync(f.Command.Identity)).Outcome);
        Assert.Equal(ContractAccountDeliveryOutcome.Delivered, (await sender.DeliverAsync(f.Command.Identity)).Outcome);
        await using var auth = f.Auth(); Assert.Equal(seven ? 107ul : 105ul, auth.AccountCurrency.Single().Amount); Assert.Single(auth.AccountCurrencyOperation);
        var loaded = await f.Load(); var track = loaded.ContractTracks.Single(); Assert.Equal(3ul, track.Revision); Assert.True(track.Slots[f.Quote.Award.Ordinal].Claim.AccountDelivered);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AuthCommitAcknowledgementFailureCanBeRetriedWithoutDoubleCompensation(bool after)
    {
        using var f = new Fixture(characterOptions, authOptions, Fixture.CompensationRow(2485), 2); await f.Execute(); f.AuthFault = new CommitFault(after);
        var first = await f.Sender().DeliverAsync(f.Command.Identity);
        Assert.Equal(after ? ContractAccountDeliveryOutcome.Delivered : ContractAccountDeliveryOutcome.Pending, first.Outcome);
        Assert.Equal(ContractAccountDeliveryOutcome.Delivered, (await f.Sender().DeliverAsync(f.Command.Identity)).Outcome);
        await using var auth = f.Auth(); Assert.Equal(105ul, auth.AccountCurrency.Single().Amount); Assert.Single(auth.AccountCurrencyOperation);
    }

    [Fact]
    public async Task SourceAcknowledgementLossCannotRecreditOrAdvanceItsTrackTwice()
    {
        using var f = new Fixture(characterOptions, authOptions, Fixture.CompensationRow(2485), 2); await f.Execute(); f.CharacterFault = new CommitFault(true, acknowledge: true);
        Assert.Equal(ContractAccountDeliveryOutcome.Delivered, (await f.Sender().DeliverAsync(f.Command.Identity)).Outcome);
        Assert.Equal(ContractAccountDeliveryOutcome.Delivered, (await f.Sender().DeliverAsync(f.Command.Identity)).Outcome);
        await using var auth = f.Auth(); Assert.Equal(105ul, auth.AccountCurrency.Single().Amount);
        Assert.Equal(3ul, (await f.Load()).ContractTracks.Single().Revision);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task DurableCompensationRetainsItsOriginalAccountAcrossTransferOrCharacterDeletion(bool delete)
    {
        using var f = new Fixture(characterOptions, authOptions, Fixture.CompensationRow(2485), 2); await f.Execute();
        await using (var source = f.Character())
        {
            if (delete) await source.Character.Where(row => row.Id == 42).ExecuteDeleteAsync();
            else await source.Character.Where(row => row.Id == 42).ExecuteUpdateAsync(set => set.SetProperty(row => row.AccountId, 28u));
            Assert.Single(source.CharacterContractAccountOutbox);
        }
        Assert.Equal(ContractAccountDeliveryOutcome.Delivered, (await f.Sender().DeliverAsync(f.Command.Identity)).Outcome);
        await using var auth = f.Auth(); Assert.Equal(105ul, auth.AccountCurrency.Single(row => row.Id == 27).Amount); Assert.Single(auth.AccountCurrencyOperation);
        Assert.Empty(auth.AccountCurrency.Where(row => row.Id == 28));
    }

    [Theory]
    [InlineData("realm")] [InlineData("owner")] [InlineData("missing-account")]
    public async Task UnprovedCompensationCannotFallBackToAnotherRealmOrAccount(string fault)
    {
        using var f = new Fixture(characterOptions, authOptions, Fixture.CompensationRow(2485), 2); await f.Execute();
        if (fault == "owner") { await using var source = f.Character(); await source.CharacterContractAccountOutbox.ExecuteUpdateAsync(set => set.SetProperty(row => row.AccountId, 28u)); }
        if (fault == "missing-account") { await using var auth = f.Auth(); await auth.Account.Where(row => row.Id == 27).ExecuteDeleteAsync(); }
        var result = await f.Sender(fault == "realm" ? (ushort)2 : (ushort)1).DeliverAsync(f.Command.Identity);
        Assert.Equal(fault == "missing-account" ? ContractAccountDeliveryOutcome.Pending : ContractAccountDeliveryOutcome.Conflict, result.Outcome);
        await using var read = f.Auth(); Assert.Empty(read.AccountCurrencyOperation);
        await using var sourceRead = f.Character(); Assert.False(sourceRead.CharacterContractAccountOutbox.Single().Delivered);
    }

    internal sealed class Fixture : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "NexusForever-ContractClaim-" + Guid.NewGuid().ToString("N"));
        private readonly DbContextOptions<AuthContext> authOptions;
        private readonly InventoryRewardDeliveryTests.Fixture inventory;
        internal InventoryRewardDeliveryTests.Fixture Inventory => inventory;
        internal readonly IGameTableManager Tables;
        internal readonly ContractRewardAwardCatalog Awards;
        internal readonly ContractTrackState Before;
        internal readonly ContractRewardClaimQuote Quote;
        internal readonly CharacterChallengeRewardCommand Command;
        internal CommitFault CharacterFault, AuthFault;
        internal CharacterChallengeRewardExecutor Executor { get; }
        internal static uint CompensationRow(uint account) => LoyaltyCatalogBuild16042Fixture.Rewards().First(row => row.RewardTrackId == 5 && row.RewardChoiceId02 == account).Id;
        internal Fixture(DbContextOptions<CharacterContext> character, DbContextOptions<AuthContext> auth, uint reward, uint choice, IGameTableManager actualTables = null)
        {
            Directory.CreateDirectory(directory); authOptions = auth ?? new DbContextOptionsBuilder<AuthContext>().UseSqlite($"Data Source={Path.Combine(directory, "auth.sqlite")};Pooling=False").Options;
            if (actualTables == null)
            {
                Tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tables);
                var rows = LoyaltyCatalogBuild16042Fixture.Rewards();
                tables.SetProperty(nameof(IGameTableManager.RewardTrack), PlayerBindPointTests.Table(LoyaltyCatalogBuild16042Fixture.Tracks()));
                tables.SetProperty(nameof(IGameTableManager.RewardTrackRewards), PlayerBindPointTests.Table(rows));
                var ids = rows.SelectMany(row => new[] { (row.RewardTrackRewardTypeEnum00, row.RewardChoiceId00), (row.RewardTrackRewardTypeEnum01, row.RewardChoiceId01), (row.RewardTrackRewardTypeEnum02, row.RewardChoiceId02) })
                    .Where(pair => pair.Item1 == 0 && pair.Item2 != 0).Select(pair => pair.Item2).Distinct();
                tables.SetProperty(nameof(IGameTableManager.Item), PlayerBindPointTests.Table(ids.Select(id => new Item2Entry { Id = id }).ToArray()));
                tables.SetProperty(nameof(IGameTableManager.AccountItem), PlayerBindPointTests.Table(new AccountItemEntry { Id = 2485, AccountCurrencyEnum = 14, AccountCurrencyAmount = 5, EntitlementCount = 1 }, new AccountItemEntry { Id = 2486, AccountCurrencyEnum = 14, AccountCurrencyAmount = 7, EntitlementCount = 1 }));
                tables.SetProperty(nameof(IGameTableManager.AccountCurrencyType), PlayerBindPointTests.Table(new AccountCurrencyTypeEntry { Id = 14 }));
                tables.SetProperty(nameof(IGameTableManager.CurrencyType), PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1 }));
            }
            else Tables = actualTables;
            Awards = new(Tables); var award = Awards.GetAward(reward, choice);
            IItemInfo actualInfo = actualTables == null || award.Item == null ? null : new ItemInfo(Tables.Item.GetEntry(award.Item.ItemId), Tables);
            inventory = new(3, actualTables == null ? [8, 5] : [], providerOptions: character, contract: true,
                itemId: award.Item?.ItemId ?? 9011, actualInfo: actualInfo);
            var definition = Awards.Catalog.GetTrack(award.TrackId); var satisfied = definition.Slots.SelectMany(slot => slot.Candidates).Select(row => row.PrerequisiteId).Where(id => id != 0).ToHashSet(); int ordinal = 0;
            var selection = ContractRewardSelection.CaptureForChoose(Awards.Catalog, definition.Id, 7, Guid.NewGuid().ToString("N"), satisfied, count =>
            { byte current = (byte)ordinal++; return current == award.Ordinal ? Awards.Catalog.EligibleCandidates(definition.Id, current, satisfied).Select(row => row.Id).ToList().IndexOf(reward) : 0; });
            Before = new(42, definition.Id, 7, definition.MaximumPoints + 17, 1, true) { SelectionInitialized = true,
                Slots = Array.AsReadOnly(definition.Slots.Select(slot => new ContractRewardSlotState(slot.Ordinal, selection.Rewards[slot.Ordinal], slot.RequiredPoints, selection.Json)).ToArray()) };
            using (var context = inventory.Context())
            {
                context.CharacterChallengeRewardTrack.ExecuteDelete(); context.CharacterContractRewardTrack.ExecuteDelete();
                context.CharacterContractRewardTrack.Add(new() { Id = 42, TrackId = Before.TrackId, CycleId = 7, Points = Before.Points, Revision = 1, Active = true, SelectionInitialized = true });
                context.CharacterContractRewardSlot.AddRange(Before.Slots.Select(slot => new NexusForever.Database.Character.Model.CharacterContractRewardSlotModel { Id = 42, TrackId = Before.TrackId, CycleId = 7, Ordinal = slot.Ordinal, RewardRowId = slot.RewardRowId, RequiredPoints = slot.RequiredPoints, SelectionJson = slot.SelectionJson }));
                context.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 1234 }); context.SaveChanges();
            }
            using (var context = Auth()) { context.Database.EnsureCreated(); context.Account.Add(new() { Id = 27, Email = "contract-claim27@example.local" }); context.AccountCurrency.Add(new() { Id = 27, CurrencyId = 14, Amount = 100 }); context.SaveChanges(); }
            Quote = ContractRewardClaimQuote.Capture(Awards, 42, [Before], Before.TrackId, 7, 1, reward, choice);
            Command = CharacterChallengeRewardCommand.ClaimContract(42, 27, 1, Guid.NewGuid().ToString("N"), DateTime.UtcNow, Awards, [Before], Quote, award.CharacterCurrency == null ? null : new(true, 1234));
            Executor = new(Character, new CharacterChallengeRewardCatalog(Tables), Tables);
        }
        internal CharacterContext Character() => inventory.Context(CharacterFault);
        internal AuthContext Auth() => AuthFault == null ? new(authOptions) : new(new DbContextOptionsBuilder<AuthContext>(authOptions).AddInterceptors(AuthFault).Options);
        internal IInventoryRewardReservation Reserve() => Quote.Award.Item == null ? null : inventory.Reserve(Quote.Award.Item.Count);
        internal Task<CharacterChallengeRewardExecution> Execute(IInventoryRewardReservation reservation = null) => Executor.ExecuteAsync(Command, reservation);
        internal async Task<CharacterChallengeRewardsState> Load()
        {
            var result = await Executor.ExecuteAsync(CharacterChallengeRewardCommand.Load(42)); Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, result.Outcome); return result.State;
        }
        internal ContractAccountDelivery Sender(ushort realm = 1) => new(Character, Auth, Awards, realm);
        public void Dispose()
        {
            inventory.Dispose(); string path = Path.GetFullPath(directory);
            if (Path.GetDirectoryName(path) != Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) || !Path.GetFileName(path).StartsWith("NexusForever-ContractClaim-", StringComparison.Ordinal)) throw new InvalidOperationException("Unsafe claim fixture cleanup path.");
            Directory.Delete(path, true);
        }
    }

    internal sealed class CommitFault(bool after, bool acknowledge = false) : DbTransactionInterceptor
    {
        private int fired;
        private bool Target(DbContext context) => context is AuthContext auth ? auth.ChangeTracker.Entries<AccountCurrencyOperationModel>().Any()
            : context is CharacterContext character && (acknowledge ? character.ChangeTracker.Entries<NexusForever.Database.Character.Model.CharacterContractAccountOutboxModel>().Any(entry => entry.Entity.Delivered)
                : character.ChangeTracker.Entries<NexusForever.Database.Character.Model.CharacterContractRewardClaimModel>().Any());
        public override InterceptionResult TransactionCommitting(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result)
        { if (!after && Target(eventData.Context) && Interlocked.CompareExchange(ref fired, 1, 0) == 0) throw new InvalidOperationException("Owned before commit failure."); return result; }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) => new(TransactionCommitting(transaction, eventData, result));
        public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
        { if (after && Target(eventData.Context) && Interlocked.CompareExchange(ref fired, 1, 0) == 0) throw new InvalidOperationException("Owned lost commit acknowledgement."); }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default) { TransactionCommitted(transaction, eventData); return Task.CompletedTask; }
    }
}
