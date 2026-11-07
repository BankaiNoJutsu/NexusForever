using System.Data.Common;
using System.Data;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.GameTable;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Entity;

public sealed class CharacterRewardItemEnrollmentTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "NexusForever-RewardItem-" + Guid.NewGuid().ToString("N"));
    private readonly DbContextOptions<CharacterContext> options;
    private readonly string claimId = Guid.NewGuid().ToString("N");
    private readonly IItemInfo info;
    private readonly IItemManager manager;
    private readonly Bag bag = new(InventoryLocation.Inventory, 3);
    private readonly GameItem item;

    public CharacterRewardItemEnrollmentTests() : this(null) { }

    internal CharacterRewardItemEnrollmentTests(DbContextOptions<CharacterContext> providerOptions)
    {
        Directory.CreateDirectory(directory);
        options = providerOptions ?? new DbContextOptionsBuilder<CharacterContext>()
            .UseSqlite($"Data Source={Path.Combine(directory, "reward.sqlite")};Pooling=False").Options;
        info = RecordingDispatchProxy<IItemInfo>.Create(out var ip);
        ip.SetProperty(nameof(IItemInfo.Id), 9011u);
        ip.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 9011, MaxStackCount = 10, MaxCharges = 5 });
        ip.SetMethodReturn(nameof(IItemInfo.IsStackable), true);
        manager = RecordingDispatchProxy<IItemManager>.Create(out var mp);
        mp.SetMethodReturn(nameof(IItemManager.GetItemInfo), info);
        mp.SetMethodReturn("get_NextItemId", 4243ul);
        using var context = Context();
        context.Database.EnsureCreated();
        context.Character.Add(new CharacterModel { Id = 42, Name = "Reward owner" });
        context.CharacterChallengeRewardTrack.Add(new CharacterChallengeRewardTrackModel
            { Id = 42, TrackId = 15, CycleId = 7, Points = 100, Revision = 1, Active = true });
        context.CharacterChallengeRewardSlot.Add(new CharacterChallengeRewardSlotModel
            { Id = 42, TrackId = 15, CycleId = 7, Ordinal = 0, RewardRowId = 315, RequiredPoints = 100 });
        context.Item.Add(Template(4242, 3));
        context.SaveChanges();
        item = new GameItem(context.Item.AsNoTracking().Single(), manager);
        bag.AddItem(item, 0);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task CommittedOverridePublishesOnceAndPreservesLaterUnrelatedDirtyFields(bool asynchronous, bool acceptChanges)
    {
        using var token = Reserve();
        await using (var context = Context())
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            StageClaim(context, token, 4);
            Assert.Equal(3u, item.StackCount);
            Assert.Null(token.PublishCommitted());
            item.Save(context); // safe reuse only after this override is fully enrolled
            if (asynchronous) await context.SaveChangesAsync(acceptChanges); else context.SaveChanges(acceptChanges);
            Assert.Equal(7u, (await context.Item.AsNoTracking().SingleAsync()).StackCount);
            item.Durability = .4f;
            item.RuneSlots.Add(new ItemRuneSlot { RuneItem2Id = 88 });
            item.TouchRuneSlots();
            Assert.Null(token.PublishCommitted());
            if (asynchronous) await transaction.CommitAsync(); else transaction.Commit();
        }
        Assert.Equal(RewardItemReservationState.Committed, token.State);
        Assert.Equal(3u, item.StackCount);
        Assert.False(token.Cancel());
        Assert.Same(item, token.PublishCommitted());
        Assert.Null(token.PublishCommitted());
        Assert.Equal(7u, item.StackCount);
        Assert.Equal(.4f, item.Durability);
        await SaveOrdinary();
        await using var verify = Context();
        var actual = await verify.Item.AsNoTracking().SingleAsync();
        Assert.Equal(7u, actual.StackCount);
        Assert.Equal(.4f, actual.Durability);
        Assert.Contains("88", actual.RuneSlots);
        Assert.Equal(1, await verify.CharacterChallengeRewardClaim.CountAsync());
        Assert.True((await verify.CharacterChallengeRewardSlot.SingleAsync()).Claimed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RollbackOrDisposalRetainsEarlierOrdinaryCountWithoutAProvisionalAward(bool disposeOnly)
    {
        item.StackCount = 4;
        item.Durability = .8f;
        using var token = Reserve();
        await using (var context = Context())
        {
            var transaction = await context.Database.BeginTransactionAsync();
            StageClaim(context, token, 2);
            await context.SaveChangesAsync();
            token.Dispose();
            Assert.True(item.HasRewardMutationReservation);
            Assert.Equal(4u, item.StackCount);
            if (!disposeOnly) await transaction.RollbackAsync();
            await transaction.DisposeAsync();
        }
        Assert.Equal(RewardItemReservationState.Uncertain, token.State);
        Assert.False(token.Cancel());
        Assert.Equal(RewardItemReservationState.ReadyToRetry, await Resolve(token));
        Assert.True(token.Cancel());
        await SaveOrdinary();
        await using var verify = Context();
        Assert.Equal(4u, (await verify.Item.AsNoTracking().SingleAsync()).StackCount);
        Assert.Empty(await verify.CharacterChallengeRewardClaim.ToListAsync());
        Assert.False((await verify.CharacterChallengeRewardSlot.SingleAsync()).Claimed);
    }

    [Fact]
    public async Task SqlFailureThenVerifiedRetryKeepsQuantityAndRefreshesOnlyUnrelatedIntent()
    {
        using var token = Reserve();
        await using (var fail = Context())
        {
            await fail.Database.ExecuteSqlRawAsync(fail.Database.IsSqlite()
                ? "CREATE TRIGGER reject_item BEFORE UPDATE ON item BEGIN SELECT RAISE(ABORT, 'owned test failure'); END"
                : "CREATE TRIGGER reject_item BEFORE UPDATE ON item FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='owned test failure'");
            StageClaim(fail, token, 4);
            await Assert.ThrowsAsync<DbUpdateException>(() => fail.SaveChangesAsync());
        }
        token.Dispose();
        Assert.Equal(3u, item.StackCount);
        Assert.Equal(RewardItemReservationState.Uncertain, token.State);
        Assert.Equal(RewardItemReservationState.ReadyToRetry, await Resolve(token));
        item.Durability = .6f;
        await using (var retry = Context())
        {
            await retry.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_item");
            StageClaim(retry, token, 4);
            Assert.Equal(7u, token.After.StackCount);
            Assert.Equal(.6f, token.After.Durability);
            await retry.SaveChangesAsync();
        }
        Assert.Same(item, token.PublishCommitted());
        await SaveOrdinary();
        await using var verify = Context();
        Assert.Equal(7u, (await verify.Item.AsNoTracking().SingleAsync()).StackCount);
        Assert.Equal(1, await verify.CharacterChallengeRewardClaim.CountAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NewPrivateStackRemainsInvisibleAndReusesItsIdAcrossVerifiedRollback(bool rollbackFirst)
    {
        ItemModel template = Template(4243, 6); template.BagIndex = 1;
        using var token = GameItem.PrepareNewRewardItem(template, info);
        template.StackCount = 1; // constructor owns a copy
        Assert.True(bag.TryReserveSlots(new Dictionary<uint, IItem> { [1] = null }, out var destination));
        using (destination)
        {
            if (rollbackFirst)
            {
                await using (var first = Context())
                await using (var transaction = await first.Database.BeginTransactionAsync())
                {
                    StageClaim(first, token, 0);
                    await first.SaveChangesAsync();
                    await transaction.RollbackAsync();
                }
                Assert.Equal(RewardItemReservationState.ReadyToRetry, await Resolve(token));
            }
            await using (var context = Context())
            {
                StageClaim(context, token, 0);
                Assert.Null(token.Before);
                Assert.Equal(4243ul, token.After.Id);
                Assert.Null(bag.GetItem(1u));
                await context.SaveChangesAsync();
            }
            GameItem published = Assert.IsType<GameItem>(token.PublishCommitted());
            Assert.True(published.Soulbound);
            Assert.False(published.PendingCreate);
            Assert.Equal(6u, published.StackCount);
            Assert.Equal(InventoryLocation.Inventory, published.Location);
            Assert.Equal(1u, published.BagIndex);
            destination.PublishItem(published, 1);
            Assert.Same(published, bag.GetItem(1u));
        }
        await using var verify = Context();
        Assert.Equal(2, await verify.Item.CountAsync());
        Assert.Equal(6u, (await verify.Item.AsNoTracking().SingleAsync(row => row.Id == 4243)).StackCount);
    }

    [Fact]
    public async Task ExistingPendingCreateIsEnrolledWithItsOrdinaryQuantityAndRewardInOneUnit()
    {
        var pending = new GameItem(42, info, 2, itemManager: manager);
        pending.MakeSoulbound();
        bag.AddItem(pending, 1);
        Assert.True(bag.TryReserveRewardMutation(pending, out var token));
        using (token)
        {
            await using (var context = Context())
            {
                StageClaim(context, token, 3);
                Assert.Null(token.Before);
                Assert.Equal(2u, pending.StackCount);
                await context.SaveChangesAsync();
            }
            Assert.Same(pending, token.PublishCommitted());
            Assert.Equal(5u, pending.StackCount);
            Assert.False(pending.PendingCreate);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailureBeforeOrAfterActualCommitResolvesThroughTheReceipt(bool afterCommit)
    {
        using var token = Reserve();
        var interceptor = new CommitFailure(afterCommit);
        await using (var context = Context(interceptor))
        {
            context.Database.AutoTransactionBehavior = AutoTransactionBehavior.Always;
            StageClaim(context, token, 4);
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        }
        Assert.Equal(RewardItemReservationState.Uncertain, token.State);
        Assert.Equal(3u, item.StackCount);
        token.Dispose();
        Assert.True(item.HasRewardMutationReservation);
        var resolved = await Resolve(token);
        Assert.Equal(afterCommit ? RewardItemReservationState.Committed : RewardItemReservationState.ReadyToRetry, resolved);
        if (afterCommit)
        {
            Assert.Same(item, token.PublishCommitted());
            await SaveOrdinary();
            Assert.Equal(7u, item.StackCount);
        }
        else Assert.True(token.Cancel());
        await using var verify = Context();
        Assert.Equal(afterCommit ? 1 : 0, await verify.CharacterChallengeRewardClaim.CountAsync());
        Assert.Equal(afterCommit ? 7u : 3u, (await verify.Item.AsNoTracking().SingleAsync()).StackCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReceiptDoesNotAuthorizeResurrectionAfterNewerConsumption(bool remove)
    {
        using var token = Reserve();
        await using (var context = Context(new CommitFailure(true)))
        {
            context.Database.AutoTransactionBehavior = AutoTransactionBehavior.Always;
            StageClaim(context, token, 4);
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        }
        await using (var external = Context())
            await external.Database.ExecuteSqlRawAsync(remove ? "DELETE FROM item WHERE id=4242" : "UPDATE item SET stackCount=1 WHERE id=4242");
        Assert.Equal(RewardItemReservationState.Quarantined, await Resolve(token));
        Assert.Null(token.PublishCommitted());
        Assert.False(token.Cancel());
        Assert.True(token.AbandonForAuthoritativeReload());
        Assert.Throws<InvalidOperationException>(() => item.StackCount = 9);
        await using (var stale = Context())
        {
            Assert.Throws<InvalidOperationException>(() => item.Save(stale));
            Assert.True(stale.IsSaveRejected);
        }
        await using var verify = Context();
        var actual = await verify.Item.AsNoTracking().SingleOrDefaultAsync();
        if (remove) Assert.Null(actual);
        else
        {
            Assert.Equal(1u, actual.StackCount);
            var reloaded = new GameItem(actual, manager);
            reloaded.Durability = .3f;
            reloaded.Save(verify); // abandoned lease does not strand the authoritative owner
            await verify.SaveChangesAsync();
        }
        Assert.Equal(1, await verify.CharacterChallengeRewardClaim.CountAsync());
    }

    [Theory]
    [InlineData("count")]
    [InlineData("owner")]
    [InlineData("location")]
    [InlineData("charges")]
    [InlineData("receipt")]
    [InlineData("claimed")]
    [InlineData("detach")]
    [InlineData("unchanged")]
    [InlineData("original-values")]
    public async Task TrackedMutationRejectsTheWholeReceiptUnitBeforeAnySql(string mutation)
    {
        using var token = Reserve();
        await using (var context = Context())
        {
            StageClaim(context, token, 4);
            var tracked = context.ChangeTracker.Entries<ItemModel>().Single();
            switch (mutation)
            {
                case "count": tracked.Entity.StackCount = 9; break;
                case "owner": tracked.Entity.OwnerId = null; break;
                case "location": tracked.Entity.Location = (ushort)InventoryLocation.PlayerBank; break;
                case "charges": tracked.Entity.Charges = 1; break;
                case "receipt": context.ChangeTracker.Entries<CharacterChallengeRewardClaimModel>().Single().Entity.DeliveryJson = "wrong"; break;
                case "claimed": context.ChangeTracker.Entries<CharacterChallengeRewardSlotModel>().Single().Entity.Claimed = false; break;
                case "detach": tracked.State = EntityState.Detached; break;
                case "unchanged": tracked.State = EntityState.Unchanged; break;
                case "original-values": tracked.OriginalValues[nameof(ItemModel.StackCount)] = 99u; break;
            }
            await Assert.ThrowsAnyAsync<Exception>(() => context.SaveChangesAsync());
            Assert.True(context.IsSaveRejected);
        }
        Assert.Equal(RewardItemReservationState.ReadyToRetry, await Resolve(token));
        Assert.True(token.Cancel());
        await using var verify = Context();
        Assert.Equal(3u, (await verify.Item.AsNoTracking().SingleAsync()).StackCount);
        Assert.Empty(await verify.CharacterChallengeRewardClaim.ToListAsync());
    }

    [Fact]
    public async Task CommitWithoutFlushRejectsInsteadOfAcknowledgingAnUnwrittenAward()
    {
        using var token = Reserve();
        await using (var context = Context())
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            StageClaim(context, token, 4);
            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
            Assert.True(context.IsSaveRejected);
            await transaction.RollbackAsync();
        }
        Assert.Equal(RewardItemReservationState.ReadyToRetry, await Resolve(token));
        Assert.True(token.Cancel());
        Assert.Equal(3u, item.StackCount);
    }

    [Fact]
    public async Task SavepointRollbackBlocksSameContextRestagingAndRequiresFreshResolution()
    {
        using var token = Reserve();
        await using (var context = Context())
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            await transaction.CreateSavepointAsync("before_reward");
            StageClaim(context, token, 4);
            await context.SaveChangesAsync();
            await transaction.RollbackToSavepointAsync("before_reward");
            Assert.Throws<InvalidOperationException>(() => item.Save(context));
            Assert.True(context.IsSaveRejected);
            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
            await transaction.RollbackAsync();
        }
        Assert.Equal(RewardItemReservationState.ReadyToRetry, await Resolve(token));
        Assert.True(token.Cancel());
    }

    [Fact]
    public async Task ASecondInstanceCannotWriteTheGuidUntilPublicationOrAuthoritativeAbandon()
    {
        using var token = Reserve();
        await using (var context = Context()) { StageClaim(context, token, 4); await context.SaveChangesAsync(); }
        await using (var other = Context())
        {
            var duplicate = new GameItem(await other.Item.AsNoTracking().SingleAsync(), manager);
            duplicate.Durability = .5f;
            Assert.Throws<InvalidOperationException>(() => duplicate.Save(other));
        }
        Assert.Same(item, token.PublishCommitted());
        await using var next = Context();
        var current = new GameItem(await next.Item.AsNoTracking().SingleAsync(), manager);
        current.Durability = .5f;
        current.Save(next);
        await next.SaveChangesAsync();
    }

    [Fact]
    public async Task CompletionObserverCannotPublishBeforeTheItemSettlementAndSnapshotsAreCopies()
    {
        using var token = Reserve();
        GameItem early = item;
        await using (var context = Context())
        {
            context.OnSaveCompleted(_ => early = token.PublishCommitted());
            StageClaim(context, token, 4);
            token.After.StackCount = 1;
            token.Before.StackCount = 9;
            await context.SaveChangesAsync();
        }
        Assert.Null(early);
        Assert.Equal(7u, token.After.StackCount);
        Assert.Equal(3u, token.Before.StackCount);
        Assert.Same(item, token.PublishCommitted());
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(8u)]
    [InlineData(uint.MaxValue)]
    public void InvalidMergeQuantityRejectsTheSuppliedUnitWithoutChangingRuntime(uint award)
    {
        using var token = Reserve();
        using var context = Context();
        Assert.Throws<InvalidOperationException>(() => token.Stage(context, award, _ => { }));
        Assert.True(context.IsSaveRejected);
        Assert.Equal(3u, item.StackCount);
        Assert.True(token.Cancel());
    }

    [Fact]
    public async Task FailedSharedContextEnrollmentRejectsAnAlreadyAddedReceiptAndRetainsOrdinaryIntent()
    {
        using var token = Reserve();
        await using (var context = Context())
        {
            (await context.Item.SingleAsync()).OwnerId = null;
            Assert.Throws<InvalidOperationException>(() => StageClaim(context, token, 4));
            Assert.True(context.IsSaveRejected);
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        }
        Assert.Equal(RewardItemReservationState.ReadyToRetry, await Resolve(token));
        Assert.True(token.Cancel());
        await SaveOrdinary();
        await using var verify = Context();
        Assert.Equal(42ul, (await verify.Item.SingleAsync()).OwnerId);
        Assert.Empty(await verify.CharacterChallengeRewardClaim.ToListAsync());
    }

    [Fact]
    public async Task SameContextReenrollmentIsIdempotentOnlyForTheSameFrozenGuardAndAward()
    {
        using var token = Reserve();
        await using (var context = Context())
        {
            Action<CharacterSaveValidationPhase> guard = _ => { };
            var first = token.Stage(context, 4, guard);
            first.StackCount = 9;
            Assert.Equal(7u, token.Stage(context, 4, guard).StackCount);
            Assert.Throws<InvalidOperationException>(() => token.Stage(context, 3, guard));
            Assert.True(context.IsSaveRejected);
        }
        Assert.Equal(RewardItemReservationState.ReadyToRetry, await Resolve(token));
        Assert.True(token.Cancel());
    }

    [Fact]
    public async Task ResolutionAndAbandonCannotRunBeforeTheOriginalUnitIsTerminal()
    {
        using var token = Reserve();
        await using (var context = Context())
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            StageClaim(context, token, 4);
            await context.SaveChangesAsync();
            await using var other = Context();
            bool called = false;
            Assert.Equal(RewardItemReservationState.Staged, token.ResolveOutcome(other, _ => { called = true; return false; }));
            Assert.False(called);
            Assert.False(token.AbandonForAuthoritativeReload());
            Assert.False(token.Cancel());
            await transaction.RollbackAsync();
        }
        Assert.Equal(RewardItemReservationState.ReadyToRetry, await Resolve(token));
        Assert.True(token.Cancel());
    }

    [Fact]
    public async Task VerifiedAbsentOutcomeRetainsTheGuidLeaseUntilExplicitCancel()
    {
        using var token = Reserve();
        await using (var context = Context())
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            StageClaim(context, token, 4);
            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }
        Assert.Equal(RewardItemReservationState.ReadyToRetry, await Resolve(token));
        await using (var blocked = Context())
        {
            var duplicate = new GameItem(await blocked.Item.AsNoTracking().SingleAsync(), manager);
            duplicate.Durability = .6f;
            Assert.Throws<InvalidOperationException>(() => duplicate.Save(blocked));
        }
        Assert.True(token.Cancel());
        await SaveOrdinary();
    }

    [Fact]
    public async Task WrongReceiptOutcomeCannotReleaseACommittedAwardAsAnAbsentRetry()
    {
        using var token = Reserve();
        await using (var context = Context(new CommitFailure(true)))
        {
            context.Database.AutoTransactionBehavior = AutoTransactionBehavior.Always;
            StageClaim(context, token, 4);
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        }
        await using var fresh = Context();
        await using var read = await fresh.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        Assert.Equal(RewardItemReservationState.Quarantined, token.ResolveOutcome(fresh, _ => false));
        Assert.False(token.Cancel());
        Assert.Null(token.PublishCommitted());
        Assert.True(token.AbandonForAuthoritativeReload());
    }

    [Fact]
    public async Task SuppressedEmptySaveCannotAcknowledgeAQuantityThatNeverReachedSql()
    {
        using var token = Reserve();
        await using (var context = Context(new SuppressSave()))
        {
            StageClaim(context, token, 4);
            Assert.Equal(0, await context.SaveChangesAsync());
        }
        Assert.Equal(RewardItemReservationState.Uncertain, token.State);
        Assert.Null(token.PublishCommitted());
        Assert.Equal(RewardItemReservationState.ReadyToRetry, await Resolve(token));
        Assert.True(token.Cancel());
    }

    [Fact]
    public async Task ResolutionRejectsMissingTransactionTrackedReadsAndAnotherConfiguredDatabase()
    {
        using var token = Reserve();
        await using (var context = Context()) { StageClaim(context, token, 4); }
        await using (var noTransaction = Context())
            Assert.Throws<InvalidOperationException>(() => token.ResolveOutcome(noTransaction, _ => false));
        await using (var tracked = Context())
        await using (var read = await tracked.Database.BeginTransactionAsync())
            Assert.Throws<InvalidOperationException>(() => token.ResolveOutcome(tracked, c => { c.CharacterChallengeRewardSlot.Single(); return false; }));
        await using (var foreign = new CharacterContext(new DbContextOptionsBuilder<CharacterContext>()
            .UseSqlite($"Data Source={Path.Combine(directory, "foreign.sqlite")};Pooling=False").Options))
        await using (var read = await foreign.Database.BeginTransactionAsync())
        {
            bool called = false;
            Assert.Throws<InvalidOperationException>(() => token.ResolveOutcome(foreign, _ => { called = true; return false; }));
            Assert.False(called);
        }
        Assert.Equal(RewardItemReservationState.Uncertain, token.State);
        Assert.Equal(RewardItemReservationState.ReadyToRetry, await Resolve(token));
        Assert.True(token.Cancel());
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("binding")]
    [InlineData("id")]
    [InlineData("location")]
    [InlineData("quantity")]
    [InlineData("charges")]
    [InlineData("uninitialized-charges")]
    [InlineData("durability")]
    [InlineData("runes")]
    [InlineData("microchips")]
    public void PrivateStackRequiresValidFrozenValuesAndLosslessCanonicalCodecs(string invalid)
    {
        var model = Template(4243, 5);
        switch (invalid)
        {
            case "owner": model.OwnerId = null; break;
            case "binding": model.Soulbound = false; break;
            case "id": model.Id = 0; break;
            case "location": model.Location = (ushort)InventoryLocation.RealmBank; break;
            case "quantity": model.StackCount = 11; break;
            case "charges": model.Charges = 6; break;
            case "uninitialized-charges": model.Charges = 0; break;
            case "durability": model.Durability = float.NaN; break;
            case "runes": model.RuneSlots = "bad-slot"; break;
            case "microchips": model.MicrochipIds = ",77,"; break;
        }
        Assert.Throws<ArgumentException>(() => GameItem.PrepareNewRewardItem(model, info));
        Assert.Single(bag);
    }

    [Fact]
    public async Task NewPrivateStackRetainsTheTableOwnerAndCommittedValuesInItsFirstWirePayload()
    {
        info.Entry.ItemRuneInstanceId = 1;
        var table = (GameTable<ItemRuneInstanceEntry>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<ItemRuneInstanceEntry>));
        typeof(GameTable<ItemRuneInstanceEntry>).GetProperty(nameof(GameTable<ItemRuneInstanceEntry>.Entries))!.SetValue(table,
            new[] { new ItemRuneInstanceEntry { Id = 1, DefinedSocketCount = 1 } });
        typeof(GameTable<ItemRuneInstanceEntry>).GetField("header", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(table, new GameTableHeader { MaxId = 2 });
        typeof(GameTable<ItemRuneInstanceEntry>).GetField("lookup", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(table, new[] { -1, 0 });
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tp);
        tp.SetProperty(nameof(IGameTableManager.ItemRuneInstance), table);
        var model = Template(4243, 5);
        model.RuneSlots = $"{(uint)RuneType.Air}:88";
        model.MicrochipIds = "77";
        model.BagIndex = 1;
        using var token = GameItem.PrepareNewRewardItem(model, info, tables);
        await using (var context = Context()) { StageClaim(context, token, 0); await context.SaveChangesAsync(); }
        var published = Assert.IsType<GameItem>(token.PublishCommitted());
        var wire = published.Build();
        Assert.True(published.Soulbound);
        Assert.Equal(5u, wire.StackCount);
        Assert.Equal(2u, wire.Charges);
        Assert.Equal(0u, wire.RandomGlyphData & 7u); // defined socket is not relabeled as an added socket
        Assert.Equal(new[] { 88u }, wire.Glyphs);
        Assert.Equal(new[] { 77u }, wire.Microchips);
        await using var verify = Context();
        Assert.Equal(model.RuneSlots, (await verify.Item.AsNoTracking().SingleAsync(row => row.Id == 4243)).RuneSlots);
    }

    private GameItem.RewardMutationReservation Reserve()
    {
        Assert.True(bag.TryReserveRewardMutation(item, out var token));
        return token;
    }

    private void StageClaim(CharacterContext context, GameItem.RewardMutationReservation token, uint award)
    {
        var slot = context.CharacterChallengeRewardSlot.Single();
        slot.Claimed = true;
        var claim = new CharacterChallengeRewardClaimModel
        {
            Id = 42, TrackId = 15, CycleId = 7, Ordinal = 0, RewardRowId = 315,
            ClaimId = claimId, DeliveryVersion = 1, DeliveryJson = "pending", CreatedUtc = DateTime.UtcNow
        };
        context.CharacterChallengeRewardClaim.Add(claim);
        Action<CharacterSaveValidationPhase> guard = _ =>
        {
            Assert.True(slot.Claimed);
            Assert.Equal(claimId, claim.ClaimId);
            Assert.Equal(JsonSerializer.Serialize(token.After), claim.DeliveryJson);
            Assert.True(context.Entry(claim).State is EntityState.Added or EntityState.Unchanged);
        };
        ItemModel staged = token.Stage(context, award, guard);
        claim.DeliveryJson = JsonSerializer.Serialize(staged);
    }

    private async Task<RewardItemReservationState> Resolve(GameItem.RewardMutationReservation token)
    {
        await using var fresh = Context();
        await using var read = await fresh.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        return token.ResolveOutcome(fresh, context =>
        {
            var receipt = context.CharacterChallengeRewardClaim.AsNoTracking().SingleOrDefault();
            bool claimed = context.CharacterChallengeRewardSlot.AsNoTracking().Single().Claimed;
            if (receipt == null) return claimed ? null : false;
            return receipt.ClaimId == claimId && receipt.DeliveryJson == JsonSerializer.Serialize(token.After) && claimed ? true : null;
        });
    }

    private CharacterContext Context(IInterceptor interceptor = null) => interceptor == null ? new(options)
        : new(new DbContextOptionsBuilder<CharacterContext>(options).AddInterceptors(interceptor).Options);

    private async Task SaveOrdinary()
    {
        await using var context = Context();
        item.Save(context);
        await context.SaveChangesAsync();
    }

    private static ItemModel Template(ulong id, uint count) => new()
    {
        Id = id, OwnerId = 42, ItemId = 9011, Location = (ushort)InventoryLocation.Inventory,
        BagIndex = 0, StackCount = count, Charges = 2, Durability = 1, ExpirationTimeLeft = 120,
        Soulbound = true, MicrochipIds = "", RuneSlots = ""
    };

    private sealed class CommitFailure(bool afterCommit) : DbTransactionInterceptor
    {
        public override InterceptionResult TransactionCommitting(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result)
        {
            if (!afterCommit) throw new InvalidOperationException("Owned failure before commit.");
            return result;
        }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default) => new(TransactionCommitting(transaction, eventData, result));
        public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
        {
            if (afterCommit) throw new InvalidOperationException("Owned failure after commit.");
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            TransactionCommitted(transaction, eventData);
            return Task.CompletedTask;
        }
    }

    private sealed class SuppressSave : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
            => InterceptionResult<int>.SuppressWithResult(0);
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default) => new(InterceptionResult<int>.SuppressWithResult(0));
    }

    public void Dispose()
    {
        string target = Path.GetFullPath(directory);
        string parent = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar);
        if (!string.Equals(Path.GetDirectoryName(target), parent, StringComparison.OrdinalIgnoreCase)
            || !Path.GetFileName(target).StartsWith("NexusForever-RewardItem-", StringComparison.Ordinal))
            throw new InvalidOperationException("Test cleanup escaped its dedicated temporary directory.");
        Directory.Delete(target, recursive: true);
    }
}
