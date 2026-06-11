using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Entity;

public class SupplySatchelManagerTests
{
    private const ushort MaterialId = 12;
    private const uint MaterialItemId = 9001u;

    [Fact]
    public void AddAmountToMaterial_WithoutRewardProperty_UsesDefaultStackLimit()
    {
        SupplySatchelManager manager = CreateManager(null, out RecordingDispatchProxy<IGameSession> sessionProxy);
        ITradeskillMaterial material = AddMaterial(manager, amount: 90);

        uint remainder = AddAmountToMaterial(manager, MaterialId, 20u);

        Assert.Equal(10u, remainder);
        Assert.Equal(100, material.Amount);

        RecordingDispatchProxy<IGameSession>.Invocation call = Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        var update = Assert.IsType<ServerSupplySatchelUpdate>(call.Arguments[0]);
        Assert.Equal(MaterialId, update.MaterialId);
        Assert.Equal(100, update.StackCount);
    }

    [Fact]
    public void AddAmountToMaterial_WhenExistingAmountExceedsCap_ReturnsRemainderWithoutMutation()
    {
        SupplySatchelManager manager = CreateManager(50f, out RecordingDispatchProxy<IGameSession> sessionProxy);
        ITradeskillMaterial material = AddMaterial(manager, amount: 75);

        uint remainder = AddAmountToMaterial(manager, MaterialId, 10u);

        Assert.Equal(10u, remainder);
        Assert.Equal(75, material.Amount);
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void AddAmountToMaterial_WhenRewardPropertyExceedsPacketCapacity_ClampsToUshortLimit()
    {
        SupplySatchelManager manager = CreateManager(70_000f, out RecordingDispatchProxy<IGameSession> sessionProxy);
        ITradeskillMaterial material = AddMaterial(manager, amount: ushort.MaxValue - 1);

        uint remainder = AddAmountToMaterial(manager, MaterialId, 10u);

        Assert.Equal(9u, remainder);
        Assert.Equal(ushort.MaxValue, material.Amount);

        RecordingDispatchProxy<IGameSession>.Invocation call = Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        var update = Assert.IsType<ServerSupplySatchelUpdate>(call.Arguments[0]);
        Assert.Equal(MaterialId, update.MaterialId);
        Assert.Equal(ushort.MaxValue, update.StackCount);
    }

    [Fact]
    public void BuildNetworkPacket_IgnoresMaterialIdsOutsidePacketCapacity()
    {
        SupplySatchelManager manager = CreateManager(100f, out _);
        AddMaterial(manager, amount: 5);
        AddMaterial(manager, amount: 7, materialId: 512);

        ushort[] packet = manager.BuildNetworkPacket();

        Assert.Equal(512, packet.Length);
        Assert.Equal(5, packet[MaterialId]);
        Assert.DoesNotContain((ushort)7, packet);
    }

    [Fact]
    public void AddAmount_WithMappedItemCreatesMaterialAndSendsUpdate()
    {
        GameTableManager gameTableManager = BuildGameTableManager(
            new TradeskillMaterialEntry
            {
                Id = MaterialId,
                Item2IdStatRevolution = MaterialItemId
            });
        SupplySatchelManager manager = CreateManager(100f, out RecordingDispatchProxy<IGameSession> sessionProxy, gameTableManager: gameTableManager);

        uint remainder = manager.AddAmount(CreateItem(MaterialItemId), 4u);

        Assert.Equal(0u, remainder);
        ITradeskillMaterial material = Assert.Single(manager);
        Assert.Equal(MaterialId, material.MaterialId);
        Assert.Equal(4, material.Amount);

        RecordingDispatchProxy<IGameSession>.Invocation call = Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        var update = Assert.IsType<ServerSupplySatchelUpdate>(call.Arguments[0]);
        Assert.Equal(MaterialId, update.MaterialId);
        Assert.Equal(4, update.StackCount);
    }

    [Fact]
    public void AddAmount_WithUnmappedItemReturnsRemainderWithoutSatchelUpdate()
    {
        GameTableManager gameTableManager = BuildGameTableManager();
        SupplySatchelManager manager = CreateManager(100f, out RecordingDispatchProxy<IGameSession> sessionProxy, gameTableManager: gameTableManager);
        IItem item = CreateItem(MaterialItemId);

        Assert.True(manager.IsFull(item));
        uint remainder = manager.AddAmount(item, 4u);

        Assert.Equal(4u, remainder);
        Assert.Empty(manager);
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void AddAmount_WithUnmappedMaterialIdReturnsRemainderWithoutSatchelUpdate()
    {
        GameTableManager gameTableManager = BuildGameTableManager();
        SupplySatchelManager manager = CreateManager(100f, out RecordingDispatchProxy<IGameSession> sessionProxy, gameTableManager: gameTableManager);

        uint remainder = manager.AddAmount(MaterialId, 4u);

        Assert.Equal(4u, remainder);
        Assert.Empty(manager);
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void MoveToInventory_WithStaleMaterialEntryDoesNotRemoveOrCreateItem()
    {
        SupplySatchelManager manager = CreateManager(100f, out RecordingDispatchProxy<IGameSession> sessionProxy, out RecordingDispatchProxy<IInventory> inventoryProxy);
        ITradeskillMaterial material = AddMaterial(manager, amount: 5);

        manager.MoveToInventory(MaterialId, 2u);

        Assert.Equal(5, material.Amount);
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
    }

    private static SupplySatchelManager CreateManager(
        float? stackLimit,
        out RecordingDispatchProxy<IGameSession> sessionProxy,
        GameTableManager gameTableManager = null)
    {
        return CreateManager(stackLimit, out sessionProxy, out _, gameTableManager);
    }

    private static SupplySatchelManager CreateManager(
        float? stackLimit,
        out RecordingDispatchProxy<IGameSession> sessionProxy,
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        GameTableManager gameTableManager = null)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        IRewardPropertyManager rewardPropertyManager = RecordingDispatchProxy<IRewardPropertyManager>.Create(out RecordingDispatchProxy<IRewardPropertyManager> rewardManagerProxy);

        if (stackLimit.HasValue)
        {
            IRewardProperty rewardProperty = RecordingDispatchProxy<IRewardProperty>.Create(out RecordingDispatchProxy<IRewardProperty> rewardPropertyProxy);
            rewardPropertyProxy.SetMethodReturn(nameof(IRewardProperty.GetValue), stackLimit.Value);
            rewardManagerProxy.SetMethodReturn(nameof(IRewardPropertyManager.GetRewardProperty), rewardProperty);
        }
        else
            rewardManagerProxy.SetMethodReturn(nameof(IRewardPropertyManager.GetRewardProperty), null);

        accountProxy.SetProperty(nameof(IAccount.RewardPropertyManager), rewardPropertyManager);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);

        return new SupplySatchelManager(player, new CharacterModel(), gameTableManager);
    }

    private static ITradeskillMaterial AddMaterial(SupplySatchelManager manager, ushort amount, ushort materialId = MaterialId)
    {
        ITradeskillMaterial material = RecordingDispatchProxy<ITradeskillMaterial>.Create(out RecordingDispatchProxy<ITradeskillMaterial> materialProxy);
        materialProxy.SetProperty(nameof(ITradeskillMaterial.MaterialId), materialId);
        materialProxy.SetProperty(nameof(ITradeskillMaterial.Amount), amount);

        GetMaterials(manager).Add(materialId, material);
        return material;
    }

    private static uint AddAmountToMaterial(SupplySatchelManager manager, ushort materialId, uint amount)
    {
        MethodInfo method = typeof(SupplySatchelManager).GetMethod("AddAmountToMaterial", BindingFlags.Instance | BindingFlags.NonPublic);
        return (uint)method.Invoke(manager, [materialId, amount]);
    }

    private static Dictionary<ushort, ITradeskillMaterial> GetMaterials(SupplySatchelManager manager)
    {
        FieldInfo field = typeof(SupplySatchelManager).GetField("tradeskillMaterials", BindingFlags.Instance | BindingFlags.NonPublic);
        return (Dictionary<ushort, ITradeskillMaterial>)field.GetValue(manager);
    }

    private static IItem CreateItem(uint itemId)
    {
        IItem item = RecordingDispatchProxy<IItem>.Create(out RecordingDispatchProxy<IItem> itemProxy);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out RecordingDispatchProxy<IItemInfo> itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = itemId });
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);
        return item;
    }

    private static GameTableManager BuildGameTableManager(params TradeskillMaterialEntry[] tradeskillMaterialEntries)
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.TradeskillMaterial), CreateGameTable(tradeskillMaterialEntries));

        return gameTableManager;
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0u : entries.Max(GetEntryId) + 1u
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
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

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }
}
