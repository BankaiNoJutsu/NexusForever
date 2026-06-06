using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Account.Unlock;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Account.Inventory;
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
using NexusForever.Shared;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Account;
using NexusForever.WorldServer.Network.Message.Handler.Character;
using NexusForever.WorldServer.Network.Message.Handler.Item;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Tests.Account.Inventory;

[Collection(LegacyServiceProviderCollection.Name)]
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
    public void TakeItem_WithoutPlayer_AccountEntitlementGrant_AppliesAndDeletesItem()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        using ServiceProvider provider = BuildGameTableProvider(
            [
                CreateCharacterSlotAccountItemEntry()
            ],
            [
                CreateCharacterSlotEntitlementEntry()
            ]);
        LegacyServiceProvider.Provider = provider;

        try
        {
            AccountInventoryManager manager = CreateAccountInventoryManager(
                accountItemId: 133u,
                out RecordingDispatchProxy<IWorldSession> sessionProxy,
                out _,
                out RecordingDispatchProxy<IAccountEntitlementManager> entitlementProxy);

            AccountOperationResult result = manager.TakeItem(null, 1ul);

            Assert.Equal(AccountOperationResult.Ok, result);
            RecordingDispatchProxy<IAccountEntitlementManager>.Invocation update =
                Assert.Single(entitlementProxy.GetInvocations(nameof(IAccountEntitlementManager.UpdateEntitlement)));
            Assert.Equal(EntitlementType.BaseCharacterSlots, update.Arguments[0]);
            Assert.Equal(1, update.Arguments[1]);
            Assert.Null(manager.GetItem(1ul));

            ServerAccountOperationResult operationResult = Assert.Single(GetEncryptedMessages<ServerAccountOperationResult>(sessionProxy));
            Assert.Equal(AccountOperation.TakeItem, operationResult.Operation);
            Assert.Equal(AccountOperationResult.Ok, operationResult.Result);
            Assert.Single(GetEncryptedMessages<ServerAccountItemDelete>(sessionProxy));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void TakeItem_WithPlayer_AccountEntitlementGrant_AppliesWithoutPlayerPrerequisite()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        using ServiceProvider provider = BuildGameTableProvider(
            [
                CreateCharacterSlotAccountItemEntry()
            ],
            [
                CreateCharacterSlotEntitlementEntry()
            ]);
        LegacyServiceProvider.Provider = provider;

        try
        {
            AccountInventoryManager manager = CreateAccountInventoryManager(
                accountItemId: 133u,
                out RecordingDispatchProxy<IWorldSession> sessionProxy,
                out _,
                out RecordingDispatchProxy<IAccountEntitlementManager> entitlementProxy);
            IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

            AccountOperationResult result = manager.TakeItem(player, 1ul);

            Assert.Equal(AccountOperationResult.Ok, result);
            RecordingDispatchProxy<IAccountEntitlementManager>.Invocation update =
                Assert.Single(entitlementProxy.GetInvocations(nameof(IAccountEntitlementManager.UpdateEntitlement)));
            Assert.Equal(EntitlementType.BaseCharacterSlots, update.Arguments[0]);
            Assert.Equal(1, update.Arguments[1]);
            Assert.Null(manager.GetItem(1ul));

            ServerAccountOperationResult operationResult = Assert.Single(GetEncryptedMessages<ServerAccountOperationResult>(sessionProxy));
            Assert.Equal(AccountOperation.TakeItem, operationResult.Operation);
            Assert.Equal(AccountOperationResult.Ok, operationResult.Result);
            Assert.Single(GetEncryptedMessages<ServerAccountItemDelete>(sessionProxy));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void TakeItem_WithPlayer_TargetedAccountEntitlementGrant_AppliesToAccount()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        using ServiceProvider provider = BuildGameTableProvider(
            [
                CreateCharacterSlotAccountItemEntry()
            ],
            [
                CreateCharacterSlotEntitlementEntry()
            ]);
        LegacyServiceProvider.Provider = provider;

        try
        {
            AccountInventoryManager manager = CreateAccountInventoryManager(
                accountItemId: 133u,
                out RecordingDispatchProxy<IWorldSession> sessionProxy,
                out _,
                out RecordingDispatchProxy<IAccountEntitlementManager> entitlementProxy,
                targetPlayerIdentity: new NetworkIdentity
                {
                    RealmId = 1,
                    Id      = 321ul
                },
                hasTargetPlayerIdentity: true);
            IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
            playerProxy.SetProperty(nameof(IPlayer.Identity), new NexusForever.Game.Abstract.Identity
            {
                RealmId = 1,
                Id      = 321ul
            });

            AccountOperationResult result = manager.TakeItem(player, 1ul);

            Assert.Equal(AccountOperationResult.Ok, result);
            RecordingDispatchProxy<IAccountEntitlementManager>.Invocation update =
                Assert.Single(entitlementProxy.GetInvocations(nameof(IAccountEntitlementManager.UpdateEntitlement)));
            Assert.Equal(EntitlementType.BaseCharacterSlots, update.Arguments[0]);
            Assert.Equal(1, update.Arguments[1]);
            Assert.Null(manager.GetItem(1ul));

            ServerAccountOperationResult operationResult = Assert.Single(GetEncryptedMessages<ServerAccountOperationResult>(sessionProxy));
            Assert.Equal(AccountOperation.TakeItem, operationResult.Operation);
            Assert.Equal(AccountOperationResult.Ok, operationResult.Result);
            Assert.Single(GetEncryptedMessages<ServerAccountItemDelete>(sessionProxy));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void TakeItem_WithoutPlayer_AccountCurrencyGrant_AddsCurrencyAndDeletesItem()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        using ServiceProvider provider = BuildGameTableProvider(
            [
                new AccountItemEntry
                {
                    Id                    = 901u,
                    AccountCurrencyEnum   = (uint)AccountCurrencyType.Omnibit,
                    AccountCurrencyAmount = 610ul
                }
            ],
            []);
        LegacyServiceProvider.Provider = provider;

        try
        {
            AccountInventoryManager manager = CreateAccountInventoryManager(
                accountItemId: 901u,
                out RecordingDispatchProxy<IWorldSession> sessionProxy,
                out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy,
                out _);

            AccountOperationResult result = manager.TakeItem(null, 1ul);

            Assert.Equal(AccountOperationResult.Ok, result);
            RecordingDispatchProxy<IAccountCurrencyManager>.Invocation add =
                Assert.Single(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencyAddAmount)));
            Assert.Equal(AccountCurrencyType.Omnibit, add.Arguments[0]);
            Assert.Equal(610ul, add.Arguments[1]);
            Assert.Null(manager.GetItem(1ul));
            Assert.Single(GetEncryptedMessages<ServerAccountItemDelete>(sessionProxy));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void TakeHandler_WithoutPlayerSuccessfulClaim_RefreshesCharacterList()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        LegacyServiceProvider.Provider = provider;

        try
        {
            IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
            IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
            IAccountInventoryManager inventoryManager = RecordingDispatchProxy<IAccountInventoryManager>.Create(out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy);
            ICharacterListManager characterListManager = RecordingDispatchProxy<ICharacterListManager>.Create(out RecordingDispatchProxy<ICharacterListManager> characterListProxy);

            sessionProxy.SetProperty(nameof(IWorldSession.Account), account);
            sessionProxy.SetProperty(nameof(IWorldSession.Player), null);
            accountProxy.SetProperty(nameof(IAccount.InventoryManager), inventoryManager);
            inventoryProxy.SetMethodReturn(nameof(IAccountInventoryManager.TakeItem), AccountOperationResult.Ok);

            var handler = new ClientAccountItemTakeHandler(
                NullLogger<ClientAccountItemTakeHandler>.Instance,
                characterListManager);

            var request = new ClientAccountItemTake();
            SetAutoProperty(request, nameof(ClientAccountItemTake.Id), 7ul);

            handler.HandleMessage(session, request);

            RecordingDispatchProxy<IAccountInventoryManager>.Invocation take =
                Assert.Single(inventoryProxy.GetInvocations(nameof(IAccountInventoryManager.TakeItem)));
            Assert.Null(take.Arguments[0]);
            Assert.Equal(7ul, take.Arguments[1]);
            Assert.Single(characterListProxy.GetInvocations(nameof(ICharacterListManager.SendCharacterListPackets)));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void GenericUnlock_WithMissingUnlockSetReturnsInvalidWithoutConsumingItem()
    {
        GenericUnlockEnvironment environment = CreateGenericUnlockEnvironment(
            itemGenericUnlockSetId: 99u,
            unlockSets: [],
            unlockEntries: []);

        var handler = new ClientItemGenericUnlockHandler(
            environment.GameTableManager,
            NullLogger<ClientItemGenericUnlockHandler>.Instance);

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

        var handler = new ClientItemGenericUnlockHandler(
            environment.GameTableManager,
            NullLogger<ClientItemGenericUnlockHandler>.Instance);

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

        var handler = new ClientItemGenericUnlockHandler(
            environment.GameTableManager,
            NullLogger<ClientItemGenericUnlockHandler>.Instance);

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

        var handler = new ClientItemGenericUnlockHandler(
            environment.GameTableManager,
            NullLogger<ClientItemGenericUnlockHandler>.Instance);

        handler.HandleMessage(environment.Session, new ClientItemGenericUnlock());

        RecordingDispatchProxy<IInventory>.Invocation itemUse =
            Assert.Single(environment.InventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
        Assert.Same(consumedItem, itemUse.Arguments[0]);
        Assert.Equal([12], environment.UnlockManager.UnlockedEntryIds);
        Assert.Empty(environment.UnlockManager.Results);
    }

    private static AccountInventoryManager CreateAccountInventoryManager(
        uint accountItemId,
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy,
        out RecordingDispatchProxy<IAccountEntitlementManager> entitlementProxy,
        NetworkIdentity targetPlayerIdentity = null,
        bool hasTargetPlayerIdentity = false)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        IAccountCurrencyManager currencyManager = RecordingDispatchProxy<IAccountCurrencyManager>.Create(out currencyProxy);
        IAccountEntitlementManager entitlementManager = RecordingDispatchProxy<IAccountEntitlementManager>.Create(out entitlementProxy);

        accountProxy.SetProperty(nameof(IAccount.Id), 5001u);
        accountProxy.SetProperty(nameof(IAccount.Session), session);
        accountProxy.SetProperty(nameof(IAccount.CurrencyManager), currencyManager);
        accountProxy.SetProperty(nameof(IAccount.EntitlementManager), entitlementManager);
        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);

        var model = new AccountModel
        {
            Id = 5001u
        };
        model.AccountInventory.Add(new AccountInventoryModel
        {
            Id            = 5001u,
            InventoryId   = 1ul,
            AccountItemId = accountItemId,
            ClaimState    = (byte)AccountItemClaimState.CanClaim,
            HasTargetPlayerIdentity = hasTargetPlayerIdentity,
            TargetRealmId = targetPlayerIdentity?.RealmId ?? 0,
            TargetCharacterId = targetPlayerIdentity?.Id ?? 0ul
        });

        return new AccountInventoryManager(account, model);
    }

    private static ServiceProvider BuildGameTableProvider(AccountItemEntry[] accountItems, EntitlementEntry[] entitlements)
    {
        var gameTableManager = (GameTableManager)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.AccountItem), CreateGameTable(accountItems));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.AccountItemCooldownGroup), CreateGameTable<AccountItemCooldownGroupEntry>());
        SetAutoProperty(gameTableManager, nameof(GameTableManager.DailyLoginReward), CreateGameTable<DailyLoginRewardEntry>());
        SetAutoProperty(gameTableManager, nameof(GameTableManager.Entitlement), CreateGameTable(entitlements));

        return new ServiceCollection()
            .AddSingleton(gameTableManager)
            .BuildServiceProvider();
    }

    private static AccountItemEntry CreateCharacterSlotAccountItemEntry()
    {
        return new AccountItemEntry
        {
            Id                   = 133u,
            EntitlementId        = (uint)EntitlementType.BaseCharacterSlots,
            EntitlementCount     = 1u,
            EntitlementScopeEnum = 2u,
            PrerequisiteId       = 39005u
        };
    }

    private static EntitlementEntry CreateCharacterSlotEntitlementEntry()
    {
        return new EntitlementEntry
        {
            Id       = (uint)EntitlementType.BaseCharacterSlots,
            MaxCount = 12u,
            Flags    = (uint)EntitlementFlags.None
        };
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

        public void SendCharacterUnlockSync()
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
