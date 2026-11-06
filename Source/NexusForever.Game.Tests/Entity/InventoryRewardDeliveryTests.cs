using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Entity;
using NexusForever.Game.Marketplace;
using NexusForever.Shared.Game;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Static;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class InventoryRewardDeliveryTests
{
    private readonly DbContextOptions<CharacterContext> providerOptions;
    public InventoryRewardDeliveryTests() { }
    internal InventoryRewardDeliveryTests(DbContextOptions<CharacterContext> providerOptions) => this.providerOptions = providerOptions;
    private Fixture CreateFixture(uint capacity, uint[] stacks, bool expiring = false) => new(capacity, stacks, expiring, providerOptions);

    [Fact]
    public async Task WholeDeliveryMergesBeforeNewStacksAndPublishesOnlyAfterCommit()
    {
        using var f = CreateFixture(3, [8, 5]);
        using var held = f.Reserve(12);
        Assert.Null(held.Plan);
        Assert.Equal(new uint[] { 8, 5 }, f.Bag.Select(item => item.StackCount));
        Assert.Equal(1u, f.Bag.SlotsRemaining);
        Assert.Equal(0u, f.Bag.AvailableSlots);
        f.AssertNoEffects();
        await using (var context = f.Context())
        await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            var plan = f.Stage(context, held);
            Assert.Equal(new uint[] { 2, 5, 5 }, plan.Lines.Select(line => line.AwardedCount));
            Assert.Equal(new uint[] { 10, 10, 5 }, plan.Lines.Select(line => line.After.StackCount));
            Assert.Single(f.ManagerProxy.GetInvocations("get_NextItemId"));
            Assert.False(held.PublishCommitted());
            await context.SaveChangesAsync();
            Assert.Equal(new uint[] { 8, 5 }, f.Bag.Select(item => item.StackCount));
            Assert.Empty(f.Messages);
            Assert.False(held.PublishCommitted());
            await transaction.CommitAsync();
        }
        Assert.Equal(InventoryRewardReservationState.Committed, held.State);
        Assert.True(held.PublishCommitted());
        Assert.False(held.PublishCommitted());
        Assert.False(f.Inventory.HasPendingRewardDelivery);
        Assert.Equal(new uint[] { 10, 10, 5 }, f.Bag.Select(item => item.StackCount));
        Assert.Equal(2, f.Messages.OfType<ServerItemStackCountUpdate>().Count());
        Assert.True(Assert.Single(f.Messages.OfType<ServerItemAdd>()).InventoryItem.Item.DynamicFlags.HasFlag(
            NexusForever.Game.Static.Item.DynamicItemFlags.Soulbound));
        Assert.Single(f.QuestProxy.GetInvocations(nameof(IQuestManager.GetActiveQuests)));
        await f.Save();
        await using var verify = f.Context();
        Assert.Equal(new uint[] { 10, 10, 5 }, await verify.Item.OrderBy(row => row.BagIndex).Select(row => row.StackCount).ToArrayAsync());
        Assert.Equal(1, await verify.CharacterChallengeRewardClaim.CountAsync());
        Assert.True((await verify.CharacterChallengeRewardSlot.SingleAsync()).Claimed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CapacityOrBindingFailureHasNoIdsPacketsOrPartialReservations(bool unbound)
    {
        using var f = CreateFixture(1, [9]);
        if (unbound) f.ReplaceWithUnbound();
        Assert.False(f.Inventory.TryReserveSoulboundCreation(InventoryLocation.Inventory, f.Info, 2, out var held, out var error));
        Assert.Null(held);
        Assert.Equal(GenericError.ItemInventoryFull, error);
        Assert.False(f.Inventory.HasPendingRewardDelivery);
        Assert.Equal(9u, Assert.Single(f.Bag).StackCount);
        f.AssertNoEffects();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SaveAndMutationLeasesPreventPreparationUntilReleased(bool save)
    {
        using var f = CreateFixture(2, [3]);
        IDisposable lease;
        Assert.True(save ? f.Inventory.TryAcquireSaveLease(out lease) : f.Inventory.TryAcquireMutationLease(out lease));
        using (lease)
        {
            Assert.False(f.Inventory.TryReserveSoulboundCreation(InventoryLocation.Inventory, f.Info, 4, out _, out _));
            f.AssertNoEffects();
        }
        lease.Dispose();
        using var held = f.Reserve(4);
        Assert.False(f.Inventory.TryAcquireSaveLease(out _));
        Assert.False(f.Inventory.TryAcquireMutationLease(out _));
        Assert.True(held.Cancel());
        Assert.True(f.Inventory.TryAcquireSaveLease(out var after));
        after.Dispose();
    }

    [Fact]
    public async Task RollbackCannotCancelUntilFreshReceiptAndAllItemStatesAreResolved()
    {
        using var f = CreateFixture(3, [3]);
        using var held = f.Reserve(18);
        await using (var context = f.Context())
        await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            f.Stage(context, held);
            await context.SaveChangesAsync();
            held.Dispose();
            Assert.True(f.Inventory.HasPendingRewardDelivery);
            await transaction.RollbackAsync();
        }
        Assert.Equal(InventoryRewardReservationState.Uncertain, held.State);
        Assert.False(held.Cancel());
        Assert.Equal(InventoryRewardReservationState.ReadyToRetry, await f.Resolve(held));
        Assert.True(held.Cancel());
        Assert.Equal(3u, Assert.Single(f.Bag).StackCount);
        Assert.False(f.Inventory.HasPendingRewardDelivery);
        f.AssertNoPackets();
        await f.Save();
        await using var verify = f.Context();
        Assert.Equal(3u, (await verify.Item.SingleAsync()).StackCount);
        Assert.Empty(await verify.CharacterChallengeRewardClaim.ToArrayAsync());
    }

    [Fact]
    public async Task PartialIdAllocationFailureRejectsWholeUnitAndReusesEveryKnownIdOnRetry()
    {
        using var f = CreateFixture(3, []);
        int requests = 0;
        f.ManagerProxy.SetMethodReturnFactory("get_NextItemId", () => ++requests switch
            { 1 => 500ul, 2 => throw new InvalidOperationException("Owned second allocation failure."), 3 => 501ul, _ => 502ul });
        using var held = f.Reserve(25);
        await using (var context = f.Context())
        await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            Assert.Throws<InvalidOperationException>(() => f.Stage(context, held));
            Assert.True(context.IsSaveRejected);
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
            await transaction.RollbackAsync();
        }
        Assert.Equal(InventoryRewardReservationState.ReadyToRetry, await f.Resolve(held));
        await using (var retry = f.Context())
        await using (var transaction = await retry.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            var plan = f.Stage(retry, held);
            Assert.Equal(new ulong[] { 500, 501, 502 }, plan.Lines.Select(line => line.After.Guid));
            await retry.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        Assert.True(held.PublishCommitted());
        Assert.Equal(4, requests);
        Assert.Equal(new uint[] { 10, 10, 5 }, f.Bag.Select(item => item.StackCount));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LostAcknowledgementResolvesTheWholeMultiItemReceiptWithoutAddingAgain(bool afterCommit)
    {
        using var f = CreateFixture(3, [8, 5]);
        using var held = f.Reserve(12);
        await using (var context = f.Context(new CommitFailure(afterCommit)))
        await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            f.Stage(context, held);
            await context.SaveChangesAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
        }
        Assert.Equal(InventoryRewardReservationState.Uncertain, held.State);
        Assert.Equal(new uint[] { 8, 5 }, f.Bag.Select(item => item.StackCount));
        Assert.Equal(afterCommit ? InventoryRewardReservationState.Committed : InventoryRewardReservationState.ReadyToRetry, await f.Resolve(held));
        if (afterCommit)
        {
            Assert.True(held.PublishCommitted());
            await f.Save();
            Assert.Equal(new uint[] { 10, 10, 5 }, f.Bag.Select(item => item.StackCount));
        }
        else Assert.True(held.Cancel());
    }

    [Fact]
    public async Task OneConflictingItemQuarantinesAllBeforeAnyPartCanPublish()
    {
        using var f = CreateFixture(3, [8, 5]);
        using var held = f.Reserve(12);
        await using (var context = f.Context(new CommitFailure(true)))
        await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            f.Stage(context, held);
            await context.SaveChangesAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
        }
        await using (var external = f.Context()) await external.Database.ExecuteSqlRawAsync("UPDATE item SET stackCount=1 WHERE id=102");
        Assert.Equal(InventoryRewardReservationState.Quarantined, await f.Resolve(held));
        Assert.True(f.Inventory.RequiresAuthoritativeReload);
        Assert.False(held.PublishCommitted());
        Assert.Equal(new uint[] { 8, 5 }, f.Bag.Select(item => item.StackCount));
        Assert.Throws<InvalidOperationException>(() => f.Bag.GetItem(0u).StackCount = 9);
        await using var stale = f.Context();
        Assert.Throws<InvalidOperationException>(() => f.Inventory.Save(stale));
        Assert.True(stale.IsSaveRejected);
        f.AssertNoPackets();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeferredExpirationPreservesElapsedTimeOnCommitOrPreparedCancellation(bool commit)
    {
        using var f = CreateFixture(2, [3], expiring: true);
        using var held = f.Reserve(4);
        f.Inventory.Update(.4);
        f.Inventory.Update(.6);
        f.Inventory.Update(2);
        Assert.Equal(120u, Assert.Single(f.Bag).ExpirationTimeLeft);
        if (commit)
        {
            await using (var context = f.Context())
            await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
            {
                f.Stage(context, held);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            Assert.True(held.PublishCommitted());
        }
        else Assert.True(held.Cancel());
        Assert.Equal(117u, Assert.Single(f.Bag).ExpirationTimeLeft);
        await f.Save();
        await using var verify = f.Context();
        var row = await verify.Item.AsNoTracking().SingleAsync();
        Assert.Equal(117u, row.ExpirationTimeLeft);
        Assert.Equal(commit ? 7u : 3u, row.StackCount);
    }

    [Fact]
    public async Task AlreadyExpiredMergeIsRemovedAfterAdoptionAndKeepsItsReceiptWitness()
    {
        using var f = CreateFixture(2, [3], expiring: true);
        using var held = f.Reserve(4);
        f.Inventory.Update(121);
        await using (var context = f.Context())
        await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            f.Stage(context, held);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        Assert.True(held.PublishCommitted());
        Assert.Empty(f.Bag);
        Assert.IsType<ServerItemStackCountUpdate>(f.Messages.First());
        Assert.IsType<ServerItemDelete>(f.Messages.Last());
        await f.Save();
        await using var verify = f.Context();
        Assert.Empty(await verify.Item.ToArrayAsync());
        Assert.Equal(1, await verify.CharacterChallengeRewardClaim.CountAsync());
    }

    [Fact]
    public async Task InvalidatedOwnerAdoptsKnownCommitAfterAllStateOwnersSettleWithoutPackets()
    {
        using var f = CreateFixture(2, [3]);
        using var held = f.Reserve(4);
        bool laterOwnerSettled = false;
        await using (var context = f.Context())
        await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            f.Stage(context, held);
            context.OnSaveCompleted(_ => { Assert.Equal(3u, f.Bag.GetItem(0u).StackCount); laterOwnerSettled = true; });
            f.Inventory.InvalidateRewardDeliveries();
            f.Bag.GetItem(0u).Durability = .6f;
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        Assert.True(laterOwnerSettled);
        Assert.Equal(InventoryRewardReservationState.Published, held.State);
        Assert.Equal(7u, Assert.Single(f.Bag).StackCount);
        f.AssertNoPackets();
        Assert.False(f.Inventory.RequiresAuthoritativeReload);
        await f.Save();
        await using var verify = f.Context();
        Assert.Equal(.6f, (await verify.Item.SingleAsync()).Durability);
    }

    [Fact]
    public async Task ReentrantInvalidationStopsThePacketTailAfterAllInventoryBookkeeping()
    {
        using var f = CreateFixture(3, [8, 5]);
        using var held = f.Reserve(12);
        await f.Commit(held);
        f.SessionProxy.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), arguments =>
        {
            Assert.Equal(new uint[] { 10, 10, 5 }, f.Bag.Select(item => item.StackCount));
            f.Inventory.InvalidateRewardDeliveries();
            return null;
        });
        Assert.True(held.PublishCommitted());
        Assert.Single(f.Messages);
        Assert.Empty(f.QuestProxy.GetInvocations(nameof(IQuestManager.GetActiveQuests)));
        Assert.Equal(InventoryRewardReservationState.Published, held.State);
    }

    [Fact]
    public async Task NotificationFailureCannotReopenTheCommittedClaimOrUndoAdoption()
    {
        using var f = CreateFixture(3, [8, 5]);
        using var held = f.Reserve(12);
        await f.Commit(held);
        f.SessionProxy.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), arguments => throw new InvalidOperationException("Owned send failure."));
        Assert.Throws<InvalidOperationException>(() => held.PublishCommitted());
        Assert.Equal(InventoryRewardReservationState.Published, held.State);
        Assert.False(held.PublishCommitted());
        Assert.False(f.Inventory.RequiresAuthoritativeReload);
        Assert.Equal(new uint[] { 10, 10, 5 }, f.Bag.Select(item => item.StackCount));
        await f.Save();
    }

    [Fact]
    public async Task PlayerDirectAndDeferredSavesStopBeforeAuthWhileDeliveryIsPending()
    {
        using var f = CreateFixture(2, [3]);
        using var held = f.Reserve(4);
        var player = BarePlayer(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => player.SaveDirect());
        int success = 0, failure = 0;
        MethodInfo save = typeof(Player).GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Instance,
            [typeof(Action), typeof(Action)])!;
        save.Invoke(player, [new Action(() => ++success), new Action(() => ++failure)]);
        save.Invoke(player, [new Action(() => ++success), new Action(() => ++failure)]);
        Assert.Equal(0, success);
        Assert.Equal(0, failure);
        Assert.True((bool)typeof(Player).GetField("forceSave", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(player)!);
        Assert.False((bool)typeof(Player).GetField("saveInProgress", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(player)!);
        Assert.Empty(f.SessionProxy.GetInvocations(nameof(IGameSession.ForceDisconnect)));
        Assert.True(held.Cancel());
        // No database manager is configured: the first permitted attempt fails at
        // Auth lookup and must release its lease and all deferred failure callbacks.
        var error = Assert.Throws<TargetInvocationException>(() => save.Invoke(player,
            [new Action(() => ++success), new Action(() => ++failure)]));
        Assert.IsType<InvalidOperationException>(error.InnerException);
        Assert.Equal(3, failure);
        Assert.Equal(0, success);
        Assert.False(f.Inventory.IsSaveBlocked);
        using var next = f.Reserve(4);
    }

    [Fact]
    public async Task DirectSaveFailureReleasesTheLogicalLeaseWithoutStartingCharacterWork()
    {
        using var f = CreateFixture(2, [3]);
        var player = BarePlayer(f);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => player.SaveDirect());
        Assert.Contains("AuthDatabase", error.Message);
        Assert.False(f.Inventory.IsSaveBlocked);
        using var held = f.Reserve(4);
    }

    [Fact]
    public void DirectContextEnrollmentRejectsBeforeAuthAndPoisonsCharacterUnit()
    {
        using var f = CreateFixture(2, [3]);
        using var held = f.Reserve(4);
        var player = BarePlayer(f);
        using var auth = new AuthContext(new DbContextOptionsBuilder<AuthContext>().UseSqlite("Data Source=:memory:").Options);
        using var character = f.Context();
        Assert.Throws<InvalidOperationException>(() => player.Save(auth));
        Assert.Empty(auth.ChangeTracker.Entries());
        Assert.Throws<InvalidOperationException>(() => player.Save(character));
        Assert.True(character.IsSaveRejected);
        Assert.Empty(character.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData("send")]
    [InlineData("attachment")]
    [InlineData("cash")]
    [InlineData("cod")]
    public void MailRejectsBeforeWalletAttachmentOrCharacterLookupEffects(string operation)
    {
        using var f = CreateFixture(2, [3]);
        using var held = f.Reserve(4);
        var mail = (MailManager)RuntimeHelpers.GetUninitializedObject(typeof(MailManager));
        typeof(MailManager).GetField("player", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(mail, f.Player);
        switch (operation)
        {
            case "send": mail.SendMail(new ClientMailSend()); break;
            case "attachment": mail.MailTakeAttachment(1, 0, 1); break;
            case "cash": mail.MailTakeCash(1, 1); break;
            case "cod": mail.MailPayCod(1); break;
        }
        object response = Assert.Single(f.Messages);
        if (response is ServerMailResult result) Assert.Equal(GenericError.MailBusy, result.Result);
        else Assert.Equal(GenericError.MailBusy, Assert.IsType<ServerMailTakeAttachment>(response).Result);
        Assert.Empty(f.PlayerProxy.GetInvocations("get_CurrencyManager"));
        Assert.Equal(3u, Assert.Single(f.Bag).StackCount);
    }

    [Fact]
    public void MarketplaceRejectsBeforeFeesItemRemovalOrListingMutation()
    {
        using var f = CreateFixture(2, [3]);
        using var held = f.Reserve(4);
        var market = new GlobalMarketplaceManager();
        Assert.Equal(GenericError.ItemNotValidForSlot, market.PostAuction(f.Player, f.Bag.GetItem(0u), 1, 2, out _));
        Assert.Equal(GenericError.ItemNotValidForSlot, market.CancelAuction(f.Player, 1, 9011, out _));
        Assert.Empty(f.PlayerProxy.GetInvocations("get_CurrencyManager"));
        Assert.Equal(3u, Assert.Single(f.Bag).StackCount);
        f.AssertNoEffects();
    }

    [Fact]
    public async Task PublicationBlocksNewClaimsUntilTheWholePacketBatchHasBeenQueued()
    {
        using var f = CreateFixture(4, [8, 5]);
        using var first = f.Reserve(12);
        await f.Commit(first);
        bool reentered = false;
        f.SessionProxy.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), arguments =>
        {
            if (reentered) return null;
            reentered = true;
            Assert.False(f.Inventory.TryReserveSoulboundCreation(InventoryLocation.Inventory, f.Info, 1, out _, out _));
            return null;
        });
        Assert.True(first.PublishCommitted());
        Assert.Equal(3, f.Messages.Count());
        using var second = f.Reserve(1);
        // A second synthetic durable cycle/slot is a different receipt, not a replay.
        using (var context = f.Context())
        {
            var track = context.CharacterChallengeRewardTrack.Single();
            track.CycleId = 8; ++track.Revision;
            context.CharacterChallengeRewardSlot.Add(new CharacterChallengeRewardSlotModel
                { Id = 42, TrackId = 15, CycleId = 8, Ordinal = 0, RewardRowId = 315, RequiredPoints = 100 });
            context.SaveChanges();
        }
        {
            using var unit = f.Context();
            using var transaction = unit.Database.BeginTransaction(IsolationLevel.Serializable);
            unit.CharacterChallengeRewardSlot.Single(row => row.CycleId == 8).Claimed = true;
            var receipt = new CharacterChallengeRewardClaimModel
                { Id = 42, TrackId = 15, CycleId = 8, Ordinal = 0, RewardRowId = 315, ClaimId = Guid.NewGuid().ToString("N"), CreatedUtc = DateTime.UtcNow, DeliveryJson = "pending" };
            unit.CharacterChallengeRewardClaim.Add(receipt);
            second.Stage(unit, receipt);
            unit.SaveChanges(); transaction.Commit();
            Assert.True(second.PublishCommitted());
        }
        Assert.Equal(4, f.Messages.Count());
        Assert.IsType<ServerItemAdd>(f.Messages.ElementAt(2));
        Assert.IsType<ServerItemStackCountUpdate>(f.Messages.Last());
        Assert.Equal(26u, f.Inventory.GetItemCount(9011));
    }

    [Fact]
    public void ReentrantPreparationCannotEnterAnUnfinishedOrdinaryCreationOperation()
    {
        using var f = CreateFixture(3, [3]);
        bool called = false;
        f.SessionProxy.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), arguments =>
        {
            called = true;
            Assert.False(f.Inventory.TryReserveSoulboundCreation(InventoryLocation.Inventory, f.Info, 1, out _, out _));
            return null;
        });
        f.Inventory.ItemCreate(InventoryLocation.Inventory, f.Info, 1);
        Assert.True(called);
        using var held = f.Reserve(1);
    }

    [Theory]
    [InlineData("payload")]
    [InlineData("claimed")]
    [InlineData("cost")]
    [InlineData("slot-unchanged")]
    [InlineData("receipt-detached")]
    [InlineData("item-unchanged")]
    public async Task ChangedEnrollmentCannotCommitAnyPartOfTheDelivery(string mutation)
    {
        using var f = CreateFixture(3, [3]);
        using var held = f.Reserve(18);
        await using (var context = f.Context())
        await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            f.Stage(context, held);
            switch (mutation)
            {
                case "payload": context.ChangeTracker.Entries<CharacterChallengeRewardClaimModel>().Single().Entity.DeliveryJson = "changed"; break;
                case "claimed": context.ChangeTracker.Entries<CharacterChallengeRewardSlotModel>().Single().Entity.Claimed = false; break;
                case "cost": context.ChangeTracker.Entries<CharacterChallengeRewardSlotModel>().Single().Entity.RequiredPoints = 0; break;
                case "slot-unchanged": context.ChangeTracker.Entries<CharacterChallengeRewardSlotModel>().Single().State = EntityState.Unchanged; break;
                case "receipt-detached": context.ChangeTracker.Entries<CharacterChallengeRewardClaimModel>().Single().State = EntityState.Detached; break;
                case "item-unchanged": context.ChangeTracker.Entries<ItemModel>().First().State = EntityState.Unchanged; break;
            }
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
            Assert.True(context.IsSaveRejected);
            await transaction.RollbackAsync();
        }
        Assert.Equal(InventoryRewardReservationState.ReadyToRetry, await f.Resolve(held));
        Assert.True(held.Cancel());
        Assert.Equal(3u, Assert.Single(f.Bag).StackCount);
        f.AssertNoPackets();
        await using var verify = f.Context();
        Assert.Empty(await verify.CharacterChallengeRewardClaim.ToArrayAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SavepointOrUnflushedCommitCannotReleaseTheReservationAsSuccessful(bool savepoint)
    {
        using var f = CreateFixture(3, [3]);
        using var held = f.Reserve(18);
        await using (var context = f.Context())
        await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            if (savepoint) await transaction.CreateSavepointAsync("before");
            f.Stage(context, held);
            if (savepoint)
            {
                await context.SaveChangesAsync();
                await transaction.RollbackToSavepointAsync("before");
            }
            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
            Assert.True(context.IsSaveRejected);
            await transaction.RollbackAsync();
        }
        Assert.Equal(InventoryRewardReservationState.ReadyToRetry, await f.Resolve(held));
        Assert.True(held.Cancel());
        f.AssertNoPackets();
    }

    [Theory]
    [InlineData("stackability")]
    [InlineData("charges")]
    [InlineData("expiration")]
    public async Task ChangedDefinitionsRejectBeforeAllocatingIdsAndRequireSafeRecovery(string mutation)
    {
        using var f = CreateFixture(3, [3]);
        using var held = f.Reserve(18);
        if (mutation == "stackability") f.InfoProxy.SetMethodReturn(nameof(IItemInfo.IsStackable), false);
        if (mutation == "charges") f.Info.Entry.MaxCharges = 8;
        if (mutation == "expiration") f.Info.Entry.ExpirationTimeMinutes = 2;
        await using (var context = f.Context())
        await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            Assert.Throws<InvalidOperationException>(() => f.Stage(context, held));
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
            await transaction.RollbackAsync();
        }
        Assert.Equal(InventoryRewardReservationState.ReadyToRetry, await f.Resolve(held));
        Assert.True(held.Cancel());
        f.AssertNoEffects();
    }

    [Fact]
    public async Task PublicationPayloadFailureQuarantinesBeforeAnyLiveQuantityIsChanged()
    {
        using var f = CreateFixture(3, [3]);
        using var held = f.Reserve(18);
        await f.Commit(held);
        int calls = 0;
        f.InfoProxy.SetMethodReturnFactory("get_Id", () => ++calls < 4 ? 9011u : throw new InvalidOperationException("Owned payload lookup failure."));
        Assert.Throws<InvalidOperationException>(() => held.PublishCommitted());
        Assert.True(f.Inventory.RequiresAuthoritativeReload);
        Assert.Equal(InventoryRewardReservationState.Quarantined, held.State);
        Assert.Equal(3u, Assert.Single(f.Bag).StackCount);
        f.AssertNoPackets();
        await using var verify = f.Context();
        Assert.Equal(21u, (uint)await verify.Item.SumAsync(row => row.StackCount));
    }

    [Fact]
    public async Task TombstoneSettlementWaitsForTheInventoryMutationGate()
    {
        using var f = CreateFixture(2, [3]);
        f.Inventory.ItemDelete(new NexusForever.Network.World.Message.Model.Shared.ItemLocation
            { Location = InventoryLocation.Inventory, BagIndex = 0 });
        await using var context = f.Context();
        var callbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        context.OnSaveCompleted(_ => callbackEntered.TrySetResult());
        f.Inventory.Save(context);
        object gate = typeof(Inventory).GetProperty("MutationGate", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(f.Inventory)!;
        var locked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        Task actor = Task.Run(() =>
        {
            lock (gate)
            {
                locked.TrySetResult();
                if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Owned actor gate was not released.");
            }
        });
        await locked.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Task flush = Task.Run(() => context.SaveChangesAsync());
        try
        {
            await callbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.NotSame(flush, await Task.WhenAny(flush, Task.Delay(100)));
        }
        finally
        {
            release.Set();
            await actor.WaitAsync(TimeSpan.FromSeconds(10));
            await flush.WaitAsync(TimeSpan.FromSeconds(10));
        }
        Assert.Empty((List<IItem>)typeof(Inventory).GetField("deletedItems", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(f.Inventory)!);
        await using var verify = f.Context();
        Assert.Empty(await verify.Item.ToArrayAsync());
    }

    private static Player BarePlayer(Fixture f)
    {
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        typeof(Player).GetProperty(nameof(Player.Identity))!.SetValue(player, new Identity { RealmId = 1, Id = 42 });
        typeof(Player).GetProperty(nameof(Player.Inventory))!.SetValue(player, f.Inventory);
        typeof(Player).GetProperty(nameof(Player.Session))!.SetValue(player,
            f.Player.Session);
        typeof(Player).GetProperty(nameof(Player.Account))!.SetValue(player, f.Player.Account);
        typeof(Player).GetField("saveTimer", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(player, new UpdateTimer(1000));
        return player;
    }

    internal sealed partial class Fixture : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "NexusForever-InventoryReward-" + Guid.NewGuid().ToString("N"));
        private readonly DbContextOptions<CharacterContext> options;
        private readonly string claimId = Guid.NewGuid().ToString("N");
        public Inventory Inventory { get; } = (Inventory)RuntimeHelpers.GetUninitializedObject(typeof(Inventory));
        public Bag Bag { get; }
        public IPlayer Player { get; }
        public RecordingDispatchProxy<IPlayer> PlayerProxy { get; }
        public RecordingDispatchProxy<IItemInfo> InfoProxy { get; }
        public IItemInfo Info { get; }
        public IItemManager Manager { get; }
        public RecordingDispatchProxy<IItemManager> ManagerProxy { get; }
        public RecordingDispatchProxy<IGameSession> SessionProxy { get; }
        public RecordingDispatchProxy<IQuestManager> QuestProxy { get; }
        public IEnumerable<object> Messages => SessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]);

        public Fixture(uint capacity, uint[] stacks, bool expiring = false, DbContextOptions<CharacterContext> providerOptions = null, bool contract = false, uint itemId = 9011, IItemInfo actualInfo = null, bool loadExisting = false)
        {
            Directory.CreateDirectory(directory);
            options = providerOptions ?? new DbContextOptionsBuilder<CharacterContext>().UseSqlite($"Data Source={Path.Combine(directory, "delivery.sqlite")};Pooling=False").Options;
            var session = RecordingDispatchProxy<IGameSession>.Create(out var sp); SessionProxy = sp;
            IAccount account = RecordingDispatchProxy<IAccount>.Create(out var ap); ap.SetProperty(nameof(IAccount.Session), session);
            ap.SetProperty(nameof(IAccount.Id), contract ? 27u : 0u);
            IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var pp); Player = player; PlayerProxy = pp;
            pp.SetProperty(nameof(IPlayer.CharacterId), 42ul); pp.SetProperty(nameof(IPlayer.Account), account);
            pp.SetProperty(nameof(IPlayer.Session), session); pp.SetProperty(nameof(IPlayer.Inventory), Inventory);
            pp.SetProperty(nameof(IPlayer.IsLoading), false);
            IQuestManager quests = RecordingDispatchProxy<IQuestManager>.Create(out var qp); QuestProxy = qp;
            qp.SetMethodReturn(nameof(IQuestManager.GetActiveQuests), Array.Empty<IQuest>()); pp.SetProperty(nameof(IPlayer.QuestManager), quests);
            if (actualInfo == null)
            {
                Info = RecordingDispatchProxy<IItemInfo>.Create(out var ip); InfoProxy = ip;
                ip.SetMethodReturnFactory("get_Id", () => itemId);
                ip.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = itemId, MaxStackCount = 10, MaxCharges = 5, ExpirationTimeMinutes = expiring ? 2u : 0u });
                ip.SetMethodReturn(nameof(IItemInfo.IsStackable), true);
            }
            else
            {
                Assert.Equal(itemId, actualInfo.Id);
                Info = actualInfo;
            }
            Manager = RecordingDispatchProxy<IItemManager>.Create(out var mp); ManagerProxy = mp;
            mp.SetMethodReturn(nameof(IItemManager.GetItemInfo), Info);
            ulong next = 500; mp.SetMethodReturnFactory("get_NextItemId", () => next++);
            Bag = new Bag(InventoryLocation.Inventory, capacity, Manager);
            Set("characterId", 42ul); Set("player", player); Set("itemManager", Manager);
            Set("bags", new Dictionary<InventoryLocation, IBag> { [InventoryLocation.Inventory] = Bag }); Set("deletedItems", new List<IItem>());
            using var context = Context();
            if (!loadExisting)
            {
                context.Database.EnsureCreated();
                context.Character.Add(new CharacterModel { Id = 42, AccountId = contract ? 27u : 0u, Name = "Inventory reward owner" });
                if (contract) SeedContract(context);
                context.CharacterChallengeRewardTrack.Add(new CharacterChallengeRewardTrackModel { Id = 42, TrackId = 15, CycleId = 7, Points = 100, Revision = 1, Active = true });
                context.CharacterChallengeRewardSlot.Add(new CharacterChallengeRewardSlotModel { Id = 42, TrackId = 15, CycleId = 7, Ordinal = 0, RewardRowId = 315, RequiredPoints = 100 });
                for (int i = 0; i < stacks.Length; ++i) context.Item.Add(new ItemModel
                {
                    Id = (ulong)(101 + i), OwnerId = 42, ItemId = itemId, Location = (ushort)InventoryLocation.Inventory, BagIndex = (uint)i,
                    StackCount = stacks[i], Charges = 5, Durability = 1, ExpirationTimeLeft = expiring ? 120u : 0u,
                    Soulbound = true, MicrochipIds = "", RuneSlots = ""
                });
                context.SaveChanges();
            }
            foreach (var row in context.Item.AsNoTracking().OrderBy(row => row.BagIndex))
                Inventory.LoadItem(new GameItem(row, Manager), InventoryLocation.Inventory, row.BagIndex);
            ManagerProxy.Invocations.Clear(); SessionProxy.Invocations.Clear();
        }

        public IInventoryRewardReservation Reserve(uint count)
        {
            Assert.True(Inventory.TryReserveSoulboundCreation(InventoryLocation.Inventory, Info, count, out var held, out var error));
            Assert.Equal(GenericError.Ok, error);
            return held;
        }

        public InventoryRewardDeliveryPlan Stage(CharacterContext context, IInventoryRewardReservation held)
        {
            var slot = context.CharacterChallengeRewardSlot.Single(); slot.Claimed = true;
            var receipt = new CharacterChallengeRewardClaimModel { Id = 42, TrackId = 15, CycleId = 7, Ordinal = 0, RewardRowId = 315,
                ClaimId = claimId, CreatedUtc = DateTime.UtcNow, DeliveryVersion = 1, DeliveryJson = "pending" };
            context.CharacterChallengeRewardClaim.Add(receipt);
            return held.Stage(context, receipt);
        }

        public async Task Commit(IInventoryRewardReservation held)
        {
            await using var context = Context();
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            Stage(context, held); await context.SaveChangesAsync(); await transaction.CommitAsync();
        }

        public async Task<InventoryRewardReservationState> Resolve(IInventoryRewardReservation held)
        {
            await using var context = Context();
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            return held.ResolveOutcome(context);
        }

        public async Task Save() { await using var context = Context(); Inventory.Save(context); await context.SaveChangesAsync(); }
        public CharacterContext Context(IInterceptor interceptor = null) => interceptor == null ? new(options)
            : new(new DbContextOptionsBuilder<CharacterContext>(options).AddInterceptors(interceptor).Options);
        public void AssertNoPackets() => Assert.Empty(Messages);
        public void AssertNoEffects() { AssertNoPackets(); Assert.Empty(ManagerProxy.GetInvocations("get_NextItemId")); }
        public void ReplaceWithUnbound()
        {
            var old = Bag.GetItem(0u); Bag.RemoveItem(old);
            var row = new ItemModel { Id = old.Guid, OwnerId = 42, ItemId = 9011, Location = (ushort)InventoryLocation.Inventory, StackCount = 9, Charges = 5, Durability = 1 };
            Bag.AddItem(new GameItem(row, Manager), 0);
        }
        private void Set(string name, object value) => typeof(Inventory).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(Inventory, value);
        public void Dispose()
        {
            Inventory.InvalidateRewardDeliveries();
            string target = Path.GetFullPath(directory);
            if (Path.GetDirectoryName(target) != Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)
                || !Path.GetFileName(target).StartsWith("NexusForever-InventoryReward-", StringComparison.Ordinal))
                throw new InvalidOperationException("Test cleanup escaped its temporary directory.");
            Directory.Delete(target, recursive: true);
        }
    }

    private sealed class CommitFailure(bool after) : DbTransactionInterceptor
    {
        public override InterceptionResult TransactionCommitting(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result)
        {
            if (!after) throw new InvalidOperationException("Owned before commit failure.");
            return result;
        }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default) => new(TransactionCommitting(transaction, eventData, result));
        public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
        {
            if (after) throw new InvalidOperationException("Owned lost commit acknowledgement.");
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            TransactionCommitted(transaction, eventData); return Task.CompletedTask;
        }
    }
}
