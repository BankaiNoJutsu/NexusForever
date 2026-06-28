using System.Reflection;
using System.Collections.Immutable;
using System.Numerics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexusForever.Database.World.Model;
using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Entity;
using NexusForever.Game.Group;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Chat;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Group;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Static.Setting;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.GameTable.Static;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Chat;
using NexusForever.Network.World.Message.Model.Loot;
using NexusForever.Network.World.Message.Model.Story;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game;
using NetworkLootItem = NexusForever.Network.World.Message.Model.Loot.LootItem;

namespace NexusForever.Game.Tests.Loot;

public class GlobalLootManagerTests
{
    private const ushort RealmId = 1;
    private const uint StaticItemId = 8765u;

    [Fact]
    public void RemoveClientLootUnitIdOverlay_NormalisesObservedCollectId()
    {
        uint normalised = GlobalLootManager.RemoveClientLootUnitIdOverlay(0xC0000019u);

        Assert.Equal(0x80000019u, normalised);
    }

    [Fact]
    public void IsMappedFlatLootGroup_DataMappingComment_ReturnsTrue()
    {
        LootGroup group = CreateLootGroup("DataMapping creature_loot: Grimstone Digger");

        Assert.True(GlobalLootManager.IsMappedFlatLootGroup(group));
    }

    [Fact]
    public void IsMappedFlatLootGroup_LaughingWsQuestLootComment_ReturnsFalse()
    {
        LootGroup group = CreateLootGroup("LaughingWS quest loot: Item for QuestObjective 13011");

        Assert.False(GlobalLootManager.IsMappedFlatLootGroup(group));
    }

    [Fact]
    public void NextLootId_ConcurrentAllocation_ReturnsUniqueNonZeroIds()
    {
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        var manager = new GlobalLootManager(groupStateManager);
        uint[] ids = new uint[2048];

        Parallel.For(0, ids.Length, i => ids[i] = manager.NextLootId);

        Assert.Equal(ids.Length, ids.Distinct().Count());
        Assert.DoesNotContain(0u, ids);
        Assert.Contains(0x80000000u, ids);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DropLoot_WithUnavailableCreatureTableReturnsFalseWithoutLoot(bool includeEmptyCreatureTable)
    {
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        var gameTableManager = (GameTableManager)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        if (includeEmptyCreatureTable)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.Creature2), CreateGameTable<Creature2Entry>());
        var manager = new GlobalLootManager(groupStateManager, gameTableManager: gameTableManager);

        IPlayer looter = CreateLootBoundaryPlayer(characterId: 42ul, guid: 4242u, out var sessionProxy);
        IWorldEntity lootedEntity = RecordingDispatchProxy<IWorldEntity>.Create(out var lootedEntityProxy);
        lootedEntityProxy.SetProperty(nameof(IGridEntity.Guid), 9090u);
        lootedEntityProxy.SetProperty(nameof(IWorldEntity.CreatureId), 1234u);

        bool dropped = manager.DropLoot(looter, lootedEntity);

        Assert.False(dropped);
        Assert.Equal(0, GetLootInstanceCount(manager));
        Assert.Equal(0, GetOwnerIndexCount(manager, lootedEntity.Guid));
        Assert.Equal(0, GetLooterIndexCount(manager, looter.CharacterId));
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void LootRequestBoundaries_NonLooterReturnsWithoutThrowing()
    {
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        var manager = new GlobalLootManager(groupStateManager);

        try
        {
            IPlayer trackedLooter = CreateLootBoundaryPlayer(characterId: 42ul, guid: 4242u, out _);
            IPlayer outsider = CreateLootBoundaryPlayer(characterId: 99ul, guid: 9999u, out var outsiderSessionProxy);
            var lootInstance = new LootInstance(
                ownerUnitId: outsider.Guid,
                looterIds: new Dictionary<ulong, uint>
                {
                    [trackedLooter.CharacterId] = trackedLooter.Guid
                },
                looterType: LooterType.Group,
                lootEntityType: LootEntityType.Creature);
            LootInstanceItem lootItem = lootInstance.AddLootItem((uint)CurrencyType.Credits, LootItemType.Cash, 1u);
            AddLootInstance(manager, lootInstance);

            Exception exception = Record.Exception(() =>
            {
                manager.GiveLoot(outsider, outsider.Guid, lootItem.Id);
                manager.RollLoot(outsider, outsider.Guid, lootItem.Id, LootRollAction.Need);
                manager.AssignMasterLoot(outsider, outsider.Guid, lootItem.Id, trackedLooter.Identity);
            });

            Assert.Null(exception);
            Assert.False(lootItem.Delivered);
            Assert.Empty(outsiderSessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        }
        finally
        {
        }
    }

    [Fact]
    public void TryDeliverHarvestLoot_RoundRobinIgnoresOutOfRangeSameMapMember()
    {
        const ulong groupId = 7001ul;

        var groupStateManager = new GroupStateManager();
        PlayerManager playerManager = CreatePlayerManager();
        var manager = new GlobalLootManager(groupStateManager, playerManager);

        try
        {
            IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out _);
            HarvestPlayer harvester = CreateHarvestPlayer(
                characterId: 100ul,
                guid: 1000u,
                accountId: 1100u,
                groupId: groupId,
                map: map,
                position: Vector3.Zero,
                slotsRemaining: 0u);
            HarvestPlayer outOfRange = CreateHarvestPlayer(
                characterId: 200ul,
                guid: 2000u,
                accountId: 1200u,
                groupId: groupId,
                map: map,
                position: new Vector3(100f, 0f, 0f),
                slotsRemaining: 0u);

            playerManager.AddPlayer(harvester.Player);
            playerManager.AddPlayer(outOfRange.Player);
            groupStateManager.UpdateGroup(CreateHarvestGroup(
                groupId,
                HarvestLootRule.RoundRobin,
                (outOfRange.Identity, 0u),
                (harvester.Identity, 1u)));

            bool delivered = manager.TryDeliverHarvestLoot(
                harvester.Player,
                [new GeneratedLootItem(LootItemType.Cash, (uint)CurrencyType.Credits, 25u)],
                ownerUnitId: 3456u);

            Assert.True(delivered);
            RecordingDispatchProxy<ICurrencyManager>.Invocation currencyCall =
                Assert.Single(harvester.CurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
            Assert.Equal(CurrencyType.Credits, currencyCall.Arguments[0]);
            Assert.Equal(25ul, currencyCall.Arguments[1]);
            Assert.Empty(outOfRange.CurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
        }
        finally
        {
            RemovePlayers(playerManager, groupId);
        }
    }

    [Fact]
    public void CreateLootRecipientContext_GroupCorpseLootKeepsOnlyInRangeMembersEligible()
    {
        const ulong groupId = 7101ul;

        var groupStateManager = new GroupStateManager();
        PlayerManager playerManager = CreatePlayerManager();
        var manager = new GlobalLootManager(groupStateManager, playerManager);

        try
        {
            IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out _);
            HarvestPlayer looter = CreateHarvestPlayer(
                characterId: 110ul,
                guid: 1100u,
                accountId: 3100u,
                groupId: groupId,
                map: map,
                position: Vector3.Zero,
                slotsRemaining: 0u);
            HarvestPlayer outOfRange = CreateHarvestPlayer(
                characterId: 210ul,
                guid: 2100u,
                accountId: 3200u,
                groupId: groupId,
                map: map,
                position: new Vector3(100f, 0f, 0f),
                slotsRemaining: 0u);
            IWorldEntity corpse = CreateLootOwner(map, guid: 9010u, position: Vector3.Zero);

            playerManager.AddPlayer(looter.Player);
            playerManager.AddPlayer(outOfRange.Player);
            groupStateManager.UpdateGroup(CreateHarvestGroup(
                groupId,
                HarvestLootRule.FirstTagger,
                (outOfRange.Identity, 0u),
                (looter.Identity, 1u)));

            object context = CreateLootRecipientContext(manager, looter.Player, corpse);
            IReadOnlyList<IPlayer> eligiblePlayers = GetContextProperty<IReadOnlyList<IPlayer>>(context, "EligiblePlayers");
            Dictionary<ulong, uint> looterIds = GetContextProperty<Dictionary<ulong, uint>>(context, "LooterIds");
            IReadOnlyList<IPlayer> masterLootCandidates = GetContextProperty<IReadOnlyList<IPlayer>>(context, "MasterLootCandidates");

            Assert.Equal(LooterType.Group, GetContextProperty<LooterType>(context, "LooterType"));
            IPlayer eligiblePlayer = Assert.Single(eligiblePlayers);
            Assert.Equal(looter.Player, eligiblePlayer);
            Assert.Single(looterIds);
            Assert.True(looterIds.ContainsKey(looter.Player.CharacterId));
            Assert.DoesNotContain(outOfRange.Player.CharacterId, looterIds.Keys);
            IPlayer masterLootCandidate = Assert.Single(masterLootCandidates);
            Assert.Equal(looter.Player, masterLootCandidate);
        }
        finally
        {
            RemovePlayers(playerManager, groupId);
        }
    }

    [Fact]
    public void TryDeliverHarvestLoot_SkipsInventoryFullRecipientBeforeRoundRobinAdvance()
    {
        const ulong groupId = 7002ul;

        var groupStateManager = new GroupStateManager();
        PlayerManager playerManager = CreatePlayerManager();
        IItemInfo itemInfo = CreateHarvestItemInfo();
        ItemManager itemManager = CreateItemManager(itemInfo);
        var manager = new GlobalLootManager(groupStateManager, playerManager, itemManager);

        try
        {
            IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out _);
            HarvestPlayer harvester = CreateHarvestPlayer(
                characterId: 101ul,
                guid: 1010u,
                accountId: 2100u,
                groupId: groupId,
                map: map,
                position: Vector3.Zero,
                slotsRemaining: 1u);
            HarvestPlayer fullRecipient = CreateHarvestPlayer(
                characterId: 201ul,
                guid: 2010u,
                accountId: 2200u,
                groupId: groupId,
                map: map,
                position: new Vector3(5f, 0f, 0f),
                slotsRemaining: 0u);

            playerManager.AddPlayer(harvester.Player);
            playerManager.AddPlayer(fullRecipient.Player);
            groupStateManager.UpdateGroup(CreateHarvestGroup(
                groupId,
                HarvestLootRule.RoundRobin,
                (fullRecipient.Identity, 0u),
                (harvester.Identity, 1u)));

            bool firstDelivered = manager.TryDeliverHarvestLoot(
                harvester.Player,
                [new GeneratedLootItem(LootItemType.StaticItem, StaticItemId, 1u)],
                ownerUnitId: 4567u);

            Assert.True(firstDelivered);
            Assert.Single(harvester.Inventory.CreatedItems);
            Assert.Empty(fullRecipient.Inventory.CreatedItems);

            fullRecipient.Inventory.SlotsRemaining = 1u;
            bool secondDelivered = manager.TryDeliverHarvestLoot(
                harvester.Player,
                [new GeneratedLootItem(LootItemType.StaticItem, StaticItemId, 1u)],
                ownerUnitId: 4568u);

            Assert.True(secondDelivered);
            Assert.Single(harvester.Inventory.CreatedItems);
            Assert.Single(fullRecipient.Inventory.CreatedItems);
        }
        finally
        {
            RemovePlayers(playerManager, groupId);
        }
    }

    [Fact]
    public void GiveAllLootInRange_PendingRollOnlyRefreshesLootNotify()
    {
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        var manager = new GlobalLootManager(groupStateManager);

        try
        {
            IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
            TestPlayerBuilder playerBuilder = TestPlayerBuilder.Create()
                .WithSession(session)
                .WithCharacterId(42ul)
                .WithGuid(4242u);
            playerBuilder.PlayerProxy.SetProperty(nameof(IPlayer.Position), Vector3.Zero);
            IPlayer player = playerBuilder.Build();

            var lootInstance = new LootInstance(
                ownerUnitId: player.Guid,
                looterIds: new Dictionary<ulong, uint> { [player.CharacterId] = player.Guid },
                looterType: LooterType.Player,
                lootEntityType: LootEntityType.Creature);
            LootInstanceItem lootItem = lootInstance.AddLootItem((uint)CurrencyType.Credits, LootItemType.Cash, 12u);
            lootItem.ConfigureRoll([new Identity { Id = player.CharacterId, RealmId = RealmId }]);
            AddLootInstance(manager, lootInstance);

            manager.GiveAllLootInRange(player);

            Assert.False(lootItem.Delivered);
            RecordingDispatchProxy<IGameSession>.Invocation sessionCall =
                Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
            var notify = Assert.IsType<ServerLootNotify>(sessionCall.Arguments[0]);
            NetworkLootItem networkItem = Assert.Single(notify.LootItems);
            Assert.Equal(lootItem.Id, networkItem.LootUnitId);
            Assert.True(networkItem.RequiresRoll);
            Assert.False(networkItem.CanLoot);
        }
        finally
        {
        }
    }

    [Fact]
    public void GiveLoot_WhenLastItemDelivered_RemovesInstanceImmediately()
    {
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        var manager = new GlobalLootManager(groupStateManager);

        try
        {
            IPlayer player = CreatePlayer(out _, out var characterCurrencyProxy, out _, out _);
            var lootInstance = new LootInstance(
                ownerUnitId: player.Guid,
                looterIds: new Dictionary<ulong, uint> { [player.CharacterId] = player.Guid },
                looterType: LooterType.Player,
                lootEntityType: LootEntityType.Creature);
            LootInstanceItem lootItem = lootInstance.AddLootItem((uint)CurrencyType.Credits, LootItemType.Cash, 12u);
            AddLootInstance(manager, lootInstance);

            manager.GiveLoot(player, player.Guid, lootItem.Id);

            Assert.True(lootItem.Delivered);
            Assert.Equal(0, GetLootInstanceCount(manager));
            Assert.Equal(0, GetOwnerIndexCount(manager, player.Guid));
            Assert.Equal(0, GetLooterIndexCount(manager, player.CharacterId));
            RecordingDispatchProxy<ICurrencyManager>.Invocation currencyCall =
                Assert.Single(characterCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
            Assert.Equal(CurrencyType.Credits, currencyCall.Arguments[0]);
            Assert.Equal(12ul, currencyCall.Arguments[1]);
        }
        finally
        {
        }
    }

    [Fact]
    public void RemoveLootForOwner_RemovesOwnerInstancesAndSendsRemoveToTrackedLooters()
    {
        const uint ownerUnitId = 9090u;
        const ulong groupId = 7201ul;

        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        PlayerManager playerManager = CreatePlayerManager();
        var manager = new GlobalLootManager(groupStateManager, playerManager);

        try
        {
            HarvestPlayer looter = CreateHarvestPlayer(
                characterId: 120ul,
                guid: 1200u,
                accountId: 4200u,
                groupId: groupId,
                map: null,
                position: Vector3.Zero,
                slotsRemaining: 0u);
            playerManager.AddPlayer(looter.Player);

            var lootInstance = new LootInstance(
                ownerUnitId: ownerUnitId,
                looterIds: new Dictionary<ulong, uint> { [looter.Player.CharacterId] = looter.Player.Guid },
                looterType: LooterType.Player,
                lootEntityType: LootEntityType.Creature,
                playerManager: playerManager);
            lootInstance.AddLootItem((uint)CurrencyType.Credits, LootItemType.Cash, 12u);
            AddLootInstance(manager, lootInstance);

            manager.RemoveLootForOwner(ownerUnitId);

            Assert.Equal(0, GetLootInstanceCount(manager));
            Assert.Equal(0, GetOwnerIndexCount(manager, ownerUnitId));
            Assert.Equal(0, GetLooterIndexCount(manager, looter.Player.CharacterId));
            RecordingDispatchProxy<IGameSession>.Invocation sessionCall =
                Assert.Single(looter.SessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
            var remove = Assert.IsType<ServerLootRemove>(sessionCall.Arguments[0]);
            Assert.Equal(ownerUnitId, remove.OwnerUnitId);
        }
        finally
        {
            RemovePlayers(playerManager, groupId);
        }
    }

    [Fact]
    public void SendLootNotify_DuplicateRequestAfterInitialNotify_DoesNotReplayFullNotify()
    {
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        var manager = new GlobalLootManager(groupStateManager);

        try
        {
            IPlayer player = CreatePlayer(out _, out _, out _, out var sessionProxy);
            var lootInstance = new LootInstance(
                ownerUnitId: player.Guid,
                looterIds: new Dictionary<ulong, uint> { [player.CharacterId] = player.Guid },
                looterType: LooterType.Player,
                lootEntityType: LootEntityType.Creature);
            lootInstance.AddLootItem((uint)CurrencyType.Credits, LootItemType.Cash, 12u);
            AddLootInstance(manager, lootInstance);

            lootInstance.SendLootNotify(player);
            manager.SendLootNotify(player, player.Guid);
            manager.SendLootNotify(player, player.Guid);

            RecordingDispatchProxy<IGameSession>.Invocation sessionCall =
                Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
            var notify = Assert.IsType<ServerLootNotify>(sessionCall.Arguments[0]);
            Assert.Equal(player.Guid, notify.OwnerUnitId);
        }
        finally
        {
        }
    }

    [Fact]
    public void SendLootNotify_WhenNoMatchingActiveLoot_SendsRemoveForRequestedOwner()
    {
        const uint ownerUnitId = 9191u;

        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        var manager = new GlobalLootManager(groupStateManager);
        IPlayer player = CreatePlayer(out _, out _, out _, out var sessionProxy);

        manager.SendLootNotify(player, ownerUnitId);

        RecordingDispatchProxy<IGameSession>.Invocation sessionCall =
            Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        var remove = Assert.IsType<ServerLootRemove>(sessionCall.Arguments[0]);
        Assert.Equal(ownerUnitId, remove.OwnerUnitId);
    }

    [Fact]
    public void SendLootNotifyForVisibleOwner_ForcesNotifyAfterInitialNotify()
    {
        const uint ownerUnitId = 9292u;

        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        var manager = new GlobalLootManager(groupStateManager);

        try
        {
            IPlayer player = CreatePlayer(out _, out _, out _, out var sessionProxy);
            IWorldEntity owner = RecordingDispatchProxy<IWorldEntity>.Create(out var ownerProxy);
            ownerProxy.SetProperty(nameof(IWorldEntity.Guid), ownerUnitId);
            var lootInstance = new LootInstance(
                ownerUnitId: ownerUnitId,
                looterIds: new Dictionary<ulong, uint> { [player.CharacterId] = player.Guid },
                looterType: LooterType.Player,
                lootEntityType: LootEntityType.Creature);
            lootInstance.AddLootItem((uint)CurrencyType.Credits, LootItemType.Cash, 12u);
            AddLootInstance(manager, lootInstance);

            lootInstance.SendLootNotify(player);
            manager.SendLootNotifyForVisibleOwner(player, owner);

            IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> sessionCalls =
                sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
            Assert.Equal(2, sessionCalls.Count);
            Assert.All(sessionCalls, sessionCall =>
            {
                var notify = Assert.IsType<ServerLootNotify>(sessionCall.Arguments[0]);
                Assert.Equal(ownerUnitId, notify.OwnerUnitId);
            });
        }
        finally
        {
        }
    }

    [Fact]
    public void GiveRandomOmnibitKillReward_GrantsCurrencyWithoutExplosionNotify()
    {
        const uint ownerUnitId = 9393u;

        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        var manager = new GlobalLootManager(groupStateManager);

        try
        {
            IPlayer player = CreatePlayer(out var accountCurrencyProxy, out var achievementProxy, out var sessionProxy);

            GlobalLootManager.GiveRandomOmnibitKillReward(player, 29u, ownerUnitId);

            RecordingDispatchProxy<IAccountCurrencyManager>.Invocation currencyCall =
                Assert.Single(accountCurrencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencyAddAmount)));
            Assert.Equal(AccountCurrencyType.Omnibit, currencyCall.Arguments[0]);
            Assert.Equal(29ul, currencyCall.Arguments[1]);

            Assert.Single(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));

            IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> sessionCalls =
                sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
            RecordingDispatchProxy<IGameSession>.Invocation sessionCall = Assert.Single(sessionCalls);
            var grant = Assert.IsType<ServerLootGrant>(sessionCall.Arguments[0]);
            Assert.Equal(ownerUnitId, grant.OwnerUnitId);
            Assert.Equal(LootItemType.AccountCurrency, grant.LootItem.Type);
            Assert.Equal((uint)AccountCurrencyType.Omnibit, grant.LootItem.ItemId);
            Assert.Equal(29u, grant.LootItem.Amount);
            Assert.False(grant.LootItem.Explosion);
            Assert.False(grant.LootItem.Granted);
        }
        finally
        {
        }
    }

    [Fact]
    public void GiveGeneratedLoot_AccountCurrencyWithGrantedNotify_SendsGrantedExplosionNotifyThenRemoveFromParentSource()
    {
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        var manager = new GlobalLootManager(groupStateManager);
        IPlayer player = CreatePlayer(out var currencyProxy, out var achievementProxy, out var sessionProxy);

        try
        {
            manager.GiveGeneratedLoot(
                player,
                [new GeneratedLootItem(LootItemType.AccountCurrency, (uint)AccountCurrencyType.Omnibit, 75u)],
                ownerUnitId: 99u,
                sendGrantedNotify: true,
                parentUnitId: 123u);
        }
        finally
        {
        }

        RecordingDispatchProxy<IAccountCurrencyManager>.Invocation currencyCall = Assert.Single(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencyAddAmount)));
        Assert.Equal(AccountCurrencyType.Omnibit, currencyCall.Arguments[0]);
        Assert.Equal(75ul, currencyCall.Arguments[1]);

        Assert.Single(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));

        var sessionCalls = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        Assert.Equal(2, sessionCalls.Count);
        var notify = Assert.IsType<ServerLootNotify>(sessionCalls[0].Arguments[0]);
        Assert.Equal(99u, notify.OwnerUnitId);
        Assert.Equal(123u, notify.ParentUnitId);
        Assert.True(notify.Explosion);

        var lootItem = Assert.Single(notify.LootItems);
        Assert.Equal(0u, lootItem.LootUnitId);
        Assert.Equal(LootItemType.AccountCurrency, lootItem.Type);
        Assert.Equal((uint)AccountCurrencyType.Omnibit, lootItem.ItemId);
        Assert.Equal(75u, lootItem.Amount);
        Assert.True(lootItem.CanLoot);
        Assert.True(lootItem.Granted);
        Assert.True(lootItem.Explosion);

        var remove = Assert.IsType<ServerLootRemove>(sessionCalls[1].Arguments[0]);
        Assert.Equal(99u, remove.OwnerUnitId);
    }

    [Fact]
    public void GiveGeneratedLoot_MultipleItemsWithGrantedNotify_SendsExplosionNotifyThenRemoveWithActualRows()
    {
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        var manager = new GlobalLootManager(groupStateManager);
        IPlayer player = CreatePlayer(
            out var accountCurrencyProxy,
            out var characterCurrencyProxy,
            out var achievementProxy,
            out var sessionProxy);

        try
        {
            manager.GiveGeneratedLoot(
                player,
                [
                    new GeneratedLootItem(LootItemType.AccountCurrency, (uint)AccountCurrencyType.Omnibit, 75u),
                    new GeneratedLootItem(LootItemType.Cash, (uint)CurrencyType.Credits, 17u)
                ],
                ownerUnitId: 99u,
                sendGrantedNotify: true,
                parentUnitId: 123u);
        }
        finally
        {
        }

        RecordingDispatchProxy<IAccountCurrencyManager>.Invocation accountCurrencyCall = Assert.Single(accountCurrencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencyAddAmount)));
        Assert.Equal(AccountCurrencyType.Omnibit, accountCurrencyCall.Arguments[0]);
        Assert.Equal(75ul, accountCurrencyCall.Arguments[1]);

        RecordingDispatchProxy<ICurrencyManager>.Invocation characterCurrencyCall = Assert.Single(characterCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
        Assert.Equal(CurrencyType.Credits, characterCurrencyCall.Arguments[0]);
        Assert.Equal(17ul, characterCurrencyCall.Arguments[1]);
        Assert.True((bool)characterCurrencyCall.Arguments[2]);

        Assert.Single(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));

        var sessionCalls = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        Assert.Equal(2, sessionCalls.Count);
        var notify = Assert.IsType<ServerLootNotify>(sessionCalls[0].Arguments[0]);
        Assert.Equal(99u, notify.OwnerUnitId);
        Assert.Equal(123u, notify.ParentUnitId);
        Assert.True(notify.Explosion);
        Assert.Equal(2, notify.LootItems.Count);

        NetworkLootItem accountLootItem = Assert.Single(notify.LootItems, item => item.Type == LootItemType.AccountCurrency);
        Assert.Equal(0u, accountLootItem.LootUnitId);
        Assert.Equal((uint)AccountCurrencyType.Omnibit, accountLootItem.ItemId);
        Assert.Equal(75u, accountLootItem.Amount);
        Assert.True(accountLootItem.CanLoot);
        Assert.True(accountLootItem.Granted);
        Assert.True(accountLootItem.Explosion);

        NetworkLootItem cashLootItem = Assert.Single(notify.LootItems, item => item.Type == LootItemType.Cash);
        Assert.Equal(0u, cashLootItem.LootUnitId);
        Assert.Equal((uint)CurrencyType.Credits, cashLootItem.ItemId);
        Assert.Equal(17u, cashLootItem.Amount);
        Assert.True(cashLootItem.CanLoot);
        Assert.True(cashLootItem.Granted);
        Assert.True(cashLootItem.Explosion);

        var remove = Assert.IsType<ServerLootRemove>(sessionCalls[1].Arguments[0]);
        Assert.Equal(99u, remove.OwnerUnitId);
    }

    [Theory]
    [InlineData(LootItemType.AccountCurrency, 9u)]
    [InlineData(LootItemType.AccountItem, 77u)]
    [InlineData(LootItemType.VirtualItem, 88u)]
    public void CanDeliverGeneratedLoot_WithMissingRewardTable_ReturnsInvalidLootItem(LootItemType type, uint staticId)
    {
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        IPlayer player = CreatePlayer(out _, out _, out _);
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));
        var manager = new GlobalLootManager(groupStateManager, gameTableManager: gameTableManager);

        bool result = manager.CanDeliverGeneratedLoot(
            player,
            [new GeneratedLootItem(type, staticId, 1u)],
            out string reason);

        Assert.False(result);
        Assert.Equal($"invalid-loot-item:{type}:{staticId}", reason);
    }

    [Fact]
    public void CanDeliverGeneratedLoot_AccountCurrencyUsesAccountCurrencyTypeTableForValidIds()
    {
        const uint tableBackedCurrencyId = 20u;
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        IPlayer player = CreatePlayer(out _, out _, out _);
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.AccountCurrencyType), CreateGameTable(new AccountCurrencyTypeEntry
        {
            Id = tableBackedCurrencyId
        }));
        var manager = new GlobalLootManager(groupStateManager, gameTableManager: gameTableManager);

        bool result = manager.CanDeliverGeneratedLoot(
            player,
            [new GeneratedLootItem(LootItemType.AccountCurrency, tableBackedCurrencyId, 1u)],
            out string reason);

        Assert.True(result);
        Assert.Equal(string.Empty, reason);
    }

    private static IPlayer CreatePlayer(
        out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
        out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        return CreatePlayer(
            out currencyProxy,
            out _,
            out achievementProxy,
            out sessionProxy);
    }

    private static LootGroup CreateLootGroup(string comment)
    {
        return new LootGroup(new LootGroupModel
        {
            Comment = comment,
            Item = []
        }, loadChildren: false);
    }

    private static IPlayer CreatePlayer(
        out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy,
        out RecordingDispatchProxy<ICurrencyManager> characterCurrencyProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
        out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        IAccountCurrencyManager currencyManager = RecordingDispatchProxy<IAccountCurrencyManager>.Create(out currencyProxy);
        ICurrencyManager characterCurrencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out characterCurrencyProxy);
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out achievementProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);

        accountProxy.SetProperty(nameof(IAccount.CurrencyManager), currencyManager);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), characterCurrencyManager);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty("Guid", 4242u);

        return player;
    }

    private static IPlayer CreateLootBoundaryPlayer(
        ulong characterId,
        uint guid,
        out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);

        playerProxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        playerProxy.SetProperty(nameof(IPlayer.Identity), new Identity { Id = characterId, RealmId = 1 });
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty("Guid", guid);

        return player;
    }

    private static ItemManager CreateItemManager(IItemInfo itemInfo)
    {
        var itemManager = new ItemManager();
        SetPrivateField(itemManager, "item", ImmutableDictionary<uint, IItemInfo>.Empty.Add(StaticItemId, itemInfo));
        return itemManager;
    }

    private static GroupLootState CreateHarvestGroup(
        ulong groupId,
        HarvestLootRule harvestRule,
        params (Identity Identity, uint GroupIndex)[] members)
    {
        return new GroupLootState
        {
            GroupId          = groupId,
            NormalRule       = LootRule.FreeForAll,
            ThresholdRule    = LootRule.FreeForAll,
            ThresholdQuality = LootThreshold.Good,
            HarvestRule      = harvestRule,
            Leader           = members[0].Identity,
            Members          = members
                .Select(member => new GroupLootMember
                {
                    Identity   = member.Identity,
                    GroupIndex = member.GroupIndex
                })
                .ToList()
        };
    }

    private static HarvestPlayer CreateHarvestPlayer(
        ulong characterId,
        uint guid,
        uint accountId,
        ulong groupId,
        IBaseMap map,
        Vector3 position,
        uint slotsRemaining)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out var currencyProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
        var inventory = new HarvestInventoryHarness(slotsRemaining);
        Identity identity = new()
        {
            Id      = characterId,
            RealmId = RealmId
        };

        accountProxy.SetProperty(nameof(IAccount.Id), accountId);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        playerProxy.SetProperty(nameof(IPlayer.Identity), identity);
        playerProxy.SetProperty(nameof(IPlayer.GroupAssociation), groupId);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetProperty(nameof(IPlayer.Position), position);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory.Inventory);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty("Guid", guid);

        return new HarvestPlayer(player, identity, currencyProxy, sessionProxy, inventory);
    }

    private static IItemInfo CreateHarvestItemInfo()
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

    private static IWorldEntity CreateLootOwner(IBaseMap map, uint guid, Vector3 position)
    {
        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out var entityProxy);
        entityProxy.SetProperty(nameof(IGridEntity.Map), map);
        entityProxy.SetProperty(nameof(IGridEntity.Position), position);
        entityProxy.SetProperty(nameof(IGridEntity.Guid), guid);
        return entity;
    }

    private static object CreateLootRecipientContext(GlobalLootManager manager, IPlayer looter, IWorldEntity lootedEntity)
    {
        return typeof(GlobalLootManager)
            .GetMethod("CreateLootRecipientContext", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(manager, [looter, lootedEntity])!;
    }

    private static T GetContextProperty<T>(object context, string propertyName)
    {
        return (T)context.GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .GetValue(context)!;
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

    private static PlayerManager CreatePlayerManager()
    {
        ICharacterManager characterManager = RecordingDispatchProxy<ICharacterManager>.Create(out _);
        return new PlayerManager(NullLogger<PlayerManager>.Instance, characterManager);
    }

    private static void RemovePlayers(IPlayerManager playerManager, ulong groupId)
    {
        foreach (IPlayer player in playerManager.Where(p => p.GroupAssociation == groupId).ToList())
            playerManager.RemovePlayer(player);
    }

    private static void AddLootInstance(GlobalLootManager manager, LootInstance lootInstance)
    {
        typeof(GlobalLootManager)
            .GetMethod("AddLootInstance", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(manager, [lootInstance]);
    }

    private static int GetLootInstanceCount(GlobalLootManager manager)
    {
        var instances = (List<LootInstance>)typeof(GlobalLootManager)
            .GetField("lootInstances", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(manager)!;
        return instances.Count;
    }

    private static int GetOwnerIndexCount(GlobalLootManager manager, uint ownerUnitId)
    {
        var instancesByOwnerUnit = (Dictionary<uint, List<LootInstance>>)typeof(GlobalLootManager)
            .GetField("lootInstancesByOwnerUnit", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(manager)!;
        return instancesByOwnerUnit.TryGetValue(ownerUnitId, out List<LootInstance> instances)
            ? instances.Count
            : 0;
    }

    private static int GetLooterIndexCount(GlobalLootManager manager, ulong characterId)
    {
        var instancesByLooter = (Dictionary<ulong, List<LootInstance>>)typeof(GlobalLootManager)
            .GetField("lootInstancesByLooter", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(manager)!;
        return instancesByLooter.TryGetValue(characterId, out List<LootInstance> instances)
            ? instances.Count
            : 0;
    }

    private sealed record HarvestPlayer(
        IPlayer Player,
        Identity Identity,
        RecordingDispatchProxy<ICurrencyManager> CurrencyProxy,
        RecordingDispatchProxy<IGameSession> SessionProxy,
        HarvestInventoryHarness Inventory);

    private sealed class HarvestInventoryHarness
    {
        public sealed record ItemCreateCall(InventoryLocation Location, uint ItemId, uint Count, ItemUpdateReason Reason);

        public IInventory Inventory { get; }
        public List<ItemCreateCall> CreatedItems { get; } = [];
        public uint SlotsRemaining { get; set; }

        public HarvestInventoryHarness(uint slotsRemaining)
        {
            SlotsRemaining = slotsRemaining;

            IBag bag = RecordingDispatchProxy<IBag>.Create(out var bagProxy);
            bagProxy.SetProperty(nameof(IBag.Location), InventoryLocation.Inventory);
            bagProxy.SetMethodReturnFactory("get_SlotsRemaining", () => SlotsRemaining);
            bagProxy.SetMethodReturnFactory("GetEnumerator", () => Enumerable.Empty<IItem>().GetEnumerator());

            Inventory = RecordingDispatchProxy<IInventory>.Create(out var inventoryProxy);
            inventoryProxy.SetMethodReturnFactory("GetEnumerator", () => new[] { bag }.AsEnumerable().GetEnumerator());
            inventoryProxy.SetMethodHandler(nameof(IInventory.ItemCreate), args =>
            {
                uint itemId = args[1] switch
                {
                    uint id         => id,
                    IItemInfo info  => info.Entry.Id,
                    _               => 0u
                };
                uint count = (uint)args[2];
                var reason = args.Length > 3 && args[3] is ItemUpdateReason itemUpdateReason
                    ? itemUpdateReason
                    : ItemUpdateReason.NoReason;
                CreatedItems.Add(new ItemCreateCall((InventoryLocation)args[0], itemId, count, reason));
                if (SlotsRemaining > 0u)
                    SlotsRemaining--;
                return null;
            });
        }
    }
}
