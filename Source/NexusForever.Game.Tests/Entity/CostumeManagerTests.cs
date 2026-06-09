using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Costume;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Account.Unlock;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Costume;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Entity;

[Collection(LegacyServiceProviderCollection.Name)]
public class CostumeManagerTests
{
    private const uint CostumeItemId = 101u;
    private const ushort ItemDisplayId = 202;
    private const uint DyeColorRampId = 303u;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SaveCostume_WithMissingDyeColorRampStaticDataSendsInvalidDye(bool includeEmptyTable)
    {
        IItemInfo itemInfo = CreateItemInfo(CostumeItemId, ItemDisplayId);
        using var scope = new LegacyServiceProviderScope(BuildProvider(
            itemInfo,
            CreateGameTable(new ItemDisplayEntry
            {
                Id              = ItemDisplayId,
                DyeChannelFlags = 1u
            }),
            includeEmptyTable ? CreateGameTable<DyeColorRampEntry>() : null));
        CostumeManager manager = CreateManager(out RecordingDispatchProxy<IGameSession> sessionProxy);
        ClientCostumeSave packet = CreateCostumeSave(CostumeItemId, DyeColorRampId);

        manager.SaveCostume(packet);

        ServerCostumeSave result = Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)))
            .Arguments
            .OfType<ServerCostumeSave>()
            .Single();
        Assert.Equal(CostumeSaveResult.InvalidDye, result.Result);
        Assert.Null(manager.GetCostume(0));
    }

    [Fact]
    public void SaveCostume_WithKnownDyeColorRampStaticDataSavesCostume()
    {
        IItemInfo itemInfo = CreateItemInfo(CostumeItemId, ItemDisplayId);
        using var scope = new LegacyServiceProviderScope(BuildProvider(
            itemInfo,
            CreateGameTable(new ItemDisplayEntry
            {
                Id              = ItemDisplayId,
                DyeChannelFlags = 1u
            }),
            CreateGameTable(new DyeColorRampEntry
            {
                Id        = DyeColorRampId,
                RampIndex = 7u
            })));
        CostumeManager manager = CreateManager(out RecordingDispatchProxy<IGameSession> sessionProxy);
        ClientCostumeSave packet = CreateCostumeSave(CostumeItemId, DyeColorRampId);

        manager.SaveCostume(packet);

        ICostume costume = manager.GetCostume(0);
        Assert.NotNull(costume);
        Assert.Equal(CostumeItemId, costume.GetItem(CostumeItemSlot.Chest).ItemId);
        Assert.Equal(CostumeSaveResult.Saved, sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .OfType<ServerCostumeSave>()
            .Single()
            .Result);
        Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .OfType<ServerCostume>());
    }

    [Fact]
    public void SaveCostume_WithMissingItemDisplayStaticDataAndNoDyeSavesCostume()
    {
        IItemInfo itemInfo = CreateItemInfo(CostumeItemId, ItemDisplayId);
        using var scope = new LegacyServiceProviderScope(BuildProvider(
            itemInfo,
            null,
            null));
        CostumeManager manager = CreateManager(out RecordingDispatchProxy<IGameSession> sessionProxy);
        ClientCostumeSave packet = CreateCostumeSave(CostumeItemId, 0u);

        manager.SaveCostume(packet);

        ICostume costume = manager.GetCostume(0);
        Assert.NotNull(costume);
        Assert.Equal(CostumeItemId, costume.GetItem(CostumeItemSlot.Chest).ItemId);
        Assert.Equal(CostumeSaveResult.Saved, sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .OfType<ServerCostumeSave>()
            .Single()
            .Result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GenerateDyeMask_WithMissingDyeColorRampStaticDataThrowsInvalidDye(bool includeEmptyTable)
    {
        using var scope = new LegacyServiceProviderScope(BuildProvider(
            CreateItemInfo(CostumeItemId, ItemDisplayId),
            CreateGameTable<ItemDisplayEntry>(),
            includeEmptyTable ? CreateGameTable<DyeColorRampEntry>() : null));

        Assert.Throws<ArgumentException>(() => CostumeItem.GenerateDyeMask([DyeColorRampId, 0u, 0u]));
    }

    private static CostumeManager CreateManager(out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        IAccount account = CreateAccount();
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out var inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IInventory.GetItemVisuals), Enumerable.Empty<IItemVisual>());

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);

        return new CostumeManager(player, new CharacterModel
        {
            Id                 = 42ul,
            ActiveCostumeIndex = -1
        });
    }

    private static IAccount CreateAccount()
    {
        IAccountCostumeManager accountCostumeManager = RecordingDispatchProxy<IAccountCostumeManager>.Create(out RecordingDispatchProxy<IAccountCostumeManager> accountCostumeProxy);
        accountCostumeProxy.SetMethodReturn(nameof(IAccountCostumeManager.HasItemUnlock), true);

        IGenericUnlockManager genericUnlockManager = RecordingDispatchProxy<IGenericUnlockManager>.Create(out RecordingDispatchProxy<IGenericUnlockManager> genericUnlockProxy);
        genericUnlockProxy.SetMethodReturn(nameof(IGenericUnlockManager.IsDyeUnlocked), true);

        IRewardProperty rewardProperty = RecordingDispatchProxy<IRewardProperty>.Create(out RecordingDispatchProxy<IRewardProperty> rewardPropertyProxy);
        rewardPropertyProxy.SetMethodReturn(nameof(IRewardProperty.GetValue), 4f);

        IRewardPropertyManager rewardPropertyManager = RecordingDispatchProxy<IRewardPropertyManager>.Create(out RecordingDispatchProxy<IRewardPropertyManager> rewardPropertyManagerProxy);
        rewardPropertyManagerProxy.SetMethodReturn(nameof(IRewardPropertyManager.GetRewardProperty), rewardProperty);

        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        accountProxy.SetProperty(nameof(IAccount.CostumeManager), accountCostumeManager);
        accountProxy.SetProperty(nameof(IAccount.GenericUnlockManager), genericUnlockManager);
        accountProxy.SetProperty(nameof(IAccount.RewardPropertyManager), rewardPropertyManager);
        return account;
    }

    private static IItemInfo CreateItemInfo(uint itemId, ushort displayId)
    {
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out RecordingDispatchProxy<IItemInfo> itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Id), itemId);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Properties), ImmutableDictionary<NexusForever.Game.Static.Entity.Property, float>.Empty);
        itemInfoProxy.SetMethodReturn(nameof(IItemInfo.GetDisplayId), displayId);
        itemInfoProxy.SetMethodReturn(nameof(IItemInfo.IsEquippable), true);
        return itemInfo;
    }

    private static ClientCostumeSave CreateCostumeSave(uint itemId, uint dyeColorRampId)
    {
        byte[] data = WritePacket(writer =>
        {
            writer.Write(0);
            writer.Write(CostumeType.Personal, 2u);
            writer.Write(0ul);
            for (int i = 0; i < NexusForever.Network.World.Message.Model.Shared.Costume.MaxCostumeItems; i++)
            {
                writer.Write(i == 0 ? itemId : 0u, 18u);
                writer.Write(i == 0 ? dyeColorRampId : 0u);
                writer.Write(0u);
                writer.Write(0u);
            }

            writer.Write(1u);
            writer.Write(false);
        });

        using var reader = new GamePacketReader(new MemoryStream(data));
        var packet = new ClientCostumeSave();
        packet.Read(reader);
        return packet;
    }

    private static byte[] WritePacket(Action<GamePacketWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }

    private static IServiceProvider BuildProvider(
        IItemInfo itemInfo,
        GameTable<ItemDisplayEntry> itemDisplayTable,
        GameTable<DyeColorRampEntry> dyeColorRampTable)
    {
        var itemManager = (ItemManager)RuntimeHelpers.GetUninitializedObject(typeof(ItemManager));
        SetPrivateField(itemManager, "item", ImmutableDictionary<uint, IItemInfo>.Empty.Add(itemInfo.Id, itemInfo));

        var gameTableManager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        if (itemDisplayTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.ItemDisplay), itemDisplayTable);
        if (dyeColorRampTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.DyeColorRamp), dyeColorRampTable);

        return new ServiceCollection()
            .AddSingleton(itemManager)
            .AddSingleton(gameTableManager)
            .BuildServiceProvider();
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

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
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
        FieldInfo idField = typeof(T).GetField("Id", BindingFlags.Instance | BindingFlags.Public);
        return (uint)idField.GetValue(entry);
    }
}
