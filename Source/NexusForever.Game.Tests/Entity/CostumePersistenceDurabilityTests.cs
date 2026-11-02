using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data;
using System.Data.Common;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Costume;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Costume;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model.Costume;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Entity;

public sealed class CostumePersistenceDurabilityTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public CostumePersistenceDurabilityTests() { }
    internal CostumePersistenceDurabilityTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AcceptedCreateEditAndEquipSurviveARejectedRealPlayerFlush(bool create)
    {
        using var f = new Fixture(options); f.Seed(); SeedCostume(f, create);
        using var actor = new Actor(f); await actor.Load();
        var inventory = RecordingDispatchProxy<IInventory>.Create(out var inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IInventory.GetItemVisuals), Enumerable.Empty<IItemVisual>());
        actor.Set(nameof(Player.Inventory), inventory);
        var currencies = new CurrencyManager(f.Source.Tables); currencies.Initialise(actor.Player, new CharacterModel { Id = 42 });
        actor.Set(nameof(Player.CurrencyManager), currencies);
        CostumeManager wardrobe = Wardrobe(f, actor);
        wardrobe.SaveCostume(Packet(2, 102)); wardrobe.SetCostume(0);
        Assert.Equal(2u, wardrobe.GetCostume(0).VisibilityMask);
        Assert.Equal(102u, wardrobe.GetCostume(0).GetItem(CostumeItemSlot.Chest).Item2Id);
        await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true));
        using (var read = f.Context())
        {
            Assert.Equal(create ? 0 : 1, read.CharacterCostume.Count());
            Assert.Equal(-1, read.Character.Single().ActiveCostumeIndex);
        }
        await Persist(f, actor, false);
        using var stored = f.Context();
        Assert.Equal(2u, stored.CharacterCostume.Single().VisibilityMask);
        Assert.Equal(102u, stored.CharacterCostumeItem.Single(row => row.Slot == 0).Item2Id);
        Assert.Equal(7, stored.CharacterCostumeItem.Count());
        Assert.Equal(0, stored.Character.Single().ActiveCostumeIndex);
        Assert.Empty(stored.CharacterEntitlement);
    }

    [Theory]
    [InlineData("dispose")] [InlineData("rollback")] [InlineData("suppressed")] [InlineData("lost-ack")]
    public async Task AnAcceptedNewGraphRetriesWithoutDuplicatingRowsAfterAnUncertainOutcome(string fault)
    {
        using var f = new Fixture(options); f.Seed(); SeedCostume(f, true);
        using var actor = new Actor(f); await actor.Load(); var wardrobe = Wardrobe(f, actor);
        wardrobe.SaveCostume(Packet(2, 102)); wardrobe.CostumeIndex = 0;
        if (fault == "lost-ack")
            await Assert.ThrowsAsync<IOException>(() => CharacterAtomicSave.Execute(() => f.Context(new LostAcknowledgement()), 42, wardrobe.Save));
        else
        {
            await using var context = f.Context(new SuppressedFlush(fault == "suppressed"));
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            wardrobe.Save(context);
            if (fault != "dispose") await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }
        using (var read = f.Context()) Assert.Equal(fault == "lost-ack" ? 1 : 0, read.CharacterCostume.Count());
        await CharacterAtomicSave.Execute(() => f.Context(), 42, wardrobe.Save);
        using var stored = f.Context(); Assert.Single(stored.CharacterCostume); Assert.Equal(7, stored.CharacterCostumeItem.Count());
        Assert.Equal(102u, stored.CharacterCostumeItem.Single(row => row.Slot == 0).Item2Id); Assert.Equal(0, stored.Character.Single().ActiveCostumeIndex);
    }

    [Fact]
    public async Task LaterActorEditsAreNotAcknowledgedByAnEarlierCommitAndCanReturnToTheLoadedValue()
    {
        using var f = new Fixture(options); f.Seed(); SeedCostume(f, false);
        using var actor = new Actor(f); await actor.Load(); var wardrobe = Wardrobe(f, actor);
        wardrobe.SaveCostume(Packet(2, 102));
        await using (var context = f.Context()) await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            wardrobe.Save(context); await context.SaveChangesAsync();
            wardrobe.SaveCostume(Packet(1, 101));
            using var competing = f.Context(); Assert.Throws<InvalidOperationException>(() => wardrobe.Save(competing));
            await transaction.CommitAsync();
        }
        using (var read = f.Context()) Assert.Equal(102u, read.CharacterCostumeItem.Single(row => row.Slot == 0).Item2Id);
        await CharacterAtomicSave.Execute(() => f.Context(), 42, wardrobe.Save);
        using var stored = f.Context(); Assert.Equal(1u, stored.CharacterCostume.Single().VisibilityMask);
        Assert.Equal(101u, stored.CharacterCostumeItem.Single(row => row.Slot == 0).Item2Id);
    }

    [Fact]
    public async Task PartialPublicSavesMergeTheirScopesAndCanUndoAnEnrolledEditBeforeFlush()
    {
        using var f = new Fixture(options); f.Seed(); SeedCostume(f, false);
        using var actor = new Actor(f); await actor.Load(); var wardrobe = Wardrobe(f, actor); var costume = wardrobe.GetCostume(0);
        await CharacterAtomicSave.Execute(() => f.Context(), 42, context =>
        {
            costume.VisibilityMask = 2; costume.Save(context);
            costume.VisibilityMask = 1; costume.Save(context);
            var chest = costume.GetItem(CostumeItemSlot.Chest); chest.Item2Id = 102; chest.Save(context);
            var legs = costume.GetItem(CostumeItemSlot.Legs); legs.DyeData = 17; legs.Save(context);
        });
        using var stored = f.Context(); Assert.Equal(1u, stored.CharacterCostume.Single().VisibilityMask);
        Assert.Equal(102u, stored.CharacterCostumeItem.Single(row => row.Slot == 0).Item2Id);
        Assert.Equal(17u, stored.CharacterCostumeItem.Single(row => row.Slot == 1).DyeData);
        Assert.Equal(-1, stored.Character.Single().ActiveCostumeIndex);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ForeignSqlOrTrackedWardrobeIntentCannotBeOverwritten(bool tracked)
    {
        using var f = new Fixture(options); f.Seed(); SeedCostume(f, false);
        using var actor = new Actor(f); await actor.Load(); var wardrobe = Wardrobe(f, actor); wardrobe.SaveCostume(Packet(2, 102));
        if (!tracked) { using var external = f.Context(); await external.CharacterCostume.ExecuteUpdateAsync(set => set.SetProperty(row => row.VisibilityMask, 3u)); }
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(() => f.Context(), 42, context =>
        {
            wardrobe.Save(context);
            if (tracked) context.CharacterCostume.Local.Single().VisibilityMask = 3;
        }));
        using var stored = f.Context(); Assert.Equal(tracked ? 1u : 3u, stored.CharacterCostume.Single().VisibilityMask);
        Assert.Equal(101u, stored.CharacterCostumeItem.Single(row => row.Slot == 0).Item2Id);
    }

    [Fact]
    public async Task RealNormalSaveFreezesWardrobeBeforeAuthAndRetainsTheLaterAcceptedEdit()
    {
        using var f = new Fixture(options); f.Seed(); SeedCostume(f, false);
        using var actor = new Actor(f); await actor.Load(); var wardrobe = Wardrobe(f, actor);
        var currencies = new CurrencyManager(f.Source.Tables); currencies.Initialise(actor.Player, new CharacterModel { Id = 42 }); actor.Set(nameof(Player.CurrencyManager), currencies);
        wardrobe.SaveCostume(Packet(2, 102)); wardrobe.CostumeIndex = 0;
        actor.OnAuth = () => { wardrobe.SaveCostume(Packet(3, 103)); wardrobe.CostumeIndex = null; };
        await actor.Player.SaveDirect(); await actor.Drain();
        using (var read = f.Context()) { Assert.Equal(2u, read.CharacterCostume.Single().VisibilityMask); Assert.Equal(102u, read.CharacterCostumeItem.Single(row => row.Slot == 0).Item2Id); Assert.Equal(0, read.Character.Single().ActiveCostumeIndex); }
        actor.OnAuth = null; await actor.Player.SaveDirect(); await actor.Drain();
        using var stored = f.Context(); Assert.Equal(3u, stored.CharacterCostume.Single().VisibilityMask);
        Assert.Equal(103u, stored.CharacterCostumeItem.Single(row => row.Slot == 0).Item2Id); Assert.Equal(-1, stored.Character.Single().ActiveCostumeIndex);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ExplicitEfAcceptanceAfterANonAcceptingFlushCanCommitTheUnchangedTarget(bool create)
    {
        using var f = new Fixture(options); f.Seed(); SeedCostume(f, create);
        using var actor = new Actor(f); await actor.Load(); var wardrobe = Wardrobe(f, actor);
        wardrobe.SaveCostume(Packet(2, 102)); wardrobe.CostumeIndex = 0;
        await using (var context = f.Context()) await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            wardrobe.Save(context);
            if (create) context.SaveChanges(false); else await context.SaveChangesAsync(false);
            context.ChangeTracker.AcceptAllChanges(); await transaction.CommitAsync();
        }
        using var read = f.Context(); Assert.Equal(2u, read.CharacterCostume.Single().VisibilityMask);
        Assert.Equal(102u, read.CharacterCostumeItem.Single(row => row.Slot == 0).Item2Id); Assert.Equal(7, read.CharacterCostumeItem.Count());
        Assert.Equal(0, read.Character.Single().ActiveCostumeIndex);
    }

    [Fact]
    public async Task MissingStaticItemDataDoesNotEraseStoredIdsOrWriteAnUntouchedParent()
    {
        using var f = new Fixture(options); f.Seed(); SeedCostume(f, false);
        CharacterCostumeModel model;
        using (var read = f.Context()) model = read.CharacterCostume.AsNoTracking().Include(row => row.CostumeItem).Single();
        var costume = new NexusForever.Game.Entity.Costume(model); Assert.Null(costume.GetItem(CostumeItemSlot.Chest).Item2Id);
        using (var context = f.Context()) { costume.Save(context); Assert.Empty(context.ChangeTracker.Entries()); }
        costume.GetItem(CostumeItemSlot.Chest).DyeData = 17;
        await CharacterAtomicSave.Execute(() => f.Context(), 42, costume.Save);
        using var stored = f.Context(); Assert.Equal(101u, stored.CharacterCostumeItem.Single(row => row.Slot == 0).Item2Id);
        Assert.Equal(17u, stored.CharacterCostumeItem.Single(row => row.Slot == 0).DyeData);
        Assert.Equal(model.Timestamp, stored.CharacterCostume.Single().Timestamp); Assert.Equal(-1, stored.Character.Single().ActiveCostumeIndex);
        Assert.Empty(stored.CharacterEntitlement);
    }

    private sealed class SuppressedFlush(bool suppress) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => new(suppress ? InterceptionResult<int>.SuppressWithResult(0) : result);
    }
    private sealed class LostAcknowledgement : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => Task.FromException(new IOException("Owned wardrobe commit acknowledgement loss."));
    }

    private static void SeedCostume(Fixture f, bool create)
    {
        using var context = f.Context(); context.Character.Single().ActiveCostumeIndex = -1;
        if (!create)
        {
            context.CharacterCostume.Add(new() { Id = 42, Index = 0, VisibilityMask = 1 });
            for (byte slot = 0; slot < 7; slot++) context.CharacterCostumeItem.Add(new() { Id = 42, Index = 0, Slot = slot, Item2Id = slot == 0 ? 101u : 0 });
        }
        context.SaveChanges();
    }

    private static CostumeManager Wardrobe(Fixture f, Actor actor)
    {
        var inventory = RecordingDispatchProxy<IInventory>.Create(out var inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IInventory.GetItemVisuals), Enumerable.Empty<IItemVisual>());
        actor.Set(nameof(Player.Inventory), inventory);
        var account = (RecordingDispatchProxy<IAccount>)(object)actor.Player.Account;
        var unlocks = RecordingDispatchProxy<IAccountCostumeManager>.Create(out var up); up.SetMethodReturn(nameof(IAccountCostumeManager.HasItemUnlock), true);
        account.SetProperty(nameof(IAccount.CostumeManager), unlocks);
        var property = RecordingDispatchProxy<IRewardProperty>.Create(out var pp); pp.SetMethodReturn(nameof(IRewardProperty.GetValue), 4f);
        var rewards = RecordingDispatchProxy<IRewardPropertyManager>.Create(out var rp); rp.SetMethodReturn(nameof(IRewardPropertyManager.GetRewardProperty), property);
        account.SetProperty(nameof(IAccount.RewardPropertyManager), rewards);
        var items = RecordingDispatchProxy<IItemManager>.Create(out var ip);
        ip.SetMethodHandler(nameof(IItemManager.GetItemInfo), (_, args) =>
        {
            var info = RecordingDispatchProxy<IItemInfo>.Create(out var proxy); proxy.SetProperty(nameof(IItemInfo.Id), (uint)args[0]);
            proxy.SetMethodReturn(nameof(IItemInfo.IsEquippable), true); return info;
        });
        using var read = f.Context(); var model = read.Character.AsNoTracking().Include(row => row.Costume).ThenInclude(row => row.CostumeItem).Single();
        var wardrobe = new CostumeManager(actor.Player, model, items);
        actor.Set(nameof(Player.CostumeManager), wardrobe); return wardrobe;
    }

    private static ClientCostumeSave Packet(uint visibility, uint item)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(0); writer.Write(CostumeType.Personal, 2u); writer.Write(0ul);
            for (int slot = 0; slot < 7; slot++) { writer.Write(slot == 0 ? item : 0u, 18u); writer.Write(0u); writer.Write(0u); writer.Write(0u); }
            writer.Write(visibility); writer.Write(false); writer.FlushBits();
            using var reader = new GamePacketReader(new MemoryStream(stream.ToArray())); var packet = new ClientCostumeSave(); packet.Read(reader); return packet;
        }
    }

    private static Task Persist(Fixture fixture, Actor actor, bool fail)
        => CharacterAtomicSave.Execute(() =>
        {
            var context = fixture.Context(); context.SavingChanges += (_, _) => { if (fail) throw new IOException("Owned wardrobe flush fault."); }; return context;
        }, 42, actor.Player.Save);
}
