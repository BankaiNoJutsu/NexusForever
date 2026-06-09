using System.Reflection;
using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Account.Currency;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Chat;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Chat;
using NexusForever.Network.World.Message.Model.Loot;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Model.Story;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Loot;

[Collection(LegacyServiceProviderCollection.Name)]
public class LootBagUsageTests
{
    private const uint LootBagItemId = 84623u;
    private const uint SalvageItemId = 447u;
    private const uint ProtostarFocusMkIIItemId = 82799u;
    private const uint StandardOmniPlasmItemId = 14781u;

    [Fact]
    public void TryUseLootBag_EmptyLootGroup_DoesNotConsumeItem()
    {
        GlobalLootManager manager = CreateLootManager(CreateItemLootGroup(
            new LootItemModel
            {
                Id          = 220000000001,
                Type        = (uint)LootItemType.AccountCurrency,
                StaticId    = (uint)AccountCurrencyType.Omnibit,
                Probability = 0f,
                MinCount    = 5u,
                MaxCount    = 5u
            }));

        IPlayer player = CreatePlayer(out var inventoryProxy, out var currencyProxy, out _, out _);
        IItem item = CreateLootBagItem();

        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(manager, CreateGameTable(
            new AccountCurrencyTypeEntry
            {
                Id = (uint)AccountCurrencyType.Omnibit
            }));

        try
        {
            bool result = manager.TryUseLootBag(player, item, out string reason);

            Assert.False(result);
            Assert.Equal($"empty-item-loot:{LootBagItemId}", reason);
            Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
            Assert.Empty(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencyAddAmount)));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void TryUseLootBag_ItemUseFails_DoesNotDeliverLoot()
    {
        GlobalLootManager manager = CreateLootManager(CreateItemLootGroup(
            new LootItemModel
            {
                Id          = 220000000002,
                Type        = (uint)LootItemType.AccountCurrency,
                StaticId    = (uint)AccountCurrencyType.Omnibit,
                Probability = 100f,
                MinCount    = 5u,
                MaxCount    = 5u
            }));

        IPlayer player = CreatePlayer(out var inventoryProxy, out var currencyProxy, out _, out _);
        inventoryProxy.SetMethodReturn(nameof(IInventory.ItemUse), false);
        IItem item = CreateLootBagItem();

        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(manager, CreateGameTable(
            new AccountCurrencyTypeEntry
            {
                Id = (uint)AccountCurrencyType.Omnibit
            }));

        try
        {
            bool result = manager.TryUseLootBag(player, item, out string reason);

            Assert.False(result);
            Assert.Equal("item-use-failed", reason);

            RecordingDispatchProxy<IInventory>.Invocation itemUseCall = Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
            Assert.Same(item, itemUseCall.Arguments[0]);

            Assert.Empty(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencyAddAmount)));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void TryUseLootBag_AccountCurrencyReward_ConsumesItemAndGrantsLoot()
    {
        GlobalLootManager manager = CreateLootManager(CreateItemLootGroup(
            new LootItemModel
            {
                Id          = 220000000003,
                Type        = (uint)LootItemType.AccountCurrency,
                StaticId    = (uint)AccountCurrencyType.Omnibit,
                Probability = 100f,
                MinCount    = 5u,
                MaxCount    = 5u
            }));

        IPlayer player = CreatePlayer(out var inventoryProxy, out var currencyProxy, out var achievementProxy, out var sessionProxy);
        inventoryProxy.SetMethodReturn(nameof(IInventory.ItemUse), true);
        IItem item = CreateLootBagItem();

        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(manager, CreateGameTable(
            new AccountCurrencyTypeEntry
            {
                Id = (uint)AccountCurrencyType.Omnibit
            }));

        try
        {
            bool result = manager.TryUseLootBag(player, item, out string reason);

            Assert.True(result);
            Assert.Equal(string.Empty, reason);

            RecordingDispatchProxy<IInventory>.Invocation itemUseCall = Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
            Assert.Same(item, itemUseCall.Arguments[0]);

            RecordingDispatchProxy<IAccountCurrencyManager>.Invocation currencyCall = Assert.Single(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencyAddAmount)));
            Assert.Equal(AccountCurrencyType.Omnibit, currencyCall.Arguments[0]);
            Assert.Equal(5ul, currencyCall.Arguments[1]);

            RecordingDispatchProxy<ICharacterAchievementManager>.Invocation achievementCall = achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)).First();
            Assert.Same(player, achievementCall.Arguments[0]);

            IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> sessionCalls = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
            Assert.Equal(2, sessionCalls.Count);
            var notify = Assert.IsType<ServerLootNotify>(sessionCalls[0].Arguments[0]);
            Assert.True(notify.Explosion);
            var singleLootItem = Assert.Single(notify.LootItems);
            Assert.All(notify.LootItems, lootItem =>
            {
                Assert.True(lootItem.Granted);
                Assert.Equal(LootItemType.AccountCurrency, lootItem.Type);
                Assert.Equal((uint)AccountCurrencyType.Omnibit, lootItem.ItemId);
            });
            Assert.Equal(5u, singleLootItem.Amount);
            Assert.Equal(5u, notify.LootItems.Sum(lootItem => lootItem.Amount));

            var remove = Assert.IsType<ServerLootRemove>(sessionCalls[1].Arguments[0]);
            Assert.Equal(player.Guid, remove.OwnerUnitId);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void TryUseLootBag_SingleStackNoChargeReward_DeletesItemAndGrantsLoot()
    {
        GlobalLootManager manager = CreateLootManager(CreateItemLootGroup(
            new LootItemModel
            {
                Id          = 220000000006,
                Type        = (uint)LootItemType.AccountCurrency,
                StaticId    = (uint)AccountCurrencyType.Omnibit,
                Probability = 100f,
                MinCount    = 5u,
                MaxCount    = 5u
            }));

        IPlayer player = CreatePlayer(out var inventoryProxy, out var currencyProxy, out _, out _);
        IItem item = CreateLootBagItem(maxStackCount: 1u, maxCharges: 0u, bagIndex: 7u);
        inventoryProxy.SetMethodReturn(nameof(IInventory.ItemUse), true);
        inventoryProxy.SetMethodReturn(nameof(IInventory.ItemDelete), item);

        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(manager, CreateGameTable(
            new AccountCurrencyTypeEntry
            {
                Id = (uint)AccountCurrencyType.Omnibit
            }));

        try
        {
            bool result = manager.TryUseLootBag(player, item, out string reason);

            Assert.True(result);
            Assert.Equal(string.Empty, reason);

            RecordingDispatchProxy<IInventory>.Invocation itemUseCall = Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
            Assert.Same(item, itemUseCall.Arguments[0]);

            RecordingDispatchProxy<IInventory>.Invocation itemDeleteCall = Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemDelete)));
            ItemLocation location = Assert.IsType<ItemLocation>(itemDeleteCall.Arguments[0]);
            Assert.Equal(InventoryLocation.Inventory, location.Location);
            Assert.Equal(7u, location.BagIndex);
            Assert.Equal(ItemUpdateReason.ConsumeCharge, itemDeleteCall.Arguments[1]);

            RecordingDispatchProxy<IAccountCurrencyManager>.Invocation currencyCall = Assert.Single(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencyAddAmount)));
            Assert.Equal(AccountCurrencyType.Omnibit, currencyCall.Arguments[0]);
            Assert.Equal(5ul, currencyCall.Arguments[1]);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void TryUseLootBag_SingleStackDeleteFailureDoesNotGrantLoot()
    {
        GlobalLootManager manager = CreateLootManager(CreateItemLootGroup(
            new LootItemModel
            {
                Id          = 220000000008,
                Type        = (uint)LootItemType.AccountCurrency,
                StaticId    = (uint)AccountCurrencyType.Omnibit,
                Probability = 100f,
                MinCount    = 5u,
                MaxCount    = 5u
            }));

        IPlayer player = CreatePlayer(out var inventoryProxy, out var currencyProxy, out _, out var sessionProxy);
        IItem item = CreateLootBagItem(maxStackCount: 1u, maxCharges: 0u, bagIndex: 7u);
        inventoryProxy.SetMethodReturn(nameof(IInventory.ItemUse), true);
        inventoryProxy.SetMethodHandler(nameof(IInventory.ItemDelete), _ => throw new ArgumentException());

        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(manager, CreateGameTable(
            new AccountCurrencyTypeEntry
            {
                Id = (uint)AccountCurrencyType.Omnibit
            }));

        try
        {
            bool result = manager.TryUseLootBag(player, item, out string reason);

            Assert.False(result);
            Assert.Equal("item-delete-failed", reason);
            Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
            Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemDelete)));
            Assert.Empty(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencyAddAmount)));
            Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void TryUseLootBag_WithOnlySalvageGroups_DoesNotConsumeItem()
    {
        GlobalLootManager manager = CreateLootManager(CreateItemLootGroup(
            "DataMapping item_salvage: loot bag should ignore this group",
            new LootItemModel
            {
                Id          = 220000000007,
                Type        = (uint)LootItemType.AccountCurrency,
                StaticId    = (uint)AccountCurrencyType.Omnibit,
                Probability = 100f,
                MinCount    = 5u,
                MaxCount    = 5u
            }));

        IPlayer player = CreatePlayer(out var inventoryProxy, out _, out _, out _);
        IItem item = CreateLootBagItem();

        bool result = manager.TryUseLootBag(player, item, out string reason);

        Assert.False(result);
        Assert.Equal($"empty-item-loot:{LootBagItemId}", reason);
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemDelete)));
    }

    [Fact]
    public void TrySalvageItem_ExactRuntimeRowDeletesSourceItemAndGrantsLoot()
    {
        GlobalLootManager manager = CreateLootManagerWithItemSalvage(
            new ItemSalvageModel
            {
                Purpose      = ItemSalvagePurpose.ExactItem,
                SourceItemId = SalvageItemId,
                Type         = (uint)LootItemType.AccountCurrency,
                StaticId     = (uint)AccountCurrencyType.Omnibit,
                Probability  = 100f,
                MinCount     = 5u,
                MaxCount     = 5u
            });

        IPlayer player = CreatePlayer(out var inventoryProxy, out var currencyProxy, out _, out var sessionProxy);
        IItem item = CreateItem(SalvageItemId, maxStackCount: 1u, maxCharges: 0u, bagIndex: 9u);
        inventoryProxy.SetMethodReturn(nameof(IInventory.ItemDelete), item);

        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(manager, CreateGameTable(
            new AccountCurrencyTypeEntry
            {
                Id = (uint)AccountCurrencyType.Omnibit
            }));

        try
        {
            bool result = manager.TrySalvageItem(player, item, out string reason);

            Assert.True(result);
            Assert.Equal(string.Empty, reason);
            Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));

            RecordingDispatchProxy<IInventory>.Invocation itemDeleteCall = Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemDelete)));
            ItemLocation location = Assert.IsType<ItemLocation>(itemDeleteCall.Arguments[0]);
            Assert.Equal(InventoryLocation.Inventory, location.Location);
            Assert.Equal(9u, location.BagIndex);
            Assert.Equal(1u, itemDeleteCall.Arguments[1]);
            Assert.Equal(ItemUpdateReason.Salvage, itemDeleteCall.Arguments[2]);

            RecordingDispatchProxy<IAccountCurrencyManager>.Invocation currencyCall = Assert.Single(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencyAddAmount)));
            Assert.Equal(AccountCurrencyType.Omnibit, currencyCall.Arguments[0]);
            Assert.Equal(5ul, currencyCall.Arguments[1]);

            IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> sessionCalls = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
            Assert.Equal(2, sessionCalls.Count);
            Assert.IsType<ServerLootNotify>(sessionCalls[0].Arguments[0]);
            Assert.IsType<ServerLootRemove>(sessionCalls[1].Arguments[0]);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void TrySalvageItem_WithoutSalvageGroupDoesNotDeleteItem()
    {
        GlobalLootManager manager = CreateLootManager(SalvageItemId, CreateItemLootGroup(
            "DataMapping item_container: not salvage",
            new LootItemModel
            {
                Id          = 230000000002,
                Type        = (uint)LootItemType.AccountCurrency,
                StaticId    = (uint)AccountCurrencyType.Omnibit,
                Probability = 100f,
                MinCount    = 5u,
                MaxCount    = 5u
            }));

        IPlayer player = CreatePlayer(out var inventoryProxy, out var currencyProxy, out _, out _);
        IItem item = CreateItem(SalvageItemId, maxStackCount: 1u, maxCharges: 0u);

        bool result = manager.TrySalvageItem(player, item, out string reason);

        Assert.False(result);
        Assert.Equal($"missing-item-salvage:{SalvageItemId}", reason);
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemDelete)));
        Assert.Empty(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencyAddAmount)));
    }

    [Fact]
    public void TrySalvageItem_WithClientTypeLevelRuntimeRowDeletesSourceItemAndGrantsMaterial()
    {
        GlobalLootManager manager = CreateLootManagerWithItemSalvage(
            new ItemSalvageModel
            {
                Purpose           = ItemSalvagePurpose.ClientTypeLevel,
                SourceItem2TypeId = 301u,
                SourceLevel       = 12u,
                Type              = (uint)LootItemType.StaticItem,
                StaticId          = StandardOmniPlasmItemId,
                Probability       = 100f,
                MinCount          = 1u,
                MaxCount          = 1u
            });

        IPlayer player = CreatePlayer(out var inventoryProxy, out _, out _, out var sessionProxy);
        ConfigureInventoryBag(inventoryProxy, slotsRemaining: 1u);
        IItem item = CreateItem(
            ProtostarFocusMkIIItemId,
            maxStackCount: 1u,
            maxCharges: 0u,
            bagIndex: 10u,
            item2TypeId: 301u,
            powerLevel: 12u,
            requiredLevel: 10u);
        inventoryProxy.SetMethodReturn(nameof(IInventory.ItemDelete), item);

        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(
            manager,
            CreateGameTable<AccountCurrencyTypeEntry>(),
            itemManager: CreateItemManager(CreateStaticItemInfo(StandardOmniPlasmItemId)),
            itemTable: CreateGameTable(new Item2Entry
            {
                Id            = StandardOmniPlasmItemId,
                ItemQualityId = 2u,
                MaxStackCount = 250u
            }));

        try
        {
            bool result = manager.TrySalvageItem(player, item, out string reason);

            Assert.True(result);
            Assert.Equal(string.Empty, reason);

            RecordingDispatchProxy<IInventory>.Invocation itemDeleteCall = Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemDelete)));
            ItemLocation location = Assert.IsType<ItemLocation>(itemDeleteCall.Arguments[0]);
            Assert.Equal(InventoryLocation.Inventory, location.Location);
            Assert.Equal(10u, location.BagIndex);
            Assert.Equal(1u, itemDeleteCall.Arguments[1]);
            Assert.Equal(ItemUpdateReason.Salvage, itemDeleteCall.Arguments[2]);

            RecordingDispatchProxy<IInventory>.Invocation itemCreateCall = Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
            Assert.Equal(InventoryLocation.Inventory, itemCreateCall.Arguments[0]);
            Assert.Equal(StandardOmniPlasmItemId, itemCreateCall.Arguments[1]);
            Assert.Equal(1u, itemCreateCall.Arguments[2]);
            Assert.Equal(ItemUpdateReason.Loot, itemCreateCall.Arguments[3]);

            IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> sessionCalls = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
            Assert.Equal(2, sessionCalls.Count);
            var notify = Assert.IsType<ServerLootNotify>(sessionCalls[0].Arguments[0]);
            NexusForever.Network.World.Message.Model.Loot.LootItem grantedItem = Assert.Single(notify.LootItems);
            Assert.True(grantedItem.Granted);
            Assert.Equal(LootItemType.StaticItem, grantedItem.Type);
            Assert.Equal(StandardOmniPlasmItemId, grantedItem.ItemId);
            Assert.IsType<ServerLootRemove>(sessionCalls[1].Arguments[0]);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void TryUseLootBag_ServiceTokenReward_ConsumesItemAndGrantsLoot()
    {
        GlobalLootManager manager = CreateLootManager(CreateItemLootGroup(
            new LootItemModel
            {
                Id          = 220000000005,
                Type        = (uint)LootItemType.AccountCurrency,
                StaticId    = (uint)AccountCurrencyType.ServiceToken,
                Probability = 100f,
                MinCount    = 140u,
                MaxCount    = 140u
            }));

        IPlayer player = CreatePlayer(out var inventoryProxy, out var currencyProxy, out var achievementProxy, out var sessionProxy);
        inventoryProxy.SetMethodReturn(nameof(IInventory.ItemUse), true);
        IItem item = CreateLootBagItem();

        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(manager, CreateGameTable(
            new AccountCurrencyTypeEntry
            {
                Id = (uint)AccountCurrencyType.ServiceToken
            }));

        try
        {
            bool result = manager.TryUseLootBag(player, item, out string reason);

            Assert.True(result);
            Assert.Equal(string.Empty, reason);

            RecordingDispatchProxy<IInventory>.Invocation itemUseCall = Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
            Assert.Same(item, itemUseCall.Arguments[0]);

            RecordingDispatchProxy<IAccountCurrencyManager>.Invocation currencyCall = Assert.Single(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencyAddAmount)));
            Assert.Equal(AccountCurrencyType.ServiceToken, currencyCall.Arguments[0]);
            Assert.Equal(140ul, currencyCall.Arguments[1]);

            RecordingDispatchProxy<ICharacterAchievementManager>.Invocation achievementCall = achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)).First();
            Assert.Same(player, achievementCall.Arguments[0]);

            IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> sessionCalls = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
            Assert.Equal(2, sessionCalls.Count);
            var notify = Assert.IsType<ServerLootNotify>(sessionCalls[0].Arguments[0]);
            Assert.True(notify.Explosion);
            var singleLootItem = Assert.Single(notify.LootItems);
            Assert.All(notify.LootItems, lootItem =>
            {
                Assert.True(lootItem.Granted);
                Assert.Equal(LootItemType.AccountCurrency, lootItem.Type);
                Assert.Equal((uint)AccountCurrencyType.ServiceToken, lootItem.ItemId);
            });
            Assert.Equal(140u, singleLootItem.Amount);
            Assert.Equal(140u, notify.LootItems.Sum(lootItem => lootItem.Amount));

            var remove = Assert.IsType<ServerLootRemove>(sessionCalls[1].Arguments[0]);
            Assert.Equal(player.Guid, remove.OwnerUnitId);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void TryUseLootBag_AccountCurrencyMissingGameTableEntry_DoesNotConsumeItem()
    {
        GlobalLootManager manager = CreateLootManager(CreateItemLootGroup(
            new LootItemModel
            {
                Id          = 220000000004,
                Type        = (uint)LootItemType.AccountCurrency,
                StaticId    = (uint)AccountCurrencyType.ServiceToken,
                Probability = 100f,
                MinCount    = 1u,
                MaxCount    = 1u
            }));

        IPlayer player = CreatePlayerWithRealAccountCurrencyManager(out var inventoryProxy, out var sessionProxy);
        inventoryProxy.SetMethodReturn(nameof(IInventory.ItemUse), true);
        IItem item = CreateLootBagItem();

        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(manager, CreateGameTable<AccountCurrencyTypeEntry>());

        try
        {
            bool result = manager.TryUseLootBag(player, item, out string reason);

            Assert.False(result);
            Assert.Equal($"invalid-loot-item:{LootItemType.AccountCurrency}:{(uint)AccountCurrencyType.ServiceToken}", reason);
            Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
            Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void TryUseLootBag_AccountCurrencyMissingGameTable_DoesNotConsumeItem()
    {
        GlobalLootManager manager = CreateLootManager(CreateItemLootGroup(
            new LootItemModel
            {
                Id          = 220000000006,
                Type        = (uint)LootItemType.AccountCurrency,
                StaticId    = (uint)AccountCurrencyType.ServiceToken,
                Probability = 100f,
                MinCount    = 1u,
                MaxCount    = 1u
            }));

        IPlayer player = CreatePlayerWithRealAccountCurrencyManager(out var inventoryProxy, out var sessionProxy);
        inventoryProxy.SetMethodReturn(nameof(IInventory.ItemUse), true);
        IItem item = CreateLootBagItem();

        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(manager, null);

        try
        {
            bool result = manager.TryUseLootBag(player, item, out string reason);

            Assert.False(result);
            Assert.Equal($"invalid-loot-item:{LootItemType.AccountCurrency}:{(uint)AccountCurrencyType.ServiceToken}", reason);
            Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
            Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    private static GlobalLootManager CreateLootManager(LootGroup lootGroup)
    {
        return CreateLootManager(LootBagItemId, lootGroup);
    }

    private static GlobalLootManager CreateLootManager()
    {
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        return new GlobalLootManager(groupStateManager);
    }

    private static GlobalLootManager CreateLootManager(uint itemId, LootGroup lootGroup)
    {
        GlobalLootManager manager = CreateLootManager();

        Dictionary<uint, List<LootGroup>> itemLoot = (Dictionary<uint, List<LootGroup>>)typeof(GlobalLootManager)
            .GetField("itemLoot", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(manager)!;
        itemLoot[itemId] = [lootGroup];

        return manager;
    }

    private static GlobalLootManager CreateLootManagerWithItemSalvage(params ItemSalvageModel[] salvageRows)
    {
        GlobalLootManager manager = CreateLootManager();

        Dictionary<uint, List<ItemSalvageModel>> itemSalvageByItem = (Dictionary<uint, List<ItemSalvageModel>>)typeof(GlobalLootManager)
            .GetField("itemSalvageByItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(manager)!;
        Dictionary<(uint Item2TypeId, uint Level), List<ItemSalvageModel>> itemSalvageByTypeLevel = (Dictionary<(uint Item2TypeId, uint Level), List<ItemSalvageModel>>)typeof(GlobalLootManager)
            .GetField("itemSalvageByTypeLevel", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(manager)!;

        foreach (ItemSalvageModel salvageRow in salvageRows)
        {
            switch (salvageRow.Purpose)
            {
                case ItemSalvagePurpose.ExactItem:
                    AddItemSalvage(itemSalvageByItem, salvageRow.SourceItemId, salvageRow);
                    break;
                case ItemSalvagePurpose.ClientTypeLevel:
                    AddItemSalvage(itemSalvageByTypeLevel, (salvageRow.SourceItem2TypeId, salvageRow.SourceLevel), salvageRow);
                    break;
            }
        }

        return manager;
    }

    private static void AddItemSalvage<TKey>(
        Dictionary<TKey, List<ItemSalvageModel>> itemSalvage,
        TKey key,
        ItemSalvageModel salvageRow)
        where TKey : notnull
    {
        if (!itemSalvage.TryGetValue(key, out List<ItemSalvageModel> rows))
        {
            rows = [];
            itemSalvage.Add(key, rows);
        }

        rows.Add(salvageRow);
    }

    private static IServiceProvider BuildProvider(
        GlobalLootManager manager,
        GameTable<AccountCurrencyTypeEntry> accountCurrencyTypeTable,
        ItemManager itemManager = null,
        GameTable<Item2Entry> itemTable = null)
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));

        SetAutoProperty(gameTableManager, nameof(GameTableManager.AccountCurrencyType), accountCurrencyTypeTable);
        SetAutoProperty(gameTableManager, nameof(GameTableManager.Item), itemTable ?? CreateGameTable<Item2Entry>());

        IServiceCollection services = new ServiceCollection()
            .AddSingleton(manager)
            .AddSingleton(gameTableManager);

        if (itemManager != null)
            services.AddSingleton(itemManager);

        return services.BuildServiceProvider();
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0u : entries.Max(GetEntryId) + 1u
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1u)).ToArray();
        for (int i = 0; i < entries.Count; i++)
            lookup[GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        return (uint)typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0].GetValue(entry)!;
    }

    private static LootGroup CreateItemLootGroup(params LootItemModel[] items)
    {
        return CreateItemLootGroup(null, items);
    }

    private static LootGroup CreateItemLootGroup(string comment, params LootItemModel[] items)
    {
        return new LootGroup(new LootGroupModel
        {
            Id          = items[0].Id,
            Probability = 100f,
            MinDrop     = 1u,
            MaxDrop     = 1u,
            Comment     = comment,
            Item        = items
        }, loadChildren: false);
    }

    private static IItem CreateLootBagItem(uint maxStackCount = 0u, uint maxCharges = 0u, InventoryLocation location = InventoryLocation.Inventory, uint bagIndex = 0u)
    {
        return CreateItem(LootBagItemId, maxStackCount, maxCharges, location, bagIndex);
    }

    private static IItem CreateItem(
        uint itemId,
        uint maxStackCount = 0u,
        uint maxCharges = 0u,
        InventoryLocation location = InventoryLocation.Inventory,
        uint bagIndex = 0u,
        uint item2TypeId = 0u,
        uint powerLevel = 0u,
        uint requiredLevel = 0u,
        uint requiredItemLevel = 0u)
    {
        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Id), itemId);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new GameTable.Model.Item2Entry
        {
            Id                = itemId,
            Item2TypeId       = item2TypeId,
            PowerLevel        = powerLevel,
            RequiredLevel     = requiredLevel,
            RequiredItemLevel = requiredItemLevel,
            MaxStackCount     = maxStackCount,
            MaxCharges        = maxCharges
        });

        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);
        itemProxy.SetProperty(nameof(IItem.Location), location);
        itemProxy.SetProperty(nameof(IItem.BagIndex), bagIndex);
        return item;
    }

    private static IPlayer CreatePlayer(
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
        out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        IAccountCurrencyManager currencyManager = RecordingDispatchProxy<IAccountCurrencyManager>.Create(out currencyProxy);
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out achievementProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);

        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty("Guid", 4242u);

        accountProxy.SetProperty(nameof(IAccount.CurrencyManager), currencyManager);

        return player;
    }

    private static void ConfigureInventoryBag(RecordingDispatchProxy<IInventory> inventoryProxy, uint slotsRemaining)
    {
        IBag bag = RecordingDispatchProxy<IBag>.Create(out var bagProxy);
        bagProxy.SetProperty(nameof(IBag.Location), InventoryLocation.Inventory);
        bagProxy.SetProperty(nameof(IBag.SlotsRemaining), slotsRemaining);
        bagProxy.SetMethodReturnFactory("GetEnumerator", () => Enumerable.Empty<IItem>().GetEnumerator());

        inventoryProxy.SetMethodReturnFactory("GetEnumerator", () => new[] { bag }.AsEnumerable().GetEnumerator());
    }

    private static ItemManager CreateItemManager(params IItemInfo[] itemInfos)
    {
        var itemManager = new ItemManager();
        SetPrivateField(itemManager, "item", itemInfos.ToImmutableDictionary(i => i.Id));
        return itemManager;
    }

    private static IItemInfo CreateStaticItemInfo(uint itemId)
    {
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Id), itemId);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            Id            = itemId,
            ItemQualityId = 2u,
            MaxStackCount = 250u
        });
        itemInfoProxy.SetMethodReturn(nameof(IItemInfo.IsStackable), true);
        return itemInfo;
    }

    private static IPlayer CreatePlayerWithRealAccountCurrencyManager(
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        var currencyManager = new AccountCurrencyManager(account, new AccountModel
        {
            Id = 77u
        });

        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty("Guid", 4242u);

        accountProxy.SetProperty(nameof(IAccount.Id), 77u);
        accountProxy.SetProperty(nameof(IAccount.Session), session);
        accountProxy.SetProperty(nameof(IAccount.CurrencyManager), currencyManager);

        return player;
    }
}
