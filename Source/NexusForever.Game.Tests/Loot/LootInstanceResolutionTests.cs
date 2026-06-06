using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Entity;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Loot;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;
using ItemLocation = NexusForever.Network.World.Message.Model.Shared.ItemLocation;

namespace NexusForever.Game.Tests.Loot;

[Collection(LegacyServiceProviderCollection.Name)]
public class LootInstanceResolutionTests
{
    private const uint StaticItemId = 91002u;
    private const ushort RealmId = (ushort)1;

    [Fact]
    public void AddLootItem_DuplicateAmountOverflow_DoesNotWrapExistingAmount()
    {
        using var providerScope = new LegacyServiceProviderScope(BuildProvider(CreateItemInfo()));

        TestPlayer looter = CreatePlayer(characterId: 42ul, guid: 4242u, accountId: 1001u, slotsRemaining: 1u);
        LootInstance lootInstance = CreateLootInstance(looter);

        LootInstanceItem lootItem = lootInstance.AddLootItem(StaticItemId, LootItemType.StaticItem, 10u);
        LootInstanceItem mergedItem = lootInstance.AddLootItem(StaticItemId, LootItemType.StaticItem, 5u);

        Assert.Same(lootItem, mergedItem);
        Assert.Equal(15u, lootItem.Amount);

        LootInstanceItem overflowAttempt = lootInstance.AddLootItem(StaticItemId, LootItemType.StaticItem, uint.MaxValue);

        Assert.Same(lootItem, overflowAttempt);
        Assert.Equal(15u, lootItem.Amount);
        Assert.Single(lootInstance);
    }

    [Fact]
    public void GiveLoot_DirectDeliveryWithRegisteredLooterDoesNotSendLootItemUpdate()
    {
        using var providerScope = new LegacyServiceProviderScope(BuildProvider(CreateItemInfo()));

        try
        {
            TestPlayer looter = CreatePlayer(characterId: 42ul, guid: 4242u, accountId: 1001u, slotsRemaining: 1u);
            PlayerManager.Instance.AddPlayer(looter.Player);

            LootInstance lootInstance = CreateLootInstance(looter);
            LootInstanceItem lootItem = lootInstance.AddLootItem(StaticItemId, LootItemType.StaticItem, 1u);

            IReadOnlyList<object> deliveryMessages = CaptureSessionMessages(
                looter.SessionProxy,
                () => Assert.True(lootInstance.GiveLoot(looter.Player, lootItem.Id)));

            Assert.Contains(deliveryMessages, message => message is ServerLootGrant grant
                && grant.LootItem.LootUnitId == lootItem.Id
                && grant.LootItem.ItemId == StaticItemId);
            Assert.DoesNotContain(deliveryMessages, message => message is ServerLootItemUpdate);
            Assert.True(lootItem.Delivered);
            Assert.Single(looter.Inventory.CreatedItems);
        }
        finally
        {
            foreach (IPlayer player in PlayerManager.Instance.Where(player => player.CharacterId == 42ul).ToList())
                PlayerManager.Instance.RemovePlayer(player);
        }
    }

    [Fact]
    public void RollWinnerOffline_RemainsLootableForWinnerWhenTheyReturn()
    {
        using var providerScope = new LegacyServiceProviderScope(BuildProvider(CreateItemInfo()));

        TestPlayer winner = CreatePlayer(characterId: 42ul, guid: 4242u, accountId: 1001u, slotsRemaining: 1u);
        TestPlayer loser = CreatePlayer(characterId: 43ul, guid: 4343u, accountId: 1002u, slotsRemaining: 1u);

        PlayerManager.Instance.AddPlayer(winner.Player);
        PlayerManager.Instance.AddPlayer(loser.Player);

        LootInstance lootInstance = CreateLootInstance(winner, loser);
        LootInstanceItem lootItem = lootInstance.AddLootItem(StaticItemId, LootItemType.StaticItem, 1u);
        lootItem.ConfigureRoll([winner.Identity, loser.Identity]);

        IReadOnlyList<object> loserFirstRollMessages = CaptureSessionMessages(
            loser.SessionProxy,
            () => Assert.True(lootInstance.RollLoot(winner.Player, lootItem.Id, LootRollAction.Need)));
        Assert.Contains(loserFirstRollMessages, message => message is ServerLootRoll);
        Assert.Contains(loserFirstRollMessages, message => message is ServerLootItemUpdate update
            && update.LootItem.LootUnitId == lootItem.Id
            && update.LootItem.RequiresRoll);

        PlayerManager.Instance.RemovePlayer(winner.Player);
        IReadOnlyList<object> loserFinalRollMessages = CaptureSessionMessages(
            loser.SessionProxy,
            () => Assert.True(lootInstance.RollLoot(loser.Player, lootItem.Id, LootRollAction.Pass)));
        Assert.Contains(loserFinalRollMessages, message => message is ServerLootRoll);
        Assert.Contains(loserFinalRollMessages, message => message is ServerLootWinner);
        Assert.Contains(loserFinalRollMessages, message => message is ServerLootItemUpdate update
            && update.LootItem.LootUnitId == lootItem.Id
            && !update.LootItem.RequiresRoll);

        Assert.False(lootItem.Delivered);
        Assert.True(lootItem.CanLoot(winner.CharacterId));
        Assert.False(lootItem.CanLoot(loser.CharacterId));

        PlayerManager.Instance.AddPlayer(winner.Player);

        RecordingDispatchProxy<IGameSession>.Invocation notifyCall = CaptureSingleSessionCall(
            winner.SessionProxy,
            () => lootInstance.SendLootNotify(winner.Player));

        ServerLootNotify notify = Assert.IsType<ServerLootNotify>(notifyCall.Arguments[0]);
        NexusForever.Network.World.Message.Model.Loot.LootItem networkItem = Assert.Single(notify.LootItems);
        Assert.True(networkItem.CanLoot);
        Assert.False(networkItem.RequiresRoll);
        Assert.False(networkItem.OnlyMasterLootable);

        int winnerMessageCount = GetEncryptedMessageCount(winner.SessionProxy);
        int loserMessageCount = GetEncryptedMessageCount(loser.SessionProxy);
        Assert.True(lootInstance.GiveLoot(winner.Player, lootItem.Id));

        IReadOnlyList<object> winnerDeliveryMessages = GetEncryptedMessages(winner.SessionProxy, winnerMessageCount);
        Assert.Contains(winnerDeliveryMessages, message => message is ServerLootGrant grant
            && grant.LootItem.LootUnitId == lootItem.Id
            && grant.LootItem.ItemId == StaticItemId);
        Assert.DoesNotContain(winnerDeliveryMessages, message => message is ServerLootItemUpdate);

        IReadOnlyList<object> loserDeliveryMessages = GetEncryptedMessages(loser.SessionProxy, loserMessageCount);
        Assert.DoesNotContain(loserDeliveryMessages, message => message is ServerLootItemUpdate);
        Assert.Contains(loserDeliveryMessages, message => message is ServerLootNotification notification
            && notification.LootUnitId == lootItem.Id
            && notification.LooterUnitId == winner.Guid);
        Assert.True(lootItem.Delivered);
        Assert.Single(winner.Inventory.CreatedItems);
    }

    [Fact]
    public void RollAllPasses_MarksDeliveredWithoutGrantingItem()
    {
        using var providerScope = new LegacyServiceProviderScope(BuildProvider(CreateItemInfo()));

        TestPlayer first = CreatePlayer(characterId: 44ul, guid: 4444u, accountId: 1101u, slotsRemaining: 1u);
        TestPlayer second = CreatePlayer(characterId: 45ul, guid: 4545u, accountId: 1102u, slotsRemaining: 1u);

        PlayerManager.Instance.AddPlayer(first.Player);
        PlayerManager.Instance.AddPlayer(second.Player);

        LootInstance lootInstance = CreateLootInstance(first, second);
        LootInstanceItem lootItem = lootInstance.AddLootItem(StaticItemId, LootItemType.StaticItem, 1u);
        lootItem.ConfigureRoll([first.Identity, second.Identity]);

        Assert.True(lootInstance.RollLoot(first.Player, lootItem.Id, LootRollAction.Pass));
        IReadOnlyList<object> finalMessages = CaptureSessionMessages(
            first.SessionProxy,
            () => Assert.True(lootInstance.RollLoot(second.Player, lootItem.Id, LootRollAction.Pass)));

        ServerLootWinner winner = Assert.Single(finalMessages.OfType<ServerLootWinner>());
        Assert.Equal(lootItem.Id, winner.LootUnitId);
        Assert.Equal(0u, winner.WinningRoll.Value);
        Assert.Equal(0ul, winner.WinningRoll.Identity.Id);
        Assert.Equal(2, winner.OtherRolls.Count);
        Assert.All(winner.OtherRolls, roll => Assert.Equal(0u, roll.Value));

        Assert.Contains(finalMessages, message => message is ServerLootItemUpdate update
            && update.LootItem.LootUnitId == lootItem.Id
            && !update.LootItem.RequiresRoll);
        Assert.Contains(finalMessages, message => message is ServerLootRemove remove
            && remove.OwnerUnitId == lootInstance.OwnerUnitId);
        Assert.DoesNotContain(finalMessages, message => message is ServerLootGrant or ServerLootNotification);
        Assert.True(lootItem.Delivered);
        Assert.False(lootItem.CanLoot(first.CharacterId));
        Assert.False(lootItem.CanLoot(second.CharacterId));
        Assert.Empty(first.Inventory.CreatedItems);
        Assert.Empty(second.Inventory.CreatedItems);
    }

    [Fact]
    public void AssignMasterLoot_DeferredDeliveryLeavesResolvedLootForAssignee()
    {
        using var providerScope = new LegacyServiceProviderScope(BuildProvider(CreateItemInfo()));

        TestPlayer master = CreatePlayer(characterId: 52ul, guid: 5252u, accountId: 2001u, slotsRemaining: 1u);
        TestPlayer assignee = CreatePlayer(characterId: 53ul, guid: 5353u, accountId: 2002u, slotsRemaining: 0u);

        PlayerManager.Instance.AddPlayer(master.Player);
        PlayerManager.Instance.AddPlayer(assignee.Player);

        LootInstance lootInstance = CreateLootInstance(master, assignee);
        LootInstanceItem lootItem = lootInstance.AddLootItem(StaticItemId, LootItemType.StaticItem, 1u);
        lootItem.ConfigureMaster(
            [master.Identity],
            [master.Identity, assignee.Identity],
            [master.Identity, assignee.Identity]);

        int masterMessageCount = GetEncryptedMessageCount(master.SessionProxy);
        int assigneeMessageCount = GetEncryptedMessageCount(assignee.SessionProxy);
        Assert.True(lootInstance.AssignMasterLoot(master.Player, lootItem.Id, assignee.Identity));

        IReadOnlyList<object> masterAssignMessages = GetEncryptedMessages(master.SessionProxy, masterMessageCount);
        Assert.Contains(masterAssignMessages, message => message is ServerLootWinner);
        Assert.Contains(masterAssignMessages, message => message is ServerLootItemUpdate update
            && update.LootItem.LootUnitId == lootItem.Id
            && !update.LootItem.OnlyMasterLootable);

        IReadOnlyList<object> assigneeAssignMessages = GetEncryptedMessages(assignee.SessionProxy, assigneeMessageCount);
        Assert.Contains(assigneeAssignMessages, message => message is ServerLootWinner);
        Assert.Contains(assigneeAssignMessages, message => message is ServerLootItemUpdate update
            && update.LootItem.LootUnitId == lootItem.Id
            && !update.LootItem.OnlyMasterLootable);
        Assert.Contains(assigneeAssignMessages, message => message is ServerItemError);
        Assert.False(lootItem.Delivered);
        Assert.True(lootItem.CanLoot(assignee.CharacterId));
        Assert.False(lootItem.CanLoot(master.CharacterId));

        RecordingDispatchProxy<IGameSession>.Invocation notifyCall = CaptureSingleSessionCall(
            assignee.SessionProxy,
            () => lootInstance.SendLootNotify(assignee.Player));

        ServerLootNotify notify = Assert.IsType<ServerLootNotify>(notifyCall.Arguments[0]);
        NexusForever.Network.World.Message.Model.Loot.LootItem networkItem = Assert.Single(notify.LootItems);
        Assert.True(networkItem.CanLoot);
        Assert.False(networkItem.RequiresRoll);
        Assert.False(networkItem.OnlyMasterLootable);

        assignee.Inventory.InventoryBag.SlotsRemaining = 1u;

        Assert.True(lootInstance.GiveLoot(assignee.Player, lootItem.Id));
        Assert.True(lootItem.Delivered);
        Assert.Single(assignee.Inventory.CreatedItems);
    }

    [Fact]
    public void AssignMasterLoot_OfflineAssigneeRejectsWithoutResolvingWinner()
    {
        using var providerScope = new LegacyServiceProviderScope(BuildProvider(CreateItemInfo()));

        TestPlayer master = CreatePlayer(characterId: 62ul, guid: 6262u, accountId: 3001u, slotsRemaining: 1u);
        TestPlayer assignee = CreatePlayer(characterId: 63ul, guid: 6363u, accountId: 3002u, slotsRemaining: 1u);

        PlayerManager.Instance.AddPlayer(master.Player);

        LootInstance lootInstance = CreateLootInstance(master, assignee);
        LootInstanceItem lootItem = lootInstance.AddLootItem(StaticItemId, LootItemType.StaticItem, 1u);
        lootItem.ConfigureMaster(
            [master.Identity],
            [master.Identity, assignee.Identity],
            [master.Identity, assignee.Identity]);

        int masterMessageCount = GetEncryptedMessageCount(master.SessionProxy);
        Assert.False(lootInstance.AssignMasterLoot(master.Player, lootItem.Id, assignee.Identity));

        Assert.Empty(GetEncryptedMessages(master.SessionProxy, masterMessageCount));
        Assert.False(lootItem.Delivered);
        Assert.Equal(0ul, lootItem.WinnerCharacterId);
        Assert.Equal(0u, lootItem.WinnerGuid);
        Assert.True(lootItem.OnlyMasterLootable);
        Assert.False(lootItem.CanLoot(assignee.CharacterId));
        Assert.False(lootItem.CanLoot(master.CharacterId));
    }

    private static LootInstance CreateLootInstance(params TestPlayer[] players)
    {
        return new LootInstance(
            ownerUnitId: 99u,
            looterIds: players.ToDictionary(player => player.CharacterId, player => player.Guid),
            looterType: LooterType.Group,
            lootEntityType: LootEntityType.Creature);
    }

    private static RecordingDispatchProxy<IGameSession>.Invocation CaptureSingleSessionCall(
        RecordingDispatchProxy<IGameSession> sessionProxy,
        Action action)
    {
        int beforeCount = GetEncryptedMessageCount(sessionProxy);
        action();

        return Assert.Single(sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Skip(beforeCount));
    }

    private static IReadOnlyList<object> CaptureSessionMessages(
        RecordingDispatchProxy<IGameSession> sessionProxy,
        Action action)
    {
        int beforeCount = GetEncryptedMessageCount(sessionProxy);
        action();
        return GetEncryptedMessages(sessionProxy, beforeCount);
    }

    private static int GetEncryptedMessageCount(RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Count;
    }

    private static IReadOnlyList<object> GetEncryptedMessages(
        RecordingDispatchProxy<IGameSession> sessionProxy,
        int beforeCount)
    {
        return sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Skip(beforeCount)
            .Select(invocation => invocation.Arguments[0])
            .ToList();
    }

    private static IServiceProvider BuildProvider(IItemInfo itemInfo)
    {
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        ICharacterManager characterManager = RecordingDispatchProxy<ICharacterManager>.Create(out _);

        var lootManager = new GlobalLootManager(groupStateManager);
        var playerManager = new PlayerManager(NullLogger<PlayerManager>.Instance, characterManager);
        var itemManager = new ItemManager();
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));
        var realmContext = (RealmContext)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(RealmContext));

        SetPrivateField(itemManager, "item", ImmutableDictionary<uint, IItemInfo>.Empty.Add(StaticItemId, itemInfo));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.Item), CreateGameTable(new Item2Entry
        {
            Id = StaticItemId
        }));
        SetAutoProperty(realmContext, nameof(RealmContext.RealmId), RealmId);

        return new ServiceCollection()
            .AddSingleton(lootManager)
            .AddSingleton(playerManager)
            .AddSingleton<IPlayerManager>(playerManager)
            .AddSingleton(itemManager)
            .AddSingleton(gameTableManager)
            .AddSingleton(realmContext)
            .BuildServiceProvider();
    }

    private static IItemInfo CreateItemInfo()
    {
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            Id            = StaticItemId,
            MaxStackCount = 1u
        });
        itemInfoProxy.SetMethodReturn(nameof(IItemInfo.IsStackable), false);
        return itemInfo;
    }

    private static TestPlayer CreatePlayer(ulong characterId, uint guid, uint accountId, uint slotsRemaining)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);

        Identity identity = new()
        {
            Id      = characterId,
            RealmId = RealmId
        };

        var inventory = new TestInventory(slotsRemaining);

        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        playerProxy.SetProperty(nameof(IPlayer.Identity), identity);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty("Guid", guid);

        accountProxy.SetProperty(nameof(IAccount.Id), accountId);

        return new TestPlayer(player, sessionProxy, inventory, identity, guid);
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

    private sealed record TestPlayer(
        IPlayer Player,
        RecordingDispatchProxy<IGameSession> SessionProxy,
        TestInventory Inventory,
        Identity Identity,
        uint Guid)
    {
        public ulong CharacterId => Identity.Id;
    }

}
