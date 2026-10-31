using System.Data;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Quest.Reward;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class InventoryRewardDeliveryTests
{
    private Fixture CreateContractFixture() => new(3, [8, 5], providerOptions: providerOptions, contract: true);

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task SuppressedExistingOrNewItemWritesCannotCommitBehindAnOtherwiseValidReceipt(bool contract, bool newItem)
    {
        using var f = contract ? CreateContractFixture() : CreateFixture(3, [8, 5]); using var held = f.Reserve(12);
        await using (var context = f.Context())
        await using (var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            var plan = contract ? f.StageContract(context, held) : f.Stage(context, held); await context.SaveChangesAsync();
            if (newItem)
            {
                ulong id = plan.Lines.Single(line => !line.ExistingLiveItem).After.Guid;
                await context.Item.Where(row => row.Id == id).ExecuteDeleteAsync();
            }
            else await context.Item.Where(row => row.Id == 101).ExecuteUpdateAsync(set => set.SetProperty(row => row.StackCount, 8u));
            await Assert.ThrowsAsync<InvalidOperationException>(() => tx.CommitAsync()); Assert.True(context.IsSaveRejected); await tx.RollbackAsync();
        }
        Assert.Equal(InventoryRewardReservationState.ReadyToRetry, await f.Resolve(held)); Assert.True(held.Cancel()); f.AssertNoPackets();
        await using var verify = f.Context(); Assert.Empty(verify.CharacterContractRewardClaim); Assert.Empty(verify.CharacterChallengeRewardClaim);
        Assert.Equal(new uint[] { 8, 5 }, verify.Item.OrderBy(row => row.BagIndex).Select(row => row.StackCount));
    }

    [Fact]
    public async Task ContractReceiptCannotUseAnotherPreparedAccountEvenWhenTheSqlCharacterMatchesIt()
    {
        using var f = CreateContractFixture(); var account = RecordingDispatchProxy<IAccount>.Create(out var proxy);
        proxy.SetProperty(nameof(IAccount.Id), 28u); proxy.SetProperty(nameof(IAccount.Session), f.Player.Session);
        f.PlayerProxy.SetProperty(nameof(IPlayer.Account), account); using var held = f.Reserve(12);
        await using var context = f.Context(); await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        Assert.Throws<InvalidOperationException>(() => f.StageContract(context, held)); Assert.True(context.IsSaveRejected);
        await tx.RollbackAsync(); f.AssertNoEffects();
    }

    [Fact]
    public void LegacyReservationImplementationsRefuseContractEnrollmentWithoutCallingTheirChallengeStage()
    {
        IInventoryRewardReservation legacy = new LegacyReservation();
        Assert.Throws<NotSupportedException>(() => legacy.StageContract(null, null));
    }

    private sealed class LegacyReservation : IInventoryRewardReservation
    {
        public InventoryRewardReservationState State => InventoryRewardReservationState.Prepared;
        public InventoryRewardDeliveryPlan Plan => null;
        public InventoryRewardDeliveryPlan Stage(CharacterContext context, CharacterChallengeRewardClaimModel receipt) => throw new InvalidOperationException("Challenge stage must not be used as a Contract fallback.");
        public InventoryRewardReservationState ResolveOutcome(CharacterContext context) => State;
        public bool PublishCommitted() => false;
        public bool Cancel() => true;
        public void Dispose() { }
    }

    [Fact]
    public async Task UnconnectedClaimAdoptionFailsClosedInsteadOfAdvertisingAClaimedRewardAsAvailable()
    {
        using var f = CreateContractFixture(); using var held = f.Reserve(12);
        await using (var context = f.Context())
        await using (var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            f.StageContract(context, held); await context.SaveChangesAsync(); await tx.CommitAsync();
        }
        await using var fresh = f.Context(); await using var read = await fresh.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        Assert.Throws<InvalidOperationException>(() => ContractRewardSnapshot.Read(fresh, 42)); f.AssertNoPackets();
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task ReceiptOrClaimedSlotSuppressionInSqlRejectsTheWholeItemDelivery(bool contract, bool receipt)
    {
        using var f = contract ? CreateContractFixture() : CreateFixture(3, [8, 5]); using var held = f.Reserve(12);
        await using (var context = f.Context())
        await using (var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            if (contract) f.StageContract(context, held); else f.Stage(context, held);
            await context.SaveChangesAsync();
            if (contract)
            {
                if (receipt) await context.CharacterContractRewardClaim.Where(row => row.Id == 42).ExecuteDeleteAsync();
                else await context.CharacterContractRewardSlot.Where(row => row.Id == 42).ExecuteUpdateAsync(set => set.SetProperty(row => row.Claimed, false));
            }
            else
            {
                if (receipt) await context.CharacterChallengeRewardClaim.Where(row => row.Id == 42).ExecuteDeleteAsync();
                else await context.CharacterChallengeRewardSlot.Where(row => row.Id == 42).ExecuteUpdateAsync(set => set.SetProperty(row => row.Claimed, false));
            }
            await Assert.ThrowsAsync<InvalidOperationException>(() => tx.CommitAsync()); Assert.True(context.IsSaveRejected); await tx.RollbackAsync();
        }
        Assert.Equal(InventoryRewardReservationState.ReadyToRetry, await f.Resolve(held)); Assert.True(held.Cancel()); f.AssertNoPackets();
        await using var verify = f.Context(); Assert.Empty(verify.CharacterContractRewardClaim); Assert.Empty(verify.CharacterChallengeRewardClaim);
        Assert.Equal(new uint[] { 8, 5 }, verify.Item.OrderBy(row => row.BagIndex).Select(row => row.StackCount));
    }

    [Fact]
    public async Task ContractDeliveryCommitsItsOwnReceiptAndAllStacksWithoutCreatingChallengeHistory()
    {
        using var f = CreateContractFixture(); using var held = f.Reserve(12);
        await using (var context = f.Context())
        await using (var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            var plan = f.StageContract(context, held); Assert.Equal(3, plan.Lines.Count); f.AssertNoPackets();
            await context.SaveChangesAsync(); Assert.False(held.PublishCommitted()); await tx.CommitAsync();
        }
        Assert.True(held.PublishCommitted()); await f.Save();
        Assert.Equal(new uint[] { 10, 10, 5 }, f.Bag.Select(item => item.StackCount));
        await using var read = f.Context(); var receipt = Assert.Single(read.CharacterContractRewardClaim);
        Assert.Equal(27u, receipt.AccountId); Assert.Equal(1u, receipt.QuoteVersion); Assert.Equal(1u, receipt.DeliveryVersion);
        Assert.Equal(12u, ContractRewardClaimQuote.Restore(Fixture.ContractCatalog(12), receipt.QuoteJson).Award.Item.Count);
        Assert.Equal(held.Plan.ToJson(), receipt.DeliveryJson); Assert.True(read.CharacterContractRewardSlot.Single().Claimed);
        Assert.Equal(7ul, read.CharacterContractRewardTrack.Single().CycleId); Assert.Equal(1000u, read.CharacterContractRewardTrack.Single().Points);
        Assert.Empty(read.CharacterChallengeRewardClaim); Assert.False(read.CharacterChallengeRewardSlot.Single().Claimed);
        Assert.Empty(read.CharacterCurrency); Assert.Empty(read.CharacterContractCompletion);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ContractLostAcknowledgementResolvesTheExactReceiptAndItemsWithoutRegranting(bool committed)
    {
        using var f = CreateContractFixture(); using var held = f.Reserve(12);
        await using (var context = f.Context(new CommitFailure(committed)))
        await using (var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            f.StageContract(context, held); await context.SaveChangesAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(() => tx.CommitAsync());
        }
        Assert.Equal(InventoryRewardReservationState.Uncertain, held.State); f.AssertNoPackets();
        Assert.Equal(committed ? InventoryRewardReservationState.Committed : InventoryRewardReservationState.ReadyToRetry, await f.Resolve(held));
        if (committed) { Assert.True(held.PublishCommitted()); Assert.False(held.PublishCommitted()); }
        else Assert.True(held.Cancel());
        await using var read = f.Context(); Assert.Equal(committed ? 1 : 0, read.CharacterContractRewardClaim.Count());
        Assert.Equal(committed, read.CharacterContractRewardSlot.Single().Claimed);
    }

    [Fact]
    public async Task ContractRollbackReusesKnownItemIdsAndItsRetainedTimeOnTheNextFreshUnit()
    {
        using var f = CreateContractFixture(); using var held = f.Reserve(12); ulong[] ids; DateTime created;
        await using (var context = f.Context())
        await using (var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            ids = f.StageContract(context, held).Lines.Select(line => line.After.Guid).ToArray(); created = context.CharacterContractRewardClaim.Local.Single().CreatedUtc;
            await context.SaveChangesAsync(); await tx.RollbackAsync();
        }
        Assert.Equal(InventoryRewardReservationState.ReadyToRetry, await f.Resolve(held));
        await using (var context = f.Context())
        await using (var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            Assert.Equal(ids, f.StageContract(context, held).Lines.Select(line => line.After.Guid));
            Assert.Equal(created, context.CharacterContractRewardClaim.Local.Single().CreatedUtc);
            await context.SaveChangesAsync(); await tx.CommitAsync();
        }
        Assert.True(held.PublishCommitted()); Assert.Single(f.ManagerProxy.GetInvocations("get_NextItemId"));
    }

    [Fact]
    public async Task AContractReservationCannotSwitchToAChallengeReceiptWithIdenticalOrdinalKeysAfterRollback()
    {
        using var f = CreateContractFixture(); using var held = f.Reserve(12);
        await using (var context = f.Context())
        await using (var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            f.StageContract(context, held); await context.SaveChangesAsync(); await tx.RollbackAsync();
        }
        Assert.Equal(InventoryRewardReservationState.ReadyToRetry, await f.Resolve(held));
        await using var retry = f.Context(); await using var transaction = await retry.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        retry.CharacterChallengeRewardTrack.Add(new() { Id = 42, TrackId = 5, CycleId = 7, Points = 1000, Revision = 1 });
        retry.CharacterChallengeRewardSlot.Add(new() { Id = 42, TrackId = 5, CycleId = 7, Ordinal = 0, RewardRowId = 17, RequiredPoints = 1000, Claimed = true });
        var receipt = new CharacterChallengeRewardClaimModel { Id = 42, TrackId = 5, CycleId = 7, Ordinal = 0, RewardRowId = 17,
            ClaimId = f.ContractClaimId, CreatedUtc = DateTime.UtcNow, DeliveryJson = "pending" };
        retry.CharacterChallengeRewardClaim.Add(receipt);
        Assert.Throws<InvalidOperationException>(() => held.Stage(retry, receipt)); Assert.True(retry.IsSaveRejected);
        await transaction.RollbackAsync(); Assert.Single(f.ManagerProxy.GetInvocations("get_NextItemId")); f.AssertNoPackets();
    }

    [Theory]
    [InlineData("account")] [InlineData("quote-version")] [InlineData("quote-empty")] [InlineData("identity")]
    [InlineData("timestamp")] [InlineData("slot-selection")] [InlineData("slot-reward")] [InlineData("slot-claimed")]
    [InlineData("delivery")] [InlineData("slot-original")]
    public async Task ContractReceiptAndSlotMutationRejectTheWholeUnitBeforeAnyItemCanPublish(string fault)
    {
        using var f = CreateContractFixture(); using var held = f.Reserve(12);
        await using (var context = f.Context())
        await using (var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            f.StageContract(context, held); var receipt = context.CharacterContractRewardClaim.Local.Single(); var slot = context.CharacterContractRewardSlot.Local.Single();
            switch (fault)
            {
                case "account": receipt.AccountId = 28; break;
                case "quote-version": receipt.QuoteVersion = 2; break;
                case "quote-empty": receipt.QuoteJson = ""; break;
                case "identity": receipt.ClaimId = Guid.NewGuid().ToString("N"); break;
                case "timestamp": receipt.CreatedUtc = receipt.CreatedUtc.AddSeconds(1); break;
                case "slot-selection": slot.SelectionJson += " "; break;
                case "slot-reward": slot.RewardRowId = 18; break;
                case "slot-claimed": slot.Claimed = false; break;
                case "delivery": receipt.DeliveryJson += " "; break;
                case "slot-original": context.Entry(slot).OriginalValues[nameof(slot.RequiredPoints)] = 999u; break;
            }
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync()); Assert.True(context.IsSaveRejected);
            await tx.RollbackAsync();
        }
        Assert.Equal(InventoryRewardReservationState.ReadyToRetry, await f.Resolve(held)); Assert.True(held.Cancel());
        await using var verify = f.Context(); Assert.Empty(verify.CharacterContractRewardClaim); Assert.False(verify.CharacterContractRewardSlot.Single().Claimed);
        Assert.Equal(new uint[] { 8, 5 }, verify.Item.OrderBy(row => row.BagIndex).Select(row => row.StackCount)); f.AssertNoPackets();
    }

    [Theory]
    [InlineData("account")] [InlineData("quote")] [InlineData("timestamp")] [InlineData("selection")]
    [InlineData("claimed")] [InlineData("delivery")] [InlineData("item")] [InlineData("owner")]
    public async Task ConflictingContractCommitWitnessQuarantinesEveryItemBeforePublication(string fault)
    {
        using var f = CreateContractFixture(); using var held = f.Reserve(12);
        await using (var context = f.Context(new CommitFailure(true)))
        await using (var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            f.StageContract(context, held); await context.SaveChangesAsync(); await Assert.ThrowsAsync<InvalidOperationException>(() => tx.CommitAsync());
        }
        await using (var external = f.Context())
        {
            if (fault is "account" or "quote" or "timestamp" or "delivery")
            {
                var receipt = external.CharacterContractRewardClaim.Single();
                if (fault == "account") receipt.AccountId++;
                if (fault == "quote") receipt.QuoteJson += " ";
                if (fault == "timestamp") receipt.CreatedUtc = receipt.CreatedUtc.AddSeconds(1);
                if (fault == "delivery") receipt.DeliveryJson += " ";
            }
            if (fault == "selection") external.CharacterContractRewardSlot.Single().SelectionJson += " ";
            if (fault == "claimed") external.CharacterContractRewardSlot.Single().Claimed = false;
            if (fault == "item") external.Item.Single(row => row.Id == 101).StackCount = 1;
            if (fault == "owner") external.Character.Single().AccountId++;
            await external.SaveChangesAsync();
        }
        Assert.Equal(InventoryRewardReservationState.Quarantined, await f.Resolve(held)); Assert.True(f.Inventory.RequiresAuthoritativeReload);
        Assert.False(held.PublishCommitted()); Assert.Equal(new uint[] { 8, 5 }, f.Bag.Select(item => item.StackCount)); f.AssertNoPackets();
    }

    [Theory]
    [InlineData("account")] [InlineData("zero-account")] [InlineData("quote-version")] [InlineData("quote-empty")]
    [InlineData("empty-id")] [InlineData("unselected")] [InlineData("unearned")] [InlineData("claimed")]
    public async Task ContractAdmissionRequiresTheActualAccountAndUnclaimedSelectedEarnedSource(string fault)
    {
        using var f = CreateContractFixture(); using var held = f.Reserve(12);
        if (fault is "unselected" or "unearned" or "claimed")
        {
            await using var external = f.Context();
            if (fault == "unselected") external.CharacterContractRewardTrack.Single().SelectionInitialized = false;
            if (fault == "unearned") external.CharacterContractRewardTrack.Single().Points = 999;
            if (fault == "claimed") external.CharacterContractRewardSlot.Single().Claimed = true;
            await external.SaveChangesAsync();
        }
        await using var context = f.Context(); await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        Assert.Throws<InvalidOperationException>(() => f.StageContract(context, held, receipt =>
        {
            if (fault == "account") receipt.AccountId = 28;
            if (fault == "zero-account") receipt.AccountId = 0;
            if (fault == "quote-version") receipt.QuoteVersion = 2;
            if (fault == "quote-empty") receipt.QuoteJson = "";
            if (fault == "empty-id") receipt.ClaimId = Guid.Empty.ToString("N");
        }));
        Assert.True(context.IsSaveRejected); await tx.RollbackAsync(); f.AssertNoEffects();
    }

    [Fact]
    public async Task ChallengeRecoveryAlsoRejectsAChangedDurableReceiptTimestamp()
    {
        using var f = CreateFixture(3, [8, 5]); using var held = f.Reserve(12);
        await using (var context = f.Context(new CommitFailure(true)))
        await using (var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            f.Stage(context, held); await context.SaveChangesAsync(); await Assert.ThrowsAsync<InvalidOperationException>(() => tx.CommitAsync());
        }
        await using (var external = f.Context()) { external.CharacterChallengeRewardClaim.Single().CreatedUtc = DateTime.UtcNow.AddHours(1); await external.SaveChangesAsync(); }
        Assert.Equal(InventoryRewardReservationState.Quarantined, await f.Resolve(held)); Assert.False(held.PublishCommitted()); f.AssertNoPackets();
    }

    internal sealed partial class Fixture
    {
        internal string ContractClaimId => claimId;
        internal static ContractRewardAwardCatalog ContractCatalog(uint count)
        {
            var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
            proxy.SetProperty(nameof(IGameTableManager.RewardTrack), PlayerBindPointTests.Table(new RewardTrackEntry { Id = 5, RewardTrackTypeEnum = 1, RewardPointCost00 = 1000 }));
            proxy.SetProperty(nameof(IGameTableManager.RewardTrackRewards), PlayerBindPointTests.Table(new RewardTrackRewardsEntry
                { Id = 17, RewardTrackId = 5, RewardPointFlags = 1, Flags = 3, RewardChoiceId00 = 9011, RewardChoiceCount00 = count }));
            proxy.SetProperty(nameof(IGameTableManager.Item), PlayerBindPointTests.Table(new Item2Entry { Id = 9011 }));
            return new(tables);
        }
        private static ContractRewardSelection ContractSelection(ContractRewardAwardCatalog catalog) => ContractRewardSelection.CaptureForChoose(catalog.Catalog, 5, 7,
            "123456781234123412341234567890ab", [], _ => 0);
        private static void SeedContract(CharacterContext context)
        {
            var selection = ContractSelection(ContractCatalog(1));
            context.CharacterContractRewardTrack.Add(new() { Id = 42, TrackId = 5, CycleId = 7, Points = 1000, Revision = 1, Active = true, SelectionInitialized = true });
            context.CharacterContractRewardSlot.Add(new() { Id = 42, TrackId = 5, CycleId = 7, Ordinal = 0, RewardRowId = 17, RequiredPoints = 1000, SelectionJson = selection.Json });
        }
        internal InventoryRewardDeliveryPlan StageContract(CharacterContext context, IInventoryRewardReservation held, Action<CharacterContractRewardClaimModel> mutate = null)
        {
            var catalog = ContractCatalog(held.Count); var selection = ContractSelection(catalog);
            var track = new ContractTrackState(42, 5, 7, 1000, 1, true) { SelectionInitialized = true, Slots = [new(0, 17, 1000, selection.Json)] };
            var quote = ContractRewardClaimQuote.Capture(catalog, 42, [track], 5, 7, 1, 17, 0);
            context.CharacterContractRewardSlot.Single().Claimed = true; context.CharacterContractRewardTrack.Single().Revision++;
            var receipt = new CharacterContractRewardClaimModel { Id = 42, TrackId = 5, CycleId = 7, Ordinal = 0, RewardRowId = 17,
                ClaimId = claimId, AccountId = 27, CreatedUtc = DateTime.UtcNow, QuoteVersion = 1, QuoteJson = quote.Json, DeliveryJson = "pending" };
            context.CharacterContractRewardClaim.Add(receipt); mutate?.Invoke(receipt); return held.StageContract(context, receipt);
        }
    }
}
