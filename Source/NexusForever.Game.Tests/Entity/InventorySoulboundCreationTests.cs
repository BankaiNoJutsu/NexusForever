using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Item;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Entity;

public sealed class InventorySoulboundCreationTests
{
    private const uint ItemId = 9011;

    [Fact]
    public void UnsupportedCustomInventoryFailsWithoutFallingBackToOrdinaryCreation()
    {
        var legacy = new TestInventory(10, createItems: true);
        IInventory inventory = legacy;
        Assert.False(inventory.SupportsSoulboundItemCreation);
        Assert.False(inventory.CanCreateSoulboundItem(InventoryLocation.Inventory, null, 1, out GenericError preflight));
        Assert.Equal(GenericError.ItemNotValidForSlot, preflight);
        Assert.False(inventory.TryCreateSoulboundItem(InventoryLocation.Inventory, null, 1, out GenericError created));
        Assert.Equal(GenericError.ItemNotValidForSlot, created);
        Assert.Empty(legacy.CreatedItems);
        Assert.Empty(legacy.InventoryBag);
    }

    [Fact]
    public void PreflightDoesNotAllocateMutateOrSendAndCountsTheWholeQuantity()
    {
        var f = new Fixture(2);
        GameItem existing = f.Add(5, bound: true);
        f.ResetObservations();
        Assert.True(f.Inventory.SupportsSoulboundItemCreation);
        Assert.True(f.Inventory.CanCreateSoulboundItem(InventoryLocation.Inventory, f.Info, 15, out GenericError success));
        Assert.Equal(GenericError.Ok, success);
        Assert.False(f.Inventory.CanCreateSoulboundItem(InventoryLocation.Inventory, f.Info, 16, out GenericError failure));
        Assert.Equal(GenericError.ItemInventoryFull, failure);
        Assert.Equal(5u, existing.StackCount);
        Assert.Single(f.Bag);
        f.AssertNoEffects();
    }

    [Fact]
    public void CreationFillsOnlyCompatibleBoundStacksAndBindsEveryNewStack()
    {
        var f = new Fixture(3);
        GameItem unbound = f.Add(7, bound: false);
        GameItem bound = f.Add(5, bound: true);
        f.ResetObservations();
        Assert.True(f.Inventory.TryCreateSoulboundItem(InventoryLocation.Inventory, f.Info, 8, out GenericError error, ItemUpdateReason.Loot));
        Assert.Equal(GenericError.Ok, error);
        Assert.Equal(7u, unbound.StackCount);
        Assert.False(unbound.Soulbound);
        Assert.Equal(10u, bound.StackCount);
        Assert.Equal(new uint[] { 7, 10, 3 }, f.Bag.Select(item => item.StackCount));
        Assert.Equal(bound.Guid, Assert.Single(f.Messages.OfType<ServerItemStackCountUpdate>()).Guid);
        ServerItemAdd add = Assert.Single(f.Messages.OfType<ServerItemAdd>());
        Assert.Equal(3u, add.InventoryItem.Item.StackCount);
        Assert.Equal(ItemUpdateReason.Loot, add.InventoryItem.Reason);
        AssertFirstWireBinding(add);
        Assert.True(f.Bag.Last().Soulbound);
    }

    [Fact]
    public void FullBagWithOnlyUnboundSpaceDoesNotTurnThatStackIntoBoundItems()
    {
        var f = new Fixture(1);
        GameItem unbound = f.Add(5, bound: false);
        f.ResetObservations();
        Assert.False(f.Inventory.TryCreateSoulboundItem(InventoryLocation.Inventory, f.Info, 1, out GenericError error));
        Assert.Equal(GenericError.ItemInventoryFull, error);
        Assert.Equal(5u, unbound.StackCount);
        Assert.False(unbound.Soulbound);
        f.AssertNoEffects();
    }

    [Fact]
    public void CompatibleBoundStackCanAcceptTheWholeQuantityWithoutAFreeSlot()
    {
        var f = new Fixture(1);
        GameItem bound = f.Add(5, bound: true);
        f.ResetObservations();
        Assert.True(f.Inventory.TryCreateSoulboundItem(InventoryLocation.Inventory, f.Info, 5, out GenericError error));
        Assert.Equal(GenericError.Ok, error);
        Assert.Equal(10u, bound.StackCount);
        Assert.Empty(f.Messages.OfType<ServerItemAdd>());
        Assert.Empty(f.ItemProxy.GetInvocations("get_NextItemId"));
    }

    [Fact]
    public void InsufficientSplitCapacityDoesNotPartiallyFillAnExistingStack()
    {
        var f = new Fixture(2);
        GameItem bound = f.Add(9, bound: true);
        f.ResetObservations();
        Assert.False(f.Inventory.TryCreateSoulboundItem(InventoryLocation.Inventory, f.Info, 12, out GenericError error));
        Assert.Equal(GenericError.ItemInventoryFull, error);
        Assert.Equal(9u, bound.StackCount);
        Assert.Single(f.Bag);
        f.AssertNoEffects();
    }

    [Theory]
    [InlineData(0u, 5u)]
    [InlineData(2u, 2u)]
    public void SplitCreationPreservesChargeAndExpirationRulesAndFirstPacketBinding(uint requestedCharges, uint expectedCharges)
    {
        var f = new Fixture(3);
        f.Entry.MaxCharges = 5;
        f.Entry.ExpirationTimeMinutes = 30;
        Assert.True(f.Inventory.TryCreateSoulboundItem(InventoryLocation.Inventory, f.Info, 25, out GenericError error,
            ItemUpdateReason.NoReason, requestedCharges));
        Assert.Equal(GenericError.Ok, error);
        Assert.Equal(new uint[] { 10, 10, 5 }, f.Bag.Select(item => item.StackCount));
        Assert.All(f.Bag, item =>
        {
            Assert.True(item.Soulbound);
            Assert.Equal(expectedCharges, item.Charges);
            Assert.Equal(1800u, item.ExpirationTimeLeft);
        });
        ServerItemAdd[] adds = f.Messages.OfType<ServerItemAdd>().ToArray();
        Assert.Equal(3, adds.Length);
        Assert.All(adds, AssertFirstWireBinding);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BoundStacksWithDifferentChargesOrRemainingExpirationAreNotMerged(bool differentCharges)
    {
        var f = new Fixture(2);
        f.Entry.MaxCharges = 5;
        f.Entry.ExpirationTimeMinutes = 30;
        GameItem existing = f.Add(3, bound: true);
        if (differentCharges) existing.Charges = 4;
        else existing.ExpirationTimeLeft = 1799;
        f.ResetObservations();
        Assert.True(f.Inventory.TryCreateSoulboundItem(InventoryLocation.Inventory, f.Info, 1, out _));
        Assert.Equal(3u, existing.StackCount);
        Assert.Equal(1u, f.Bag.Last().StackCount);
        Assert.Equal(5u, f.Bag.Last().Charges);
        Assert.Equal(1800u, f.Bag.Last().ExpirationTimeLeft);
        Assert.Empty(f.Messages.OfType<ServerItemStackCountUpdate>());
    }

    [Fact]
    public void MatchingChargeAndExpirationStackCanMergeWithoutAllocatingAnItem()
    {
        var f = new Fixture(1);
        f.Entry.MaxCharges = 5;
        f.Entry.ExpirationTimeMinutes = 30;
        GameItem existing = f.Add(9, bound: true);
        f.ResetObservations();
        Assert.True(f.Inventory.TryCreateSoulboundItem(InventoryLocation.Inventory, f.Info, 1, out _));
        Assert.Equal(10u, existing.StackCount);
        Assert.Empty(f.ItemProxy.GetInvocations("get_NextItemId"));
    }

    [Fact]
    public void NonstackableQuantityRequiresOnePhysicalSlotPerItem()
    {
        var f = new Fixture(1);
        f.InfoProxy.SetMethodReturn(nameof(IItemInfo.IsStackable), false);
        Assert.False(f.Inventory.TryCreateSoulboundItem(InventoryLocation.Inventory, f.Info, 2, out GenericError error));
        Assert.Equal(GenericError.ItemInventoryFull, error);
        f.AssertNoEffects();
        Assert.True(f.Inventory.TryCreateSoulboundItem(InventoryLocation.Inventory, f.Info, 1, out _));
        Assert.Equal(1u, Assert.Single(f.Bag).StackCount);
    }

    [Fact]
    public void ForcedCreationCannotUseQuantityOnlySatchelCapacityButOrdinaryCreationStillCan()
    {
        var f = new Fixture(0, material: true);
        Assert.True(f.Inventory.CanCreateItem(InventoryLocation.Inventory, f.Info, 4));
        Assert.False(f.Inventory.CanCreateSoulboundItem(InventoryLocation.Inventory, f.Info, 4, out GenericError error));
        Assert.Equal(GenericError.ItemInventoryFull, error);
        Assert.False(f.Inventory.TryCreateSoulboundItem(InventoryLocation.Inventory, f.Info, 4, out _));
        f.AssertNoEffects();
        Assert.Empty(f.Satchel!);
        f.Inventory.ItemCreate(InventoryLocation.Inventory, f.Info, 4);
        Assert.Equal(4u, (uint)Assert.Single(f.Satchel!).Amount);
        Assert.Empty(f.Bag);
        Assert.Single(f.Messages.OfType<ServerSupplySatchelUpdate>());
    }

    [Fact]
    public void ForcedMaterialKeepsItsBoundPhysicalInstanceAndOrdinaryMaterialKeepsSatchelRouting()
    {
        var f = new Fixture(1, material: true);
        Assert.True(f.Inventory.TryCreateSoulboundItem(InventoryLocation.Inventory, f.Info, 4, out _));
        Assert.True(Assert.Single(f.Bag).Soulbound);
        Assert.Empty(f.Satchel!);
        f.Inventory.ItemCreate(InventoryLocation.Inventory, f.Info, 6);
        Assert.Equal(6u, (uint)Assert.Single(f.Satchel!).Amount);
        Assert.Equal(4u, Assert.Single(f.Bag).StackCount);
    }

    [Fact]
    public void OrdinaryNonMaterialCreationKeepsItsUnboundDefault()
    {
        var f = new Fixture(1);
        f.Inventory.ItemCreate(InventoryLocation.Inventory, f.Info, 3);
        Assert.False(Assert.Single(f.Bag).Soulbound);
        Assert.Equal(DynamicItemFlags.None, Assert.Single(f.Messages.OfType<ServerItemAdd>()).InventoryItem.Item.DynamicFlags);
    }

    [Fact]
    public void CollectQuestRefreshUsesTheCompleteBoundAndUnboundInventoryCount()
    {
        var f = new Fixture(3);
        f.Add(7, bound: false);
        f.Add(5, bound: true);
        var manager = RecordingDispatchProxy<IQuestManager>.Create(out var mp);
        var quest = RecordingDispatchProxy<IQuest>.Create(out var qp);
        var objective = RecordingDispatchProxy<IQuestObjective>.Create(out var op);
        var info = RecordingDispatchProxy<IQuestObjectiveInfo>.Create(out var ip);
        ip.SetProperty(nameof(IQuestObjectiveInfo.Id), 42u);
        ip.SetProperty(nameof(IQuestObjectiveInfo.Type), QuestObjectiveType.CollectItem);
        ip.SetProperty(nameof(IQuestObjectiveInfo.Entry), new QuestObjectiveEntry { Data = ItemId });
        op.SetProperty(nameof(IQuestObjective.ObjectiveInfo), info);
        uint progress = 0;
        op.SetMethodReturnFactory("get_Progress", () => progress);
        qp.SetMethodReturnFactory("GetEnumerator", () => ((IEnumerable<IQuestObjective>)new[] { objective }).GetEnumerator());
        qp.SetMethodHandler(nameof(IQuest.ObjectiveUpdate), args => { progress += (uint)args[1]; return null; });
        mp.SetMethodReturn(nameof(IQuestManager.GetActiveQuests), new[] { quest });
        f.PlayerProxy.SetProperty(nameof(IPlayer.QuestManager), manager);
        Assert.True(f.Inventory.TryCreateSoulboundItem(InventoryLocation.Inventory, f.Info, 8, out _));
        Assert.Equal(20u, progress);
        Assert.Equal(20u, f.Inventory.GetItemCount(ItemId));
    }

    [Fact]
    public void ZeroQuantityDoesNothingEvenWithNoFreeSlots()
    {
        var f = new Fixture(0);
        Assert.True(f.Inventory.CanCreateSoulboundItem(InventoryLocation.Inventory, f.Info, 0, out _));
        Assert.True(f.Inventory.TryCreateSoulboundItem(InventoryLocation.Inventory, f.Info, 0, out GenericError error));
        Assert.Equal(GenericError.Ok, error);
        f.AssertNoEffects();
    }

    [Theory]
    [InlineData(InventoryLocation.RealmBank)]
    [InlineData(InventoryLocation.Equipped)]
    public void SharedAndDirectEquipDestinationsCannotBypassTheirExistingGuards(InventoryLocation location)
    {
        var f = new Fixture(2);
        Assert.False(f.Inventory.TryCreateSoulboundItem(location, f.Info, 1, out GenericError error));
        Assert.Equal(GenericError.ItemNotValidForSlot, error);
        f.AssertNoEffects();
    }

    private static void AssertFirstWireBinding(ServerItemAdd message)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream)) { message.Write(writer); writer.FlushBits(); }
        using var reader = new GamePacketReader(new MemoryStream(stream.ToArray()));
        reader.ReadULong(); // item guid
        reader.ReadULong(); // maker
        reader.ReadUInt(18); reader.ReadUInt(9); reader.ReadUInt(); // item/location
        reader.ReadUInt(); reader.ReadUInt(); // stack/charges
        reader.ReadULong(); reader.ReadUInt(); reader.ReadULong(); // circuit/glyph/threshold
        reader.ReadSingle(); reader.ReadUInt(); reader.ReadByte(); reader.ReadUInt();
        Assert.Equal((uint)DynamicItemFlags.Soulbound, reader.ReadUInt() & (uint)DynamicItemFlags.Soulbound);
    }

    [Fact]
    public void ReservedMergeTargetIsExcludedBeforeAnyImmediateCreationEffects()
    {
        var f = new Fixture(1);
        var held = f.Add(3, true);
        f.ResetObservations();
        Assert.True(((Bag)f.Bag).TryReserveRewardMutation(held, out var token));
        using (token)
        {
            Assert.False(f.Inventory.CanCreateItem(InventoryLocation.Inventory, f.Info, 1));
            Assert.False(f.Inventory.CanCreateSoulboundItem(InventoryLocation.Inventory, f.Info, 1, out var preflight));
            Assert.Equal(GenericError.ItemInventoryFull, preflight);
            Assert.False(f.Inventory.TryCreateSoulboundItem(InventoryLocation.Inventory, f.Info, 1, out var result));
            Assert.Equal(GenericError.ItemInventoryFull, result);
            Assert.Equal(3u, held.StackCount);
            f.AssertNoEffects();
        }
        Assert.True(f.Inventory.CanCreateSoulboundItem(InventoryLocation.Inventory, f.Info, 1, out _));
    }

    [Fact]
    public void WholeQuantityBoundCreationSkipsReservedEmptySlotsWithoutCorruptingPhysicalCapacity()
    {
        var f = new Fixture(3);
        Assert.True(((Bag)f.Bag).TryReserveSlots(new Dictionary<uint, IItem> { [0] = null }, out var token));
        using (token)
        {
            Assert.Equal(3u, f.Inventory.GetInventorySlotsRemaining(InventoryLocation.Inventory));
            Assert.False(f.Inventory.CanCreateSoulboundItem(InventoryLocation.Inventory, f.Info, 21, out _));
            f.AssertNoEffects();
            Assert.True(f.Inventory.TryCreateSoulboundItem(InventoryLocation.Inventory, f.Info, 11, out _));
            Assert.Null(f.Bag.GetItem(0u));
            Assert.Equal(10u, f.Bag.GetItem(1u).StackCount);
            Assert.Equal(1u, f.Bag.GetItem(2u).StackCount);
            Assert.Equal(1u, f.Bag.SlotsRemaining);
            Assert.Equal(0u, f.Bag.AvailableSlots);
            Assert.All(f.Messages.OfType<ServerItemAdd>(), AssertFirstWireBinding);
        }
    }

    [Fact]
    public void BoundCreationUsesOtherSlotsInsteadOfChangingAReservedMerge()
    {
        var f = new Fixture(3);
        var held = f.Add(3, true);
        Assert.True(((Bag)f.Bag).TryReserveRewardMutation(held, out var token));
        using (token)
        {
            Assert.True(f.Inventory.TryCreateSoulboundItem(InventoryLocation.Inventory, f.Info, 12, out _));
            Assert.Equal(3u, held.StackCount);
            Assert.Equal(10u, f.Bag.GetItem(1u).StackCount);
            Assert.Equal(2u, f.Bag.GetItem(2u).StackCount);
            Assert.All(f.Messages.OfType<ServerItemAdd>(), AssertFirstWireBinding);
        }
    }

    [Fact]
    public void OrdinaryCreationSkipsReservedMergeAndEmptyDestination()
    {
        var f = new Fixture(3);
        var held = f.Add(3, true);
        Assert.True(((Bag)f.Bag).TryReserveRewardMutation(held, out var itemToken));
        Assert.True(((Bag)f.Bag).TryReserveSlots(new Dictionary<uint, IItem> { [0] = held, [1] = null }, out var bagToken));
        using (itemToken)
        using (bagToken)
        {
            f.Inventory.ItemCreate(InventoryLocation.Inventory, f.Info, 2);
            Assert.Equal(3u, held.StackCount);
            Assert.Null(f.Bag.GetItem(1u));
            Assert.Equal(2u, f.Bag.GetItem(2u).StackCount);
            Assert.False(f.Bag.GetItem(2u).Soulbound); // ordinary creation retains its own binding behavior
            Assert.Single(f.Messages.OfType<ServerItemAdd>());
            Assert.Empty(f.Messages.OfType<ServerItemStackCountUpdate>());
        }
    }

    [Fact]
    public void OrdinaryCreationWithoutAvailableCapacityDoesNotAllocateAnItemId()
    {
        var f = new Fixture(1);
        var held = f.Add(3, true);
        f.ResetObservations();
        Assert.True(((Bag)f.Bag).TryReserveRewardMutation(held, out var token));
        using (token)
        {
            f.Inventory.ItemCreate(InventoryLocation.Inventory, f.Info, 1);
            Assert.Equal(3u, held.StackCount);
            Assert.Empty(f.ItemProxy.GetInvocations("get_NextItemId"));
            Assert.Equal(GenericError.ItemInventoryFull, Assert.Single(f.Messages.OfType<ServerItemError>()).ErrorCode);
        }
    }

    [Fact]
    public void AggregateMovePreflightRejectsBeforeItsGlobalRollbackSnapshotCanOverlapAReservation()
    {
        var f = new Fixture(3);
        var held = f.Add(3, true);
        var other = f.Add(2, false);
        Assert.True(((Bag)f.Bag).TryReserveRewardMutation(held, out var token));
        using (token)
        {
            Assert.Equal(GenericError.ItemNotValidForSlot, f.Inventory.CanMoveItem(other, InventoryLocation.Inventory, 2));
            Assert.Same(other, f.Bag.GetItem(1u));
            Assert.Equal(InventoryLocation.None, other.PreviousLocation);
        }
    }

    [Fact]
    public void AggregateSnapshotRestoreRejectsBeforeRestoringEarlierItemCounts()
    {
        var f = new Fixture(2);
        var held = f.Add(3, true);
        MethodInfo create = typeof(Inventory).GetMethod("CreateOperationSnapshot", BindingFlags.NonPublic | BindingFlags.Instance)!;
        MethodInfo restore = typeof(Inventory).GetMethod("RestoreOperationSnapshot", BindingFlags.NonPublic | BindingFlags.Instance)!;
        object snapshot = create.Invoke(f.Inventory, null)!;
        held.StackCount = 4;
        Assert.True(((Bag)f.Bag).TryReserveRewardMutation(held, out var token));
        using (token)
        {
            var error = Assert.Throws<TargetInvocationException>(() => restore.Invoke(f.Inventory, [snapshot]));
            Assert.IsType<InvalidOperationException>(error.InnerException);
            Assert.Equal(4u, held.StackCount);
            Assert.Same(held, f.Bag.GetItem(0u));
        }
    }

    [Fact]
    public void AggregateResizeRejectsBeforeRelocatingItemsFromTheTail()
    {
        var f = new Fixture(3);
        var held = f.Add(3, true);
        f.Bag.MoveItem(held, 2);
        Assert.True(((Bag)f.Bag).TryReserveRewardMutation(held, out var token));
        using (token)
        {
            MethodInfo resize = typeof(Inventory).GetMethod("InventoryResize", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var error = Assert.Throws<TargetInvocationException>(() => resize.Invoke(f.Inventory, [InventoryLocation.Inventory, -1]));
            Assert.IsType<InvalidOperationException>(error.InnerException);
            Assert.Equal(2u, held.BagIndex);
            Assert.Same(held, f.Bag.GetItem(2u));
            Assert.Equal(3u, f.Bag.Slots);
        }
    }

    private sealed class Fixture
    {
        public Item2Entry Entry { get; } = new() { Id = ItemId, MaxStackCount = 10 };
        public IItemInfo Info { get; }
        public RecordingDispatchProxy<IItemInfo> InfoProxy { get; }
        public RecordingDispatchProxy<IPlayer> PlayerProxy { get; }
        public RecordingDispatchProxy<IItemManager> ItemProxy { get; }
        public RecordingDispatchProxy<IGameSession> SessionProxy { get; }
        public IItemManager ItemManager { get; }
        public Inventory Inventory { get; } = (Inventory)RuntimeHelpers.GetUninitializedObject(typeof(Inventory));
        public IBag Bag { get; }
        public SupplySatchelManager Satchel { get; }
        public IEnumerable<object> Messages => SessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]);

        public Fixture(uint slots, bool material = false)
        {
            var player = RecordingDispatchProxy<IPlayer>.Create(out var pp);
            PlayerProxy = pp;
            pp.SetProperty(nameof(IPlayer.CharacterId), 42ul);
            pp.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out var sp));
            SessionProxy = sp;
            Info = RecordingDispatchProxy<IItemInfo>.Create(out var ip);
            InfoProxy = ip;
            ip.SetProperty(nameof(IItemInfo.Id), ItemId);
            ip.SetProperty(nameof(IItemInfo.Entry), Entry);
            ip.SetMethodReturn(nameof(IItemInfo.IsStackable), true);
            ItemManager = RecordingDispatchProxy<IItemManager>.Create(out var imp);
            ItemProxy = imp;
            ulong next = 1;
            imp.SetMethodReturnFactory("get_NextItemId", () => next++);
            imp.SetMethodReturn(nameof(IItemManager.GetItemInfo), Info);
            IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tp);
            if (material)
            {
                var table = (GameTable<TradeskillMaterialEntry>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<TradeskillMaterialEntry>));
                typeof(GameTable<TradeskillMaterialEntry>).GetProperty(nameof(GameTable<TradeskillMaterialEntry>.Entries))!.SetValue(table,
                    new[] { new TradeskillMaterialEntry { Id = 12, Item2IdStatRevolution = ItemId } });
                typeof(GameTable<TradeskillMaterialEntry>).GetField("header", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(table, new GameTableHeader { MaxId = 13 });
                int[] lookup = Enumerable.Repeat(-1, 13).ToArray(); lookup[12] = 0;
                typeof(GameTable<TradeskillMaterialEntry>).GetField("lookup", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(table, lookup);
                tp.SetProperty(nameof(IGameTableManager.TradeskillMaterial), table);
                Satchel = new SupplySatchelManager(player, new CharacterModel(), tables);
                pp.SetProperty(nameof(IPlayer.SupplySatchelManager), Satchel);
            }
            Bag = new Bag(InventoryLocation.Inventory, slots, ItemManager);
            Set("bags", new Dictionary<InventoryLocation, IBag> { [InventoryLocation.Inventory] = Bag });
            Set("deletedItems", new List<IItem>());
            Set("player", player); Set("characterId", 42ul);
            Set("itemManager", ItemManager); Set("gameTableManager", tables);
            pp.SetProperty(nameof(IPlayer.Inventory), Inventory);
        }

        public GameItem Add(uint count, bool bound)
        {
            var item = new GameItem(42, Info, count, itemManager: ItemManager);
            if (bound) item.MakeSoulbound();
            Bag.AddItem(item, Bag.GetFirstAvailableBagIndex()!.Value);
            return item;
        }

        public void ResetObservations() { ItemProxy.Invocations.Clear(); SessionProxy.Invocations.Clear(); }
        public void AssertNoEffects()
        {
            Assert.Empty(Messages);
            Assert.Empty(ItemProxy.GetInvocations("get_NextItemId"));
        }
        private void Set(string field, object value) => typeof(Inventory).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(Inventory, value);
    }
}

