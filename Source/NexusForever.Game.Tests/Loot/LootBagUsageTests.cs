using System.Reflection;
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
using NexusForever.Game.Static.Chat;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Chat;
using NexusForever.Network.World.Message.Model.Loot;
using NexusForever.Network.World.Message.Model.Story;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Loot;

[Collection(LegacyServiceProviderCollection.Name)]
public class LootBagUsageTests
{
    private const uint LootBagItemId = 84623u;

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

            RecordingDispatchProxy<IGameSession>.Invocation floaterCall = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .First(i => i.Arguments[0] is ServerGenericFloaterString);
            var floater = Assert.IsType<ServerGenericFloaterString>(floaterCall.Arguments[0]);
            Assert.Equal("+5 Omnibit", floater.Text);

            RecordingDispatchProxy<IGameSession>.Invocation chatCall = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .First(i => i.Arguments[0] is ServerChat);
            var chat = Assert.IsType<ServerChat>(chatCall.Arguments[0]);
            Assert.Equal(ChatChannelType.Loot, chat.Channel.ChatChannelId);
            Assert.Equal("You receive 5 Omnibit.", chat.Text);

            RecordingDispatchProxy<IGameSession>.Invocation sessionCall = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .First(i => i.Arguments[0] is ServerLootNotify);
            var notify = Assert.IsType<ServerLootNotify>(sessionCall.Arguments[0]);
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

    private static GlobalLootManager CreateLootManager(LootGroup lootGroup)
    {
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        var manager = new GlobalLootManager(groupStateManager);

        Dictionary<uint, List<LootGroup>> itemLoot = (Dictionary<uint, List<LootGroup>>)typeof(GlobalLootManager)
            .GetField("itemLoot", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(manager)!;
        itemLoot[LootBagItemId] = [lootGroup];

        return manager;
    }

    private static IServiceProvider BuildProvider(GlobalLootManager manager, GameTable<AccountCurrencyTypeEntry> accountCurrencyTypeTable)
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));

        SetAutoProperty(gameTableManager, nameof(GameTableManager.AccountCurrencyType), accountCurrencyTypeTable);

        return new ServiceCollection()
            .AddSingleton(manager)
            .AddSingleton(gameTableManager)
            .BuildServiceProvider();
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
        return new LootGroup(new LootGroupModel
        {
            Id          = items[0].Id,
            Probability = 100f,
            MinDrop     = 1u,
            MaxDrop     = 1u,
            Item        = items
        }, loadChildren: false);
    }

    private static IItem CreateLootBagItem()
    {
        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new GameTable.Model.Item2Entry
        {
            Id = LootBagItemId
        });

        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);
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
