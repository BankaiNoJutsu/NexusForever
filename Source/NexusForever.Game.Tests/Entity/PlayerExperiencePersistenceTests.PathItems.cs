using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Database.Character;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;
using GamePath = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class PlayerExperiencePersistenceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PathItems_WaitForTheOriginalRewardMarkerAndItemCommit(bool merge)
    {
        using var f = new Fixture(options); f.Seed(); Seed(f);
        using var actor = CreatePathItemActor(f, out var session);
        await actor.Load();
        var path = ConfigurePathItemReward(f, actor);
        if (merge)
        {
            actor.Player.Inventory.ItemCreate(InventoryLocation.Inventory, 10, 1);
            await Persist(f, actor, false);
        }
        path.AddXp(1000);
        Assert.Empty(PathItemPackets(session));
        await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true));
        await actor.Drain();
        Assert.Empty(PathItemPackets(session));
        using (var before = f.Context())
        {
            Assert.Equal(1, before.CharacterPath.Single(row => row.Path == (byte)GamePath.Soldier).LevelRewarded);
            Assert.Equal(merge ? 1 : 0, before.Item.Count());
        }
        await Persist(f, actor, false);
        await actor.Drain();
        var packet = Assert.Single(PathItemPackets(session));
        if (merge) Assert.Equal(4u, Assert.IsType<ServerItemStackCountUpdate>(packet).StackCount);
        else Assert.Equal(3u, Assert.IsType<ServerItemAdd>(packet).InventoryItem.Item.StackCount);
        using var after = f.Context();
        Assert.Equal(2, after.CharacterPath.Single(row => row.Path == (byte)GamePath.Soldier).LevelRewarded);
        Assert.Equal(merge ? 4u : 3u, Assert.Single(after.Item).StackCount);
    }

    [Fact]
    public async Task PathItems_EarningDuringAuthStaysHiddenUntilItsOwnFrozenSave()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f);
        using var actor = CreatePathItemActor(f, out var session);
        await actor.Load(); var path = ConfigurePathItemReward(f, actor);
        var currency = new CurrencyManager(f.Source.Tables);
        currency.Initialise(actor.Player, new CharacterModel { Id = 42 });
        actor.Set(nameof(Player.CurrencyManager), currency);
        actor.OnAuth = () => path.AddXp(1000);
        await actor.Player.SaveDirect(); await actor.Drain();
        Assert.Empty(PathItemPackets(session));
        using (var first = f.Context())
        {
            Assert.Empty(first.Item);
            Assert.Equal(1, first.CharacterPath.Single(row => row.Path == (byte)GamePath.Soldier).LevelRewarded);
        }
        actor.OnAuth = null;
        await actor.Player.SaveDirect(); await actor.Drain();
        Assert.Equal(3u, Assert.IsType<ServerItemAdd>(Assert.Single(PathItemPackets(session))).InventoryItem.Item.StackCount);
        using var final = f.Context(); Assert.Equal(3u, Assert.Single(final.Item).StackCount);
        Assert.Equal(2, final.CharacterPath.Single(row => row.Path == (byte)GamePath.Soldier).LevelRewarded);
    }

    [Theory]
    [InlineData("same")]
    [InlineData("session")]
    [InlineData("queue")]
    [InlineData("epoch")]
    [InlineData("inventory")]
    public async Task PathItems_RetryFreshPayloadOnlyOnTheOriginalActor(string replacement)
    {
        using var f = new Fixture(options); f.Seed(); Seed(f);
        using var actor = CreatePathItemActor(f, out var session);
        await actor.Load(); var path = ConfigurePathItemReward(f, actor);
        int attempts = 0;
        ServerItemAdd first = null;
        session.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is not ServerItemAdd packet || packet.InventoryItem.Reason != ItemUpdateReason.PathReward) return null;
            using var stored = f.Context();
            Assert.Equal(3u, Assert.Single(stored.Item).StackCount);
            Assert.Equal(2, stored.CharacterPath.Single(row => row.Path == (byte)GamePath.Soldier).LevelRewarded);
            Assert.Equal(3u, packet.InventoryItem.Item.StackCount);
            Assert.Empty(packet.InventoryItem.Item.Glyphs);
            if (++attempts == 1)
            {
                first = packet;
                packet.InventoryItem.Item.StackCount = 999;
                packet.InventoryItem.Item.Glyphs.Add(999);
                if (replacement == "session") session.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
                if (replacement == "queue") session.SetProperty(nameof(IWorldSession.Events), new EventQueue());
                if (replacement == "epoch") ((RecordingDispatchProxy<IAccount>)(object)actor.Player.Account).SetProperty(nameof(IAccount.WorldSessionEpoch), actor.Player.Account.WorldSessionEpoch + 1);
                if (replacement == "inventory") actor.Set(nameof(Player.Inventory), RecordingDispatchProxy<IInventory>.Create(out _));
                throw new IOException("Owned Path item enqueue refusal.");
            }
            Assert.NotSame(first, packet);
            Assert.NotSame(first.InventoryItem.Item, packet.InventoryItem.Item);
            return null;
        });
        path.AddXp(1000);
        await Persist(f, actor, false); await actor.Drain();
        Assert.Equal(replacement == "same" ? 2 : 1, attempts);
        Assert.Empty(actor.Errors);
    }

    [Fact]
    public async Task PathItems_OrdinaryCreationKeepsItsExistingImmediateRoute()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f);
        using var actor = CreatePathItemActor(f, out var session);
        await actor.Load(); ConfigurePathItemReward(f, actor);
        actor.Player.Inventory.ItemCreate(InventoryLocation.Inventory, 10, 1);
        var packet = Assert.Single(session.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).OfType<ServerItemAdd>());
        Assert.Equal(ItemUpdateReason.NoReason, packet.InventoryItem.Reason);
    }


    [Fact]
    public async Task PathItems_MissionAndEpisodeShareTheConfirmedOrderedPacketCursor()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f);
        using var actor = CreatePathItemActor(f, out var session);
        await actor.Load(); var path = ConfigurePathItemReward(f, actor);
        var tables = (GameTableManager)typeof(PathManager).GetField("gameTableManager", Instance)!.GetValue(path)!;
        typeof(GameTableManager).GetField("<PathReward>k__BackingField", Instance)!.SetValue(tables,
            PlayerBindPointTests.Table(
                new PathRewardEntry { Id = 2, ObjectId = 35, PathRewardTypeEnum = PathRewardGrant.MissionRewardType, Item2Id = 10, Count = 3 },
                new PathRewardEntry { Id = 3, ObjectId = 9, PathRewardTypeEnum = PathRewardGrant.EpisodeRewardType, Item2Id = 10, Count = 3 }));
        path.ActivateMissions(9, new Dictionary<ushort, uint> { [35] = 25 });
        int stackAttempts = 0;
        session.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is not ServerItemStackCountUpdate { Reason: ItemUpdateReason.PathReward } packet) return null;
            using var read = f.Context();
            Assert.Equal(1, Assert.Single(read.CharacterPathMission).Completed);
            Assert.Equal(6u, Assert.Single(read.Item).StackCount);
            Assert.Equal(6u, packet.StackCount);
            if (++stackAttempts == 1) { packet.StackCount = 999; throw new IOException("Owned second Path item packet refusal."); }
            return null;
        });
        Assert.True(path.CompleteActiveMission(35));
        Assert.Empty(PathItemPackets(session));
        await Persist(f, actor, false); await actor.Drain();
        Assert.Single(PathItemPackets(session).OfType<ServerItemAdd>());
        Assert.Equal(2, stackAttempts);
        Assert.False(path.CompleteActiveMission(35));
        await Persist(f, actor, false); await actor.Drain();
        Assert.Single(PathItemPackets(session).OfType<ServerItemAdd>());
        Assert.Equal(2, stackAttempts);
        using var final = f.Context(); Assert.Equal(6u, Assert.Single(final.Item).StackCount);
    }

    [Fact]
    public async Task PathItems_AnIsolatedInventorySaveCannotConfirmThePathReward()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f);
        using var actor = CreatePathItemActor(f, out var session);
        await actor.Load(); var path = ConfigurePathItemReward(f, actor);
        path.AddXp(1000);
        await CharacterAtomicSave.Execute(() => f.Context(), 42, actor.Player.Inventory.Save);
        await actor.Drain();
        Assert.Empty(PathItemPackets(session));
        using (var read = f.Context())
        {
            Assert.Equal(3u, Assert.Single(read.Item).StackCount);
            Assert.Equal(1, read.CharacterPath.Single(row => row.Path == (byte)GamePath.Soldier).LevelRewarded);
        }
        await Persist(f, actor, false); await actor.Drain();
        Assert.Single(PathItemPackets(session));
        using var final = f.Context(); Assert.Equal(2, final.CharacterPath.Single(row => row.Path == (byte)GamePath.Soldier).LevelRewarded);
    }

    [Fact]
    public async Task PathItems_AConfirmedLevelGrantDoesNotReleaseALaterMissionStack()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f);
        using var actor = CreatePathItemActor(f, out var session);
        await actor.Load(); var path = ConfigurePathItemReward(f, actor);
        var tables = (GameTableManager)typeof(PathManager).GetField("gameTableManager", Instance)!.GetValue(path)!;
        var level = new PathRewardEntry { Id = 1, ObjectId = PathRewardGrant.GetLevelRewardObjectId(GamePath.Soldier, 2), Item2Id = 10, Count = 3 };
        typeof(GameTableManager).GetField("<PathReward>k__BackingField", Instance)!.SetValue(tables,
            PlayerBindPointTests.Table(level, new PathRewardEntry { Id = 2, ObjectId = 35, PathRewardTypeEnum = PathRewardGrant.MissionRewardType, Item2Id = 10, Count = 3 }));
        path.ActivateMissions(9, new Dictionary<ushort, uint> { [35] = 25 });
        path.AddXp(1000);
        var currency = new CurrencyManager(f.Source.Tables); currency.Initialise(actor.Player, new CharacterModel { Id = 42 });
        actor.Set(nameof(Player.CurrencyManager), currency);
        actor.OnAuth = () => Assert.True(path.CompleteActiveMission(35));
        await actor.Player.SaveDirect(); await actor.Drain();
        Assert.Equal(3u, Assert.IsType<ServerItemAdd>(Assert.Single(PathItemPackets(session))).InventoryItem.Item.StackCount);
        using (var read = f.Context())
        {
            Assert.Equal(3u, Assert.Single(read.Item).StackCount);
            Assert.Equal(0, Assert.Single(read.CharacterPathMission).Completed);
        }
        actor.OnAuth = null; await actor.Player.SaveDirect(); await actor.Drain();
        Assert.Equal(6u, Assert.Single(PathItemPackets(session).OfType<ServerItemStackCountUpdate>()).StackCount);
        using var final = f.Context(); Assert.Equal(6u, Assert.Single(final.Item).StackCount);
        Assert.Equal(1, Assert.Single(final.CharacterPathMission).Completed);
    }


    [Theory]
    [InlineData("new-merge", 4u)]
    [InlineData("existing-merge", 5u)]
    [InlineData("existing-consume", 3u)]
    [InlineData("new-remove", 0u)]
    public async Task PathItems_OrdinarySameItemChangesCannotBeOverwrittenByADelayedReward(string operation, uint expected)
    {
        using var f = new Fixture(options); f.Seed(); Seed(f);
        using var actor = CreatePathItemActor(f, out var session);
        await actor.Load(); var path = ConfigurePathItemReward(f, actor);
        if (operation.StartsWith("existing"))
        {
            actor.Player.Inventory.ItemCreate(InventoryLocation.Inventory, 10, 1);
            await Persist(f, actor, false);
        }
        path.AddXp(1000);
        if (operation.EndsWith("merge")) actor.Player.Inventory.ItemCreate(InventoryLocation.Inventory, 10, 1);
        else actor.Player.Inventory.ItemDelete(10, operation == "new-remove" ? 3u : 1u, ItemUpdateReason.NoReason);
        await Persist(f, actor, false); await actor.Drain();
        using var stored = f.Context();
        if (expected == 0)
        {
            Assert.Empty(stored.Item);
            Assert.Empty(PathItemPackets(session).OfType<ServerItemAdd>());
        }
        else
        {
            Assert.Equal(expected, Assert.Single(stored.Item).StackCount);
            var last = ItemQuantityPackets(session).Last();
            uint count = last is ServerItemAdd added ? added.InventoryItem.Item.StackCount : Assert.IsType<ServerItemStackCountUpdate>(last).StackCount;
            Assert.Equal(expected, count);
        }
    }

    [Fact]
    public async Task PathItems_OrdinaryConsumptionDuringAuthWaitsForItsOwnCommittedCount()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f);
        using var actor = CreatePathItemActor(f, out var session);
        await actor.Load(); var path = ConfigurePathItemReward(f, actor);
        path.AddXp(1000);
        var currency = new CurrencyManager(f.Source.Tables); currency.Initialise(actor.Player, new CharacterModel { Id = 42 });
        actor.Set(nameof(Player.CurrencyManager), currency);
        actor.OnAuth = () => actor.Player.Inventory.ItemDelete(10, 1, ItemUpdateReason.NoReason);
        await actor.Player.SaveDirect(); await actor.Drain();
        Assert.Equal(3u, Assert.IsType<ServerItemAdd>(Assert.Single(ItemQuantityPackets(session))).InventoryItem.Item.StackCount);
        using (var first = f.Context()) Assert.Equal(3u, Assert.Single(first.Item).StackCount);
        actor.OnAuth = null; await actor.Player.SaveDirect(); await actor.Drain();
        Assert.Equal(2u, Assert.IsType<ServerItemStackCountUpdate>(ItemQuantityPackets(session).Last()).StackCount);
        using var final = f.Context(); Assert.Equal(2u, Assert.Single(final.Item).StackCount);
    }

    private static IEnumerable<object> ItemQuantityPackets(RecordingDispatchProxy<IWorldSession> session)
        => session.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0])
            .Where(packet => packet is ServerItemAdd or ServerItemStackCountUpdate);

    private static Actor CreatePathItemActor(Fixture f, out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        var session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        var events = new EventQueue(); sessionProxy.SetProperty(nameof(IWorldSession.Events), events);
        var actor = new Actor(f, actorSession: session, actorEvents: events,
            inventoryFactory: (owner, _) => BankDeferredCurrencyRecoveryTests.Inventory(owner,
                (IItemManager)(object)f.Source.ManagerProxy, f.Source.Tables, null));
        sessionProxy.SetProperty(nameof(IWorldSession.Player), actor.Player);
        ((RecordingDispatchProxy<IAccount>)(object)actor.Player.Account).SetProperty(nameof(IAccount.Session), session);
        return actor;
    }

    private static PathManager ConfigurePathItemReward(Fixture f, Actor actor)
    {
        var (_, path) = Configure(f, actor, true);
        actor.Player.IsLoading = false;
        var info = RecordingDispatchProxy<IItemInfo>.Create(out var proxy);
        proxy.SetProperty(nameof(IItemInfo.Id), 10u);
        proxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 10, MaxStackCount = 10, MaxCharges = 1 });
        proxy.SetMethodReturn(nameof(IItemInfo.IsStackable), true);
        f.Source.ManagerProxy.SetMethodHandler(nameof(IItemManager.GetItemInfo), _ => info);
        var tables = (GameTableManager)typeof(PathManager).GetField("gameTableManager", Instance)!.GetValue(path)!;
        typeof(GameTableManager).GetField("<PathReward>k__BackingField", Instance)!.SetValue(tables,
            PlayerBindPointTests.Table(new PathRewardEntry { Id = 1, ObjectId = PathRewardGrant.GetLevelRewardObjectId(GamePath.Soldier, 2), Item2Id = 10, Count = 3 }));
        return path;
    }

    private static IEnumerable<object> PathItemPackets(RecordingDispatchProxy<IWorldSession> session)
        => session.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).Where(packet =>
            packet is ServerItemAdd { InventoryItem.Reason: ItemUpdateReason.PathReward }
                or ServerItemStackCountUpdate { Reason: ItemUpdateReason.PathReward });
}
