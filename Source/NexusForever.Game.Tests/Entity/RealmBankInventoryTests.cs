using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.Auth;
using NexusForever.Database.Character.Model;
using NexusForever.Game;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Entity;

[Collection(LegacyServiceProviderCollection.Name)]
public class RealmBankInventoryTests : IDisposable
{
    private readonly IServiceProvider previousProvider;
    private readonly ServiceProvider provider;

    public RealmBankInventoryTests()
    {
        EnsureInventoryLocationCapacities();

        previousProvider = LegacyServiceProvider.Provider;
        provider = new ServiceCollection()
            .AddSingleton(new RealmBankManager())
            .BuildServiceProvider();
        LegacyServiceProvider.Provider = provider;
    }

    public void Dispose()
    {
        LegacyServiceProvider.Provider = previousProvider;
        provider.Dispose();
    }

    [Fact]
    public void AddGame_RegistersRealmBankManagerForLegacySingleton()
    {
        var services = new ServiceCollection();
        services.AddGame();
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetRequiredService<RealmBankManager>());
    }

    [Fact]
    public void GetSlotCapacity_WithoutUnlock_ReturnsZero()
    {
        IPlayer player = CreatePlayer(unlocked: false);

        Assert.Equal(0u, RealmBankManager.Instance.GetSlotCapacity(player));
    }

    [Fact]
    public void GetSlotCapacity_WithExtraSlots_AddsEightPerStack()
    {
        IPlayer player = CreatePlayer(unlocked: true, extraSlotStacks: 3u);

        Assert.Equal(40u, RealmBankManager.Instance.GetSlotCapacity(player));
    }

    [Fact]
    public void CanMoveItem_ToRealmBankWithoutUnlock_ReturnsInvalidSlot()
    {
        var inventory = CreateInventory(unlocked: false);
        IItem item = CreateItem(InventoryLocation.Inventory);

        GenericError? result = inventory.CanMoveItem(item, InventoryLocation.RealmBank, bagIndex: 0u);

        Assert.Equal(GenericError.ItemNotValidForSlot, result);
    }

    [Fact]
    public void CanMoveItem_ToRealmBankOutsideEntitlementCapacity_ReturnsInvalidSlot()
    {
        var inventory = CreateInventory(unlocked: true);
        IItem item = CreateItem(InventoryLocation.Inventory);

        GenericError? result = inventory.CanMoveItem(item, InventoryLocation.RealmBank, bagIndex: 16u);

        Assert.Equal(GenericError.ItemNotValidForSlot, result);
    }

    [Fact]
    public void CanMoveItem_ToRealmBankInsideEntitlementCapacity_AllowsMove()
    {
        var inventory = CreateInventory(unlocked: true);
        IItem item = CreateItem(InventoryLocation.Inventory);

        GenericError? result = inventory.CanMoveItem(item, InventoryLocation.RealmBank, bagIndex: 15u);

        Assert.Null(result);
    }

    [Fact]
    public void CanMoveItem_ToRealmBankBeyondDefaultSlots_ExpandsToEntitlementCapacity()
    {
        var inventory = CreateInventory(unlocked: true, extraSlotStacks: 7u);
        IItem item = CreateItem(InventoryLocation.Inventory);

        GenericError? result = inventory.CanMoveItem(item, InventoryLocation.RealmBank, bagIndex: 71u);

        Assert.Null(result);
    }

    private static NexusForever.Game.Entity.Inventory CreateInventory(bool unlocked, uint extraSlotStacks = 0u)
    {
        IPlayer player = CreatePlayer(unlocked, extraSlotStacks);
        return new NexusForever.Game.Entity.Inventory(player, new CharacterModel());
    }

    private static IPlayer CreatePlayer(bool unlocked, uint extraSlotStacks = 0u)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 1234ul);

        var entitlementManager = new StubAccountEntitlementManager();
        if (unlocked)
            entitlementManager.Set(EntitlementType.SharedRealmBankUnlock, 1u);
        if (extraSlotStacks > 0u)
            entitlementManager.Set(EntitlementType.SharedRealmBankSlots, extraSlotStacks);

        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        accountProxy.SetProperty(nameof(IAccount.EntitlementManager), entitlementManager);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        return player;
    }

    private static IItem CreateItem(InventoryLocation location)
    {
        IItem item = RecordingDispatchProxy<IItem>.Create(out RecordingDispatchProxy<IItem> itemProxy);
        itemProxy.SetProperty(nameof(IItem.Guid), 0x1000ul);
        itemProxy.SetProperty(nameof(IItem.Id), 7001u);
        itemProxy.SetProperty(nameof(IItem.Location), location);
        itemProxy.SetProperty(nameof(IItem.PreviousLocation), InventoryLocation.None);
        itemProxy.SetProperty(nameof(IItem.BagIndex), 0u);
        itemProxy.SetProperty(nameof(IItem.StackCount), 1u);
        itemProxy.SetProperty(nameof(IItem.Info), CreateItemInfo());
        return item;
    }

    private static IItemInfo CreateItemInfo()
    {
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out RecordingDispatchProxy<IItemInfo> itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Id), 7001u);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            Id            = 7001u,
            MaxStackCount = 1u
        });
        return itemInfo;
    }

    private sealed class StubAccountEntitlementManager : IAccountEntitlementManager
    {
        private readonly Dictionary<EntitlementType, uint> amounts = new();

        public void Set(EntitlementType type, uint amount) => amounts[type] = amount;

        public IAccountEntitlement GetEntitlement(EntitlementType type) =>
            amounts.TryGetValue(type, out uint amount) ? new StubAccountEntitlement(type, amount) : null;

        public void UpdateEntitlement(EntitlementType type, int value) => throw new NotSupportedException();

        public void Save(AuthContext context) { }

        public IEnumerator<IAccountEntitlement> GetEnumerator() => throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class StubAccountEntitlement : IAccountEntitlement
    {
        public StubAccountEntitlement(EntitlementType type, uint amount)
        {
            Type   = type;
            Amount = amount;
        }

        public EntitlementEntry Entry => null;
        public EntitlementType Type { get; }
        public uint Amount { get; set; }

        public ServerAccountEntitlement Build() => throw new NotSupportedException();

        public void Save(AuthContext context) { }
    }

    private static void EnsureInventoryLocationCapacities()
    {
        if (AssetManager.InventoryLocationCapacities != null)
            return;

        var entries = new Dictionary<InventoryLocation, uint>();
        foreach (FieldInfo field in typeof(InventoryLocation).GetFields())
        {
            foreach (InventoryLocationAttribute attribute in field.GetCustomAttributes<InventoryLocationAttribute>())
            {
                InventoryLocation location = (InventoryLocation)field.GetValue(null);
                entries.Add(location, attribute.DefaultCapacity);
            }
        }

        typeof(AssetManager)
            .GetProperty(nameof(AssetManager.InventoryLocationCapacities))
            .SetValue(null, entries.ToImmutableDictionary());
    }
}
