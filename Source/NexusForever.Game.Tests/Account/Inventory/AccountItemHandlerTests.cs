using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database.Auth;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Account.Unlock;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.GenericUnlock;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.GenericUnlock;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Account;
using NexusForever.WorldServer.Network.Message.Handler.Item;

namespace NexusForever.Game.Tests.Account.Inventory;

public class AccountItemHandlerTests
{
    [Fact]
    public void GiftPendingItemGroupToAccount_WithNonzeroReservedFieldRejectsBeforeManagerTransfer()
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IAccountInventoryManager inventoryManager = RecordingDispatchProxy<IAccountInventoryManager>.Create(out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        accountProxy.SetProperty(nameof(IAccount.InventoryManager), inventoryManager);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 123u);

        var handler = new ClientAccountItemGiftPendingItemGroupToAccountHandler(
            NullLogger<ClientAccountItemGiftPendingItemGroupToAccountHandler>.Instance);

        handler.HandleMessage(session, CreateAccountGiftRequest(reservedZero: 1u));

        Assert.Empty(inventoryProxy.GetInvocations(nameof(IAccountInventoryManager.GiftPendingItemGroupToAccount)));
        Assert.Single(inventoryProxy.GetInvocations(nameof(IAccountInventoryManager.SendPendingItems)));

        ServerAccountOperationResult result = Assert.Single(GetEncryptedMessages<ServerAccountOperationResult>(sessionProxy));
        Assert.Equal(AccountOperation.GiftItem, result.Operation);
        Assert.Equal(AccountOperationResult.GenericFail, result.Result);
    }

    [Fact]
    public void GenericUnlock_WithMissingUnlockSetReturnsInvalidWithoutConsumingItem()
    {
        GenericUnlockEnvironment environment = CreateGenericUnlockEnvironment(
            itemGenericUnlockSetId: 99u,
            unlockSets: [],
            unlockEntries: []);

        var handler = new ClientItemGenericUnlockHandler(environment.GameTableManager);

        handler.HandleMessage(environment.Session, new ClientItemGenericUnlock());

        Assert.Equal([GenericUnlockResult.Invalid], environment.UnlockManager.Results);
        Assert.Empty(environment.UnlockManager.UnlockedEntryIds);
        Assert.Empty(environment.InventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
    }

    [Fact]
    public void GenericUnlock_WithMissingUnlockEntryReturnsInvalidWithoutConsumingItem()
    {
        GenericUnlockEnvironment environment = CreateGenericUnlockEnvironment(
            itemGenericUnlockSetId: 10u,
            unlockSets:
            [
                new GenericUnlockSetEntry
                {
                    Id = 10u,
                    GenericUnlockEntryId00 = 11u
                }
            ],
            unlockEntries: []);

        var handler = new ClientItemGenericUnlockHandler(environment.GameTableManager);

        handler.HandleMessage(environment.Session, new ClientItemGenericUnlock());

        Assert.Equal([GenericUnlockResult.Invalid], environment.UnlockManager.Results);
        Assert.Empty(environment.UnlockManager.UnlockedEntryIds);
        Assert.Empty(environment.InventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
    }

    [Fact]
    public void GenericUnlock_WhenAllEntriesAlreadyUnlockedReturnsAlreadyUnlockedWithoutConsumingItem()
    {
        GenericUnlockEnvironment environment = CreateGenericUnlockEnvironment(
            itemGenericUnlockSetId: 10u,
            unlockSets:
            [
                new GenericUnlockSetEntry
                {
                    Id = 10u,
                    GenericUnlockEntryId00 = 11u,
                    GenericUnlockEntryId01 = 12u
                }
            ],
            unlockEntries:
            [
                CreateGenericUnlockEntry(11u, 200u),
                CreateGenericUnlockEntry(12u, 201u)
            ],
            alreadyUnlockedObjects: [200u, 201u]);

        var handler = new ClientItemGenericUnlockHandler(environment.GameTableManager);

        handler.HandleMessage(environment.Session, new ClientItemGenericUnlock());

        Assert.Equal([GenericUnlockResult.AlreadyUnlocked], environment.UnlockManager.Results);
        Assert.Empty(environment.UnlockManager.UnlockedEntryIds);
        Assert.Empty(environment.InventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
    }

    [Fact]
    public void GenericUnlock_WithMixedEntriesConsumesItemAndUnlocksOnlyMissingEntries()
    {
        IItem consumedItem;
        GenericUnlockEnvironment environment = CreateGenericUnlockEnvironment(
            itemGenericUnlockSetId: 10u,
            unlockSets:
            [
                new GenericUnlockSetEntry
                {
                    Id = 10u,
                    GenericUnlockEntryId00 = 11u,
                    GenericUnlockEntryId01 = 12u
                }
            ],
            unlockEntries:
            [
                CreateGenericUnlockEntry(11u, 200u),
                CreateGenericUnlockEntry(12u, 201u)
            ],
            alreadyUnlockedObjects: [200u],
            itemUseResult: true,
            item: out consumedItem);

        var handler = new ClientItemGenericUnlockHandler(environment.GameTableManager);

        handler.HandleMessage(environment.Session, new ClientItemGenericUnlock());

        RecordingDispatchProxy<IInventory>.Invocation itemUse =
            Assert.Single(environment.InventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
        Assert.Same(consumedItem, itemUse.Arguments[0]);
        Assert.Equal([12], environment.UnlockManager.UnlockedEntryIds);
        Assert.Empty(environment.UnlockManager.Results);
    }

    private static ClientAccountItemGiftPendingItemGroupToAccount CreateAccountGiftRequest(uint reservedZero)
    {
        var request = new ClientAccountItemGiftPendingItemGroupToAccount();
        SetAutoProperty(request, nameof(ClientAccountItemGiftPendingItemGroupToAccount.Group), "group-a");
        SetAutoProperty(request, nameof(ClientAccountItemGiftPendingItemGroupToAccount.TargetAccountId), 2002ul);
        SetAutoProperty(request, nameof(ClientAccountItemGiftPendingItemGroupToAccount.ReservedZero), reservedZero);
        request.SenderCharacter.RealmId = 1;
        request.SenderCharacter.Id = 101ul;
        return request;
    }

    private static GenericUnlockEnvironment CreateGenericUnlockEnvironment(
        uint itemGenericUnlockSetId,
        GenericUnlockSetEntry[] unlockSets,
        GenericUnlockEntryEntry[] unlockEntries,
        uint[] alreadyUnlockedObjects = null,
        bool itemUseResult = false)
    {
        return CreateGenericUnlockEnvironment(
            itemGenericUnlockSetId,
            unlockSets,
            unlockEntries,
            alreadyUnlockedObjects,
            itemUseResult,
            out _);
    }

    private static GenericUnlockEnvironment CreateGenericUnlockEnvironment(
        uint itemGenericUnlockSetId,
        GenericUnlockSetEntry[] unlockSets,
        GenericUnlockEntryEntry[] unlockEntries,
        uint[] alreadyUnlockedObjects,
        bool itemUseResult,
        out IItem item)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out RecordingDispatchProxy<IInventory> inventoryProxy);
        item = RecordingDispatchProxy<IItem>.Create(out RecordingDispatchProxy<IItem> itemProxy);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out RecordingDispatchProxy<IItemInfo> itemInfoProxy);

        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> gameTableProxy);
        var unlockManager = new RecordingGenericUnlockManager(alreadyUnlockedObjects ?? []);

        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        accountProxy.SetProperty(nameof(IAccount.GenericUnlockManager), unlockManager);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            GenericUnlockSetId = itemGenericUnlockSetId
        });
        inventoryProxy.SetMethodReturn(nameof(IInventory.GetItem), item);
        inventoryProxy.SetMethodReturn(nameof(IInventory.ItemUse), itemUseResult);
        gameTableProxy.SetProperty(nameof(IGameTableManager.GenericUnlockSet), CreateGameTable(unlockSets));
        gameTableProxy.SetProperty(nameof(IGameTableManager.GenericUnlockEntry), CreateGameTable(unlockEntries));

        return new GenericUnlockEnvironment(session, gameTableManager, unlockManager, inventoryProxy);
    }

    private static GenericUnlockEntryEntry CreateGenericUnlockEntry(uint id, uint unlockObject)
    {
        return new GenericUnlockEntryEntry
        {
            Id = id,
            GenericUnlockTypeEnum = GenericUnlockType.Dye,
            UnlockObject = unlockObject
        };
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

    private static IReadOnlyList<T> GetEncryptedMessages<T>(RecordingDispatchProxy<IWorldSession> sessionProxy)
        where T : class, IWritable
    {
        return sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Where(i => i.Arguments.Length == 1)
            .Select(i => i.Arguments[0])
            .OfType<T>()
            .ToList();
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

    private sealed record GenericUnlockEnvironment(
        IWorldSession Session,
        IGameTableManager GameTableManager,
        RecordingGenericUnlockManager UnlockManager,
        RecordingDispatchProxy<IInventory> InventoryProxy);

    private sealed class RecordingGenericUnlockManager : IGenericUnlockManager
    {
        private readonly HashSet<uint> alreadyUnlockedObjects;

        public List<GenericUnlockResult> Results { get; } = [];
        public List<ushort> UnlockedEntryIds { get; } = [];

        public RecordingGenericUnlockManager(IEnumerable<uint> alreadyUnlockedObjects)
        {
            this.alreadyUnlockedObjects = alreadyUnlockedObjects.ToHashSet();
        }

        public void Unlock(ushort genericUnlockEntryId)
        {
            UnlockedEntryIds.Add(genericUnlockEntryId);
        }

        public void UnlockAll(GenericUnlockType type)
        {
        }

        public bool IsUnlocked(GenericUnlockType type, uint objectId)
        {
            return type == GenericUnlockType.Dye && alreadyUnlockedObjects.Contains(objectId);
        }

        public bool IsDyeUnlocked(uint dyeColourRampId)
        {
            return alreadyUnlockedObjects.Contains(dyeColourRampId);
        }

        public void SendUnlock(ushort genericUnlockEntryId)
        {
        }

        public void SendUnlockResult(GenericUnlockResult result)
        {
            Results.Add(result);
        }

        public void SendUnlockList()
        {
        }

        public void Save(AuthContext context)
        {
        }

        public IEnumerator<IGenericUnlock> GetEnumerator()
        {
            return Enumerable.Empty<IGenericUnlock>().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
