using System;
using System.Collections.Generic;
using System.Numerics;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Costume;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Account.Unlock;
using NexusForever.Game.Abstract.Challenges;
using NexusForever.GameTable.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Static.Housing;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Account;
using NexusForever.GameTable;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Entity.Movement.Command.Mode;
using NexusForever.Game.Static.Entity.Movement.Command.State;
using NexusForever.Game.Static.Group;
using NexusForever.Game.Static.PlayerPath;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Static;

namespace NexusForever.Game.Tests.Entity;

public class PrerequisiteCheckTests
{
    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true, true)]
    [InlineData(PrerequisiteComparison.Equal, false, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true, false)]
    [InlineData(PrerequisiteComparison.NotEqual, false, true)]
    public void UnderSpell_UsesActiveTrackedSpellState(PrerequisiteComparison comparison, bool active, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetMethodReturn(nameof(IPlayer.HasTrackedSpellState), active);

        var check = new PrerequisiteCheckUnderSpell(NullLogger<PrerequisiteCheckUnderSpell>.Instance);

        bool result = check.Meets(player, comparison, value: 85563u, objectId: 0u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 517u, true)]
    [InlineData(PrerequisiteComparison.Equal, 518u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 517u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 518u, true)]
    public void InSubZone_ComparesAgainstCurrentZone(PrerequisiteComparison comparison, uint requiredZoneId, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Zone), new WorldZoneEntry { Id = 517u });

        var check = new PrerequisiteCheckInSubZone(NullLogger<PrerequisiteCheckInSubZone>.Instance, gameTableManager: null);

        bool result = check.Meets(player, comparison, value: requiredZoneId, objectId: 0u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 3460u, true)]
    [InlineData(PrerequisiteComparison.Equal, 1537u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 3460u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 1537u, true)]
    public void WorldRequirement_ComparesAgainstCurrentMapWorldId(PrerequisiteComparison comparison, uint requiredWorldId, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = 3460u });
        playerProxy.SetProperty(nameof(IPlayer.Map), map);

        var check = new PrerequisiteCheckWorldRequirement();

        bool result = check.Meets(player, comparison, value: 0u, objectId: requiredWorldId, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 2u, true)]
    [InlineData(PrerequisiteComparison.Equal, 1u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 2u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 1u, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 2u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 2u, false)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 2u, true)]
    [InlineData(PrerequisiteComparison.LessThan, 2u, false)]
    public void ChallengeRequirement_ComparesChallengeCompletionCount(PrerequisiteComparison comparison, uint requiredCount, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IChallengeManager challengeManager = RecordingDispatchProxy<IChallengeManager>.Create(out var challengeProxy);
        challengeProxy.SetMethodReturn(nameof(IChallengeManager.GetCompletionCount), 2u);
        playerProxy.SetProperty(nameof(IPlayer.ChallengeManager), challengeManager);

        var check = new PrerequisiteCheckChallengeRequirement(NullLogger<PrerequisiteCheckChallengeRequirement>.Instance);

        bool result = check.Meets(player, comparison, requiredCount, objectId: 971u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, SettlerInfrastructureState.Built, (uint)SettlerInfrastructureState.Built, true)]
    [InlineData(PrerequisiteComparison.Equal, SettlerInfrastructureState.Inactive, (uint)SettlerInfrastructureState.Built, false)]
    [InlineData(PrerequisiteComparison.NotEqual, SettlerInfrastructureState.Building, (uint)SettlerInfrastructureState.Built, true)]
    [InlineData(PrerequisiteComparison.NotEqual, SettlerInfrastructureState.Built, (uint)SettlerInfrastructureState.Built, false)]
    public void InfrastructureState_ComparesPathManagerState(
        PrerequisiteComparison comparison,
        SettlerInfrastructureState currentState,
        uint requiredState,
        bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IPathManager pathManager = RecordingDispatchProxy<IPathManager>.Create(out var pathProxy);
        pathProxy.SetMethodReturn(nameof(IPathManager.GetSettlerInfrastructureState), currentState);
        playerProxy.SetProperty(nameof(IPlayer.PathManager), pathManager);

        var check = new PrerequisiteCheckInfrastructureState(NullLogger<PrerequisiteCheckInfrastructureState>.Instance);

        bool result = check.Meets(player, comparison, requiredState, objectId: 2u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true, true)]
    [InlineData(PrerequisiteComparison.Equal, false, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true, false)]
    [InlineData(PrerequisiteComparison.NotEqual, false, true)]
    public void PetEntitySpell4_UsesActiveVanityPetPresence(PrerequisiteComparison comparison, bool hasVanityPet, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        uint? vanityGuid = hasVanityPet ? 9001u : null;
        playerProxy.SetProperty(nameof(IPlayer.VanityPetGuid), vanityGuid);

        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out var spellManagerProxy);
        spellManagerProxy.SetMethodReturn(
            nameof(ISpellManager.GetPets),
            hasVanityPet
                ? new List<ICharacterSpell> { RecordingDispatchProxy<ICharacterSpell>.Create(out _) }
                : new List<ICharacterSpell>());
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);

        if (hasVanityPet)
        {
            IPetEntity pet = RecordingDispatchProxy<IPetEntity>.Create(out _);
            playerProxy.SetMethodReturn(nameof(IPlayer.GetVisible), pet);
        }
        else
        {
            playerProxy.SetMethodReturn(nameof(IPlayer.GetVisible), null);
        }

        var check = new PrerequisiteCheckPetEntitySpell4(NullLogger<PrerequisiteCheckPetEntitySpell4>.Instance);

        bool result = check.Meets(player, comparison, value: 0u, objectId: 0u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ItemRolledPropertyValue_WithoutItemContext_ReturnsFalse()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        var check = new PrerequisiteCheckItemRolledPropertyValue();

        bool result = check.Meets(
            player,
            PrerequisiteComparison.Equal,
            value: 0u,
            objectId: (uint)Property.ShieldCapacityMax,
            new PrerequisiteParameters());

        Assert.False(result);
    }

    [Fact]
    public void ItemRolledPropertyValue_MatchesTemplatePropertyMagnitudeBits()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        float magnitude = 42f;
        itemInfoProxy.SetProperty(
            nameof(IItemInfo.Properties),
            ImmutableDictionary<Property, float>.Empty.Add(Property.ShieldCapacityMax, magnitude));

        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);

        var parameters = new PrerequisiteParameters { Item = item };
        var check = new PrerequisiteCheckItemRolledPropertyValue();
        uint magnitudeBits = unchecked((uint)BitConverter.SingleToInt32Bits(magnitude));

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, magnitudeBits, (uint)Property.ShieldCapacityMax, parameters));
        Assert.False(check.Meets(player, PrerequisiteComparison.NotEqual, magnitudeBits, (uint)Property.ShieldCapacityMax, parameters));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 41f, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 42f, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 42f, false)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 42f, true)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 43f, true)]
    [InlineData(PrerequisiteComparison.LessThan, 42f, false)]
    public void ItemRolledPropertyValue_UsesInclusiveComparisonEdges(
        PrerequisiteComparison comparison,
        float expectedMagnitude,
        bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(
            nameof(IItemInfo.Properties),
            ImmutableDictionary<Property, float>.Empty.Add(Property.ShieldCapacityMax, 42f));

        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);

        var parameters = new PrerequisiteParameters { Item = item };
        var check = new PrerequisiteCheckItemRolledPropertyValue();
        uint expectedBits = unchecked((uint)BitConverter.SingleToInt32Bits(expectedMagnitude));

        bool result = check.Meets(player, comparison, expectedBits, (uint)Property.ShieldCapacityMax, parameters);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 0u, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 1u, false)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 0u, true)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 1u, true)]
    [InlineData(PrerequisiteComparison.LessThan, 1u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 0u, false)]
    public void ItemRolledPropertyValue_MissingSlotComparesAsZero(
        PrerequisiteComparison comparison,
        uint expectedBits,
        bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Properties), ImmutableDictionary<Property, float>.Empty);

        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);

        var parameters = new PrerequisiteParameters { Item = item };
        var check = new PrerequisiteCheckItemRolledPropertyValue();

        bool result = check.Meets(player, comparison, expectedBits, (uint)Property.ShieldCapacityMax, parameters);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 0u, false)]
    [InlineData(PrerequisiteComparison.Equal, 7u, true)]
    [InlineData(PrerequisiteComparison.NotEqual, 0u, true)]
    public void ItemSpecial_UsesTemplateSpecialOrMicrochips(PrerequisiteComparison comparison, uint itemSpecialId, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        var entry = new Item2Entry { ItemSpecialId00 = itemSpecialId };
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), entry);

        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);
        itemProxy.SetProperty(nameof(IItem.MicrochipIds), new List<uint>());
        itemProxy.SetProperty(nameof(IItem.RuneSlots), new List<ItemRuneSlot>());

        var check = new PrerequisiteCheckItemSpecial();
        bool result = check.Meets(player, comparison, 0u, 0u, new PrerequisiteParameters { Item = item });

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ItemSpecial_TreatsRuneSlotsAsSpecialPresence()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { ItemSpecialId00 = 0 });

        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);
        itemProxy.SetProperty(nameof(IItem.MicrochipIds), new List<uint>());
        itemProxy.SetProperty(nameof(IItem.RuneSlots), new List<ItemRuneSlot> { new(RuneType.Water) });

        var check = new PrerequisiteCheckItemSpecial();
        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 0u, new PrerequisiteParameters { Item = item }));
    }

    [Fact]
    public void ItemSpecial_UsesItemRuneInstanceTemplateWhenSlotsEmpty()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        IItemInfo info = RecordingDispatchProxy<IItemInfo>.Create(out var infoProxy);
        infoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            ItemSpecialId00    = 0,
            ItemRuneInstanceId = 2u,
        });
        itemProxy.SetProperty(nameof(IItem.Info), info);
        itemProxy.SetProperty(nameof(IItem.MicrochipIds), new List<uint>());
        itemProxy.SetProperty(nameof(IItem.RuneSlots), new List<ItemRuneSlot>());

        IGameTableManager tables = CreateItemRuneInstanceTablesForPrereq();
        var check = new PrerequisiteCheckItemSpecial(tables);

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 0u, new PrerequisiteParameters { Item = item }));
    }

    [Fact]
    public void ItemMicrochip_ObjectIdZero_ComparesInstalledCount()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.MicrochipIds), new List<uint> { 1, 2 });

        var check = new PrerequisiteCheckItemMicrochip();
        var parameters = new PrerequisiteParameters { Item = item };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 2u, 0u, parameters));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 1u, 0u, parameters));
    }

    [Fact]
    public void ItemMicrochip_SocketType_UsesRuneSlotTypeBitmask()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.MicrochipIds), new List<uint>());
        itemProxy.SetProperty(nameof(IItem.RuneSlots), new List<ItemRuneSlot>
        {
            new(RuneType.Fire, 0u)
        });

        var check = new PrerequisiteCheckItemMicrochip();
        var parameters = new PrerequisiteParameters { Item = item };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, (uint)RuneType.Fire, parameters));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 0u, (uint)RuneType.Air, parameters));
    }

    [Fact]
    public void ItemMicrochip_SocketType_FallsBackToItemRuneInstanceTemplate()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        IItemInfo info = RecordingDispatchProxy<IItemInfo>.Create(out var infoProxy);
        infoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { ItemRuneInstanceId = 2u });
        itemProxy.SetProperty(nameof(IItem.Info), info);
        itemProxy.SetProperty(nameof(IItem.MicrochipIds), new List<uint>());
        itemProxy.SetProperty(nameof(IItem.RuneSlots), new List<ItemRuneSlot>());

        IGameTableManager tables = CreateItemRuneInstanceTablesForPrereq();
        var check = new PrerequisiteCheckItemMicrochip(tables);
        var parameters = new PrerequisiteParameters { Item = item };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, (uint)RuneType.Air, parameters));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 0u, (uint)RuneType.Water, parameters));
    }

    [Fact]
    public void ItemMicrochip_ObjectIdZero_PrefersInstalledRuneCountWhenNoWireMicrochips()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.MicrochipIds), new List<uint>());
        itemProxy.SetProperty(nameof(IItem.RuneSlots), new List<ItemRuneSlot>
        {
            new(RuneType.Fire, 100u),
            new(RuneType.Water, 0u)
        });

        var check = new PrerequisiteCheckItemMicrochip();
        var parameters = new PrerequisiteParameters { Item = item };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 1u, 0u, parameters));
    }

    [Fact]
    public void Item2Id_ComparesEvaluatedItemTemplateId()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Id), 12345u);

        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);

        var check = new PrerequisiteCheckItem2Id();
        var parameters = new PrerequisiteParameters { Item = item };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 12345u, parameters));
        Assert.True(check.Meets(player, PrerequisiteComparison.NotEqual, 0u, 999u, parameters));
    }

    [Fact]
    public void ItemLevel_ComparesRequiredLevel()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { RequiredLevel = 50 });

        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);

        var check = new PrerequisiteCheckItemLevel();
        var parameters = new PrerequisiteParameters { Item = item };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 50u, 0u, parameters));
        Assert.True(check.Meets(player, PrerequisiteComparison.GreaterThanOrEqual, 40u, 0u, parameters));
    }

    [Fact]
    public void ItemStatId_ComparesTemplateItemStatId()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { ItemStatId = 42u });

        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);

        var check = new PrerequisiteCheckItemStatId();
        var parameters = new PrerequisiteParameters { Item = item };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 42u, 0u, parameters));
        Assert.True(check.Meets(player, PrerequisiteComparison.NotEqual, 99u, 0u, parameters));
    }

    [Fact]
    public void AppliedItemStatId_ComparesResolvedStatEntryId()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.StatEntry), new ItemStatEntry { Id = 77u });

        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);

        var check = new PrerequisiteCheckAppliedItemStatId();
        var parameters = new PrerequisiteParameters { Item = item };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 77u, parameters));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 0u, 78u, parameters));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 10u, true)]
    [InlineData(PrerequisiteComparison.Equal, 9u, false)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 10u, true)]
    public void DailyLoginDaysTotal_ComparesAccountLoginDaysTotal(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        IAccountInventoryManager inventory = RecordingDispatchProxy<IAccountInventoryManager>.Create(out var inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IAccountInventoryManager.GetDailyLoginDaysTotal), 10u);
        accountProxy.SetProperty(nameof(IAccount.InventoryManager), inventory);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);

        var check = new PrerequisiteCheckDailyLoginDaysTotal();

        bool result = check.Meets(player, comparison, value, objectId: 0u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 3u, true)]
    [InlineData(PrerequisiteComparison.Equal, 2u, false)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 2u, true)]
    public void DailyLoginRewardsAvailable_ComparesAvailableRewardCount(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        IAccountInventoryManager inventory = RecordingDispatchProxy<IAccountInventoryManager>.Create(out var inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IAccountInventoryManager.GetDailyLoginRewardsAvailable), 3u);
        accountProxy.SetProperty(nameof(IAccount.InventoryManager), inventory);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);

        var check = new PrerequisiteCheckDailyLoginRewardsAvailable();

        bool result = check.Meets(player, comparison, value, objectId: 0u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AccountItemCount_RequiresNpcTarget()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out _);
        var check = new PrerequisiteCheckAccountItemCount(tables);

        bool result = check.Meets(
            player,
            PrerequisiteComparison.Equal,
            value: 0u,
            objectId: 500u,
            new PrerequisiteParameters());

        Assert.False(result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 50u, true)]
    [InlineData(PrerequisiteComparison.Equal, 49u, false)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 50u, true)]
    [InlineData(PrerequisiteComparison.LessThan, 51u, true)]
    public void GameFormula_ComparesFormulaDataint0AgainstObjectId(PrerequisiteComparison comparison, uint objectId, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.GameFormula), CreateGameTable(new GameFormulaEntry
        {
            Id       = 378u,
            Dataint0 = 50u
        }));

        var check = new PrerequisiteCheckGameFormula(tables);

        bool result = check.Meets(player, comparison, value: 378u, objectId, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 1675u, 2u, true, true)]
    [InlineData(PrerequisiteComparison.Equal, 1675u, 2u, false, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 1675u, 2u, false, true)]
    public void PathMissionChecklistItemComplete_ComparesChecklistCompletion(
        PrerequisiteComparison comparison,
        uint missionId,
        uint checklistIndex,
        bool itemComplete,
        bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IPathManager pathManager = RecordingDispatchProxy<IPathManager>.Create(out var pathProxy);
        pathProxy.SetMethodHandler(
            nameof(IPathManager.TryIsPathMissionChecklistItemComplete),
            args =>
            {
                args[2] = itemComplete;
                return true;
            });
        playerProxy.SetProperty(nameof(IPlayer.PathManager), pathManager);

        var check = new PrerequisiteCheckPathMissionChecklistItemComplete();

        bool result = check.Meets(player, comparison, checklistIndex, missionId, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Fact]
    public void PathMissionChecklistItemComplete_UnknownMission_NotEqualPasses()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IPathManager pathManager = RecordingDispatchProxy<IPathManager>.Create(out var pathProxy);
        pathProxy.SetMethodHandler(nameof(IPathManager.TryIsPathMissionChecklistItemComplete), args =>
        {
            args[2] = false;
            return false;
        });
        playerProxy.SetProperty(nameof(IPlayer.PathManager), pathManager);

        var check = new PrerequisiteCheckPathMissionChecklistItemComplete();

        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 0u, 1675u, new PrerequisiteParameters()));
        Assert.True(check.Meets(player, PrerequisiteComparison.NotEqual, 0u, 1675u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.NotEqual, 1u, 0u, true)]
    [InlineData(PrerequisiteComparison.Equal, 1u, 1u, true)]
    [InlineData(PrerequisiteComparison.Equal, 1u, 0u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 1u, 1u, false)]
    public void CREDDPendingOrderState_ComparesAccountPendingFlag(
        PrerequisiteComparison comparison,
        uint objectId,
        uint pendingState,
        bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        accountProxy.SetMethodReturn(nameof(IAccount.GetCREDDPendingOrderState), pendingState);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);

        var check = new PrerequisiteCheckCREDDPendingOrderState();

        bool result = check.Meets(player, comparison, value: 0u, objectId, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Fact]
    public void RapidTransport_UsesTaxiNodeFromCastContext()
    {
        var check = new PrerequisiteCheckRapidTransport();
        var parameters = new PrerequisiteParameters
        {
            TaxiNode = 88
        };

        Assert.True(check.Meets(player: null, PrerequisiteComparison.Equal, value: 0u, objectId: 88u, parameters));
        Assert.False(check.Meets(player: null, PrerequisiteComparison.Equal, value: 0u, objectId: 89u, parameters));
    }

    [Fact]
    public void ItemTradeSkillKnown_RequiresNpcTargetAndLearnedSchematicForItem2()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IUnitEntity npc = RecordingDispatchProxy<IUnitEntity>.Create(out var npcProxy);
        npcProxy.SetProperty(nameof(IUnitEntity.Guid), 999u);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 1u);
        playerProxy.SetMethodReturn(nameof(IPlayer.HasLearnedSchematic), true);

        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.Item), CreateGameTable(new Item2Entry { Id = 500u }));
        tableProxy.SetProperty(nameof(IGameTableManager.TradeskillSchematic2), CreateGameTable(
            new TradeskillSchematic2Entry { Id = 10u, TradeSkillId = 1u, Item2IdOutput = 500u }));

        var check = new PrerequisiteCheckItemTradeSkillKnown(tables);
        var parameters = new PrerequisiteParameters { Target = npc };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 1u, 500u, parameters));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 1u, 500u, new PrerequisiteParameters()));
    }

    [Fact]
    public void Unknown275_NonAccountItemContext_UsesItemTradeSkillKnownDuplicateBody()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IUnitEntity npc = RecordingDispatchProxy<IUnitEntity>.Create(out var npcProxy);
        npcProxy.SetProperty(nameof(IUnitEntity.Guid), 999u);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 1u);
        playerProxy.SetMethodReturn(nameof(IPlayer.HasLearnedSchematic), true);

        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.Item), CreateGameTable(new Item2Entry { Id = 500u }));
        tableProxy.SetProperty(nameof(IGameTableManager.TradeskillSchematic2), CreateGameTable(
            new TradeskillSchematic2Entry { Id = 10u, TradeSkillId = 1u, Item2IdOutput = 500u }));

        var check = new PrerequisiteCheckUnknown275(tables);

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 1u, 500u, new PrerequisiteParameters { Target = npc }));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 1u, 500u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Unknown275_AccountItemContext_ComparesHoloWardrobeUnlockToObjectId(bool alreadyUnlocked, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        IAccountCostumeManager costumeManager = RecordingDispatchProxy<IAccountCostumeManager>.Create(out var costumeProxy);
        costumeProxy.SetMethodReturn(nameof(IAccountCostumeManager.HasItemUnlock), alreadyUnlocked);
        accountProxy.SetProperty(nameof(IAccount.CostumeManager), costumeManager);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);

        var check = new PrerequisiteCheckUnknown275(gameTableManager: null);

        bool result = check.Meets(player, PrerequisiteComparison.NotEqual, value: 0u, objectId: 42367u,
            new PrerequisiteParameters { AccountItemContext = true });

        Assert.Equal(expected, result);
        RecordingDispatchProxy<IAccountCostumeManager>.Invocation call = Assert.Single(costumeProxy.GetInvocations(nameof(IAccountCostumeManager.HasItemUnlock)));
        Assert.Equal(42367u, call.Arguments[0]);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 2u, 250u, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 3u, 250u, false)]
    public void ItemTradeSkill_ComparesTradeskillTierRank(
        PrerequisiteComparison comparison,
        uint value,
        uint tradeskillXp,
        bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IUnitEntity npc = RecordingDispatchProxy<IUnitEntity>.Create(out var npcProxy);
        npcProxy.SetProperty(nameof(IUnitEntity.Guid), 999u);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 1u);
        playerProxy.SetMethodReturn(nameof(IPlayer.HasTradeskill), true);
        playerProxy.SetMethodReturn(nameof(IPlayer.GetTradeskillXp), tradeskillXp);

        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.Item), CreateGameTable(new Item2Entry { Id = 500u }));
        tableProxy.SetProperty(nameof(IGameTableManager.TradeskillSchematic2), CreateGameTable(
            new TradeskillSchematic2Entry { Id = 10u, TradeSkillId = 1u, Item2IdOutput = 500u }));
        tableProxy.SetProperty(nameof(IGameTableManager.TradeskillTier), CreateGameTable(
            new TradeskillTierEntry { Id = 1u, TradeSkillId = 1u, Tier = 1u, RequiredXp = 0u },
            new TradeskillTierEntry { Id = 2u, TradeSkillId = 1u, Tier = 2u, RequiredXp = 100u },
            new TradeskillTierEntry { Id = 3u, TradeSkillId = 1u, Tier = 3u, RequiredXp = 300u }));

        var check = new PrerequisiteCheckItemTradeSkill(tables);
        var parameters = new PrerequisiteParameters { Target = npc };

        bool result = check.Meets(player, comparison, value, objectId: 500u, parameters);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Unknown245_NonAccountItemContext_UsesItemTradeSkillDuplicateBody()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IUnitEntity npc = RecordingDispatchProxy<IUnitEntity>.Create(out var npcProxy);
        npcProxy.SetProperty(nameof(IUnitEntity.Guid), 999u);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 1u);
        playerProxy.SetMethodReturn(nameof(IPlayer.HasTradeskill), true);
        playerProxy.SetMethodReturn(nameof(IPlayer.GetTradeskillXp), 250u);

        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.Item), CreateGameTable(new Item2Entry { Id = 500u }));
        tableProxy.SetProperty(nameof(IGameTableManager.TradeskillSchematic2), CreateGameTable(
            new TradeskillSchematic2Entry { Id = 10u, TradeSkillId = 1u, Item2IdOutput = 500u }));
        tableProxy.SetProperty(nameof(IGameTableManager.TradeskillTier), CreateGameTable(
            new TradeskillTierEntry { Id = 1u, TradeSkillId = 1u, Tier = 1u, RequiredXp = 0u },
            new TradeskillTierEntry { Id = 2u, TradeSkillId = 1u, Tier = 2u, RequiredXp = 100u }));

        var check = new PrerequisiteCheckUnknown245(tables);

        Assert.True(check.Meets(player, PrerequisiteComparison.GreaterThanOrEqual, 2u, 500u,
            new PrerequisiteParameters { Target = npc }));
        Assert.False(check.Meets(player, PrerequisiteComparison.GreaterThanOrEqual, 2u, 500u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Unknown245_AccountItemContext_ComparesDyeUnlockToObjectId(bool alreadyUnlocked, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        IGenericUnlockManager unlockManager = RecordingDispatchProxy<IGenericUnlockManager>.Create(out var unlockProxy);
        unlockProxy.SetMethodReturn(nameof(IGenericUnlockManager.IsDyeUnlocked), alreadyUnlocked);
        accountProxy.SetProperty(nameof(IAccount.GenericUnlockManager), unlockManager);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);

        var check = new PrerequisiteCheckUnknown245(gameTableManager: null);

        bool result = check.Meets(player, PrerequisiteComparison.NotEqual, value: 0u, objectId: 3456u,
            new PrerequisiteParameters { AccountItemContext = true });

        Assert.Equal(expected, result);
        RecordingDispatchProxy<IGenericUnlockManager>.Invocation call = Assert.Single(unlockProxy.GetInvocations(nameof(IGenericUnlockManager.IsDyeUnlocked)));
        Assert.Equal(3456u, call.Arguments[0]);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 0u, true)]
    [InlineData(PrerequisiteComparison.Equal, 1u, false)]
    public void ProgressTrackOnMatchingEntity_RequiresPlayerTargetAndComparesScalar(
        PrerequisiteComparison comparison,
        uint value,
        bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 1u);
        IChallengeManager challenges = RecordingDispatchProxy<IChallengeManager>.Create(out _);
        playerProxy.SetProperty(nameof(IPlayer.ChallengeManager), challenges);

        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.Challenge), CreateGameTable<ChallengeEntry>());

        var check = new PrerequisiteCheckProgressTrackOnMatchingEntity(tables);
        var parameters = new PrerequisiteParameters { Target = player };

        bool result = check.Meets(player, comparison, value, objectId: 99u, parameters);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ProgressTrackOnMatchingEntity_UsesChallengeCompletionCountWhenTrackIdMatchesChallenge()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 1u);
        IChallengeManager challenges = RecordingDispatchProxy<IChallengeManager>.Create(out var challengeProxy);
        challengeProxy.SetMethodReturn(nameof(IChallengeManager.GetCompletionCount), 2u);
        playerProxy.SetProperty(nameof(IPlayer.ChallengeManager), challenges);

        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.Challenge), CreateGameTable(new ChallengeEntry { Id = 42u }));

        var check = new PrerequisiteCheckProgressTrackOnMatchingEntity(tables);

        Assert.True(check.Meets(player, PrerequisiteComparison.GreaterThanOrEqual, 2u, 42u, new PrerequisiteParameters { Target = player }));
        Assert.False(check.Meets(player, PrerequisiteComparison.GreaterThanOrEqual, 3u, 42u, new PrerequisiteParameters { Target = player }));
    }

    [Fact]
    public void TradeSkill_ComparesTradeskillTierRankForObjectId()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetMethodReturn(nameof(IPlayer.HasTradeskill), true);
        playerProxy.SetMethodReturn(nameof(IPlayer.GetTradeskillXp), 150u);

        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.TradeskillTier), CreateGameTable(
            new TradeskillTierEntry { Id = 1u, TradeSkillId = (uint)TradeskillType.Mining, Tier = 2u, RequiredXp = 100u }));

        var check = new PrerequisiteCheckTradeSkill(tables);

        Assert.True(check.Meets(player, PrerequisiteComparison.GreaterThanOrEqual, 2u, (uint)TradeskillType.Mining, new PrerequisiteParameters()));
        Assert.False(check.Meets(player, PrerequisiteComparison.GreaterThanOrEqual, 3u, (uint)TradeskillType.Mining, new PrerequisiteParameters()));
    }

    [Fact]
    public void ChallengeObject_RequiresActivatedChallenge()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IChallengeManager challenges = RecordingDispatchProxy<IChallengeManager>.Create(out var challengeProxy);
        challengeProxy.SetMethodReturn(nameof(IChallengeManager.IsChallengeActivated), true);
        playerProxy.SetProperty(nameof(IPlayer.ChallengeManager), challenges);

        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.Challenge), CreateGameTable(new ChallengeEntry { Id = 42u }));

        var check = new PrerequisiteCheckChallengeObject(tables);

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 1u, 42u, new PrerequisiteParameters()));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 1u, 99u, new PrerequisiteParameters()));
    }

    [Fact]
    public void PlayerGlobalTreeLookup_RequiresNpcAndChallengePresence()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 1u);
        IUnitEntity npc = RecordingDispatchProxy<IUnitEntity>.Create(out var npcProxy);
        npcProxy.SetProperty(nameof(IUnitEntity.Guid), 999u);
        IChallengeManager challenges = RecordingDispatchProxy<IChallengeManager>.Create(out var challengeProxy);
        challengeProxy.SetMethodReturn(nameof(IChallengeManager.IsChallengeActivated), true);
        playerProxy.SetProperty(nameof(IPlayer.ChallengeManager), challenges);

        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.Challenge), CreateGameTable(new ChallengeEntry { Id = 42u }));

        var check = new PrerequisiteCheckPlayerGlobalTreeLookup(tables);
        var parameters = new PrerequisiteParameters { Target = npc };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 1u, 42u, parameters));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 1u, 42u, new PrerequisiteParameters()));
    }

    [Fact]
    public void CreatureDifficultyRank_ComparesTargetDifficultyRankValue()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out var targetProxy);
        targetProxy.SetProperty(nameof(IWorldEntity.CreatureInfo), RecordingDispatchProxy<ICreatureInfo>.Create(out var infoProxy));
        infoProxy.SetProperty(nameof(ICreatureInfo.DifficultyEntry), new Creature2DifficultyEntry { Id = 7u, RankValue = 3u });

        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out _);

        var check = new PrerequisiteCheckCreatureDifficultyRank(tables);
        var parameters = new PrerequisiteParameters { Target = target };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 3u, 7u, parameters));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 4u, 7u, parameters));
    }

    [Fact]
    public void LiveEventTreeLookup_RequiresNpcTargetAndTableRow()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IUnitEntity npc = RecordingDispatchProxy<IUnitEntity>.Create(out var npcProxy);
        npcProxy.SetProperty(nameof(IUnitEntity.Guid), 999u);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 1u);

        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.LiveEvent), CreateGameTable(new LiveEventEntry { Id = 42u }));

        var check = new PrerequisiteCheckLiveEventTreeLookup(tables);
        var parameters = new PrerequisiteParameters { Target = npc };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 42u, parameters));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 0u, 99999u, parameters));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 0u, 42u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 0u, true)]
    [InlineData(PrerequisiteComparison.Equal, 1u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 1u, true)]
    public void LiveEventWorldFactionBranch_ComparesFactionBranchProgress(
        PrerequisiteComparison comparison,
        uint value,
        bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IUnitEntity npc = RecordingDispatchProxy<IUnitEntity>.Create(out var npcProxy);
        npcProxy.SetProperty(nameof(IUnitEntity.Guid), 999u);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 1u);

        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.LiveEvent), CreateGameTable(new LiveEventEntry { Id = 42u }));

        var check = new PrerequisiteCheckLiveEventWorldFactionBranch(tables);
        var parameters = new PrerequisiteParameters { Target = npc };

        bool result = check.Meets(player, comparison, value, objectId: 42u, parameters);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 50u, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 51u, false)]
    [InlineData(PrerequisiteComparison.LessThan, 51u, true)]
    public void HealthScaled_ComparesHealthPercent(
        PrerequisiteComparison comparison,
        uint value,
        bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IWorldEntity.Health), 500u);
        playerProxy.SetProperty(nameof(IWorldEntity.MaxHealth), 1000u);

        var check = new PrerequisiteCheckHealthScaled();

        bool result = check.Meets(player, comparison, value, objectId: 0u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 800u, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 801u, false)]
    [InlineData(PrerequisiteComparison.LessThan, 801u, true)]
    public void Health_ComparesRawHealthOnTarget(
        PrerequisiteComparison comparison,
        uint value,
        bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out var targetProxy);
        targetProxy.SetProperty(nameof(IWorldEntity.Health), 800u);

        var check = new PrerequisiteCheckHealth();
        var parameters = new PrerequisiteParameters { Target = target };

        bool result = check.Meets(player, comparison, value, objectId: 0u, parameters);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 75u, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 76u, false)]
    [InlineData(PrerequisiteComparison.Equal, 75u, true)]
    public void ZoneExplored_ComparesMapZoneExploredPercent(
        PrerequisiteComparison comparison,
        uint value,
        bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IZoneMapManager zoneMapManager = RecordingDispatchProxy<IZoneMapManager>.Create(out var zoneMapProxy);
        zoneMapProxy.SetMethodReturn(nameof(IZoneMapManager.GetMapZoneExploredPercent), (byte)75);
        playerProxy.SetProperty(nameof(IPlayer.ZoneMapManager), zoneMapManager);

        var check = new PrerequisiteCheckZoneExplored();

        bool result = check.Meets(player, comparison, value, objectId: 12u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 1u, true)]
    [InlineData(PrerequisiteComparison.Equal, 0u, false)]
    public void InCombat_ComparesTargetCombatState(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out var targetProxy);
        targetProxy.SetProperty(nameof(IUnitEntity.InCombat), true);

        var check = new PrerequisiteCheckInCombat();
        var parameters = new PrerequisiteParameters { Target = target };

        Assert.Equal(expected, check.Meets(player, comparison, value, 0u, parameters));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 1u, true)]
    [InlineData(PrerequisiteComparison.Equal, 0u, false)]
    public void IsPlayer_ComparesTargetEntityKind(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        var check = new PrerequisiteCheckIsPlayer();

        Assert.Equal(expected, check.Meets(player, comparison, value, 0u, new PrerequisiteParameters { Target = player }));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 1u, true)]
    [InlineData(PrerequisiteComparison.Equal, 0u, false)]
    public void IsCreature_ComparesTargetEntityKind(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out _);

        var check = new PrerequisiteCheckIsCreature();
        var parameters = new PrerequisiteParameters { Target = creature };

        Assert.Equal(expected, check.Meets(player, comparison, value, 0u, parameters));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 1u, true)]
    [InlineData(PrerequisiteComparison.Equal, 0u, false)]
    public void DeadState_ComparesAliveState(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IUnitEntity.IsAlive), false);

        var check = new PrerequisiteCheckDeadState();

        Assert.Equal(expected, check.Meets(player, comparison, value, 0u, new PrerequisiteParameters()));
    }

    [Fact]
    public void ActionSetSpell_FindsShortcutOnAnyActionSet()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out var spellProxy);
        IActionSet actionSet = RecordingDispatchProxy<IActionSet>.Create(out var actionSetProxy);
        IActionSetShortcut shortcut = RecordingDispatchProxy<IActionSetShortcut>.Create(out var shortcutProxy);
        shortcutProxy.SetProperty(nameof(IActionSetShortcut.ShortcutType), ShortcutType.SpellbookItem);
        shortcutProxy.SetProperty(nameof(IActionSetShortcut.ObjectId), 9001u);
        actionSetProxy.SetProperty(nameof(IActionSet.Actions), new[] { shortcut });
        spellProxy.SetMethodReturn(nameof(ISpellManager.GetActionSet), actionSet);
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);

        var check = new PrerequisiteCheckActionSetSpell();

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 1u, 9001u, new PrerequisiteParameters()));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 1u, 9002u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 1u, true)]
    [InlineData(PrerequisiteComparison.Equal, 0u, false)]
    public void IsGroupLeader_ComparesPartyLeader(PrerequisiteComparison comparison, uint value, bool expected)
    {
        var leader = new Identity { RealmId = 1, Id = 2ul };
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Identity), leader);
        playerProxy.SetProperty(nameof(IPlayer.GroupAssociation), 99ul);

        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out var groupProxy);
        groupProxy.SetMethodHandler(nameof(IGroupStateManager.TryGetGroup), args =>
        {
            args[1] = new GroupLootState { GroupId = (ulong)args[0], Leader = leader };
            return true;
        });

        var check = new PrerequisiteCheckIsGroupLeader(groupStateManager);

        Assert.Equal(expected, check.Meets(player, comparison, value, 0u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 1u, GroupFlags.Raid, true)]
    [InlineData(PrerequisiteComparison.Equal, 1u, GroupFlags.None, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 0u, GroupFlags.Raid, true)]
    [InlineData(PrerequisiteComparison.NotEqual, 0u, GroupFlags.None, false)]
    public void GroupIsRaid_ComparesCurrentGroupRaidFlag(PrerequisiteComparison comparison, uint value, GroupFlags flags, bool expected)
    {
        var leader = new Identity { RealmId = 1, Id = 2ul };
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Identity), leader);
        playerProxy.SetProperty(nameof(IPlayer.GroupAssociation), 99ul);

        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out var groupProxy);
        groupProxy.SetMethodHandler(nameof(IGroupStateManager.TryGetGroup), args =>
        {
            args[1] = new GroupLootState
            {
                GroupId = (ulong)args[0],
                Flags   = flags,
                Leader  = leader
            };
            return true;
        });

        var check = new PrerequisiteCheckGroupIsRaid(groupStateManager);

        Assert.Equal(expected, check.Meets(player, comparison, value, 0u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 0u, true)]
    [InlineData(PrerequisiteComparison.NotEqual, 0u, false)]
    public void GroupIsRaid_TreatsMissingGroupAsNonRaid(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.GroupAssociation), 0ul);

        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        var check = new PrerequisiteCheckGroupIsRaid(groupStateManager);

        Assert.Equal(expected, check.Meets(player, comparison, value, 0u, new PrerequisiteParameters()));
    }

    [Fact]
    public void UnitEntityType_ComparesEntityTypeEnum()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out var targetProxy);
        targetProxy.SetProperty(nameof(IWorldEntity.Type), EntityType.Player);

        var check = new PrerequisiteCheckUnitEntityType();
        var parameters = new PrerequisiteParameters { Target = target };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, (uint)EntityType.Player, 0u, parameters));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, (uint)EntityType.NonPlayer, 0u, parameters));
    }

    [Theory]
    [InlineData(EntityType.Pet, PrerequisiteComparison.Equal, 1u, true)]
    [InlineData(EntityType.EsperPet, PrerequisiteComparison.Equal, 1u, true)]
    [InlineData(EntityType.Player, PrerequisiteComparison.Equal, 1u, false)]
    [InlineData(EntityType.Player, PrerequisiteComparison.NotEqual, 1u, true)]
    public void PetOrEsperPetEntity_ComparesTargetEntityKind(EntityType entityType, PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out var targetProxy);
        targetProxy.SetProperty(nameof(IWorldEntity.Type), entityType);

        var check = new PrerequisiteCheckPetOrEsperPetEntity();
        var parameters = new PrerequisiteParameters { Target = target };

        Assert.Equal(expected, check.Meets(player, comparison, value, 0u, parameters));
    }

    [Fact]
    public void Currency_ComparesPlayerCurrencyAmount()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        ICurrency currency = RecordingDispatchProxy<ICurrency>.Create(out var currencyProxy);
        currencyProxy.SetProperty(nameof(ICurrency.Id), CurrencyType.Credits);
        currencyProxy.SetProperty(nameof(ICurrency.Amount), 500ul);
        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out var currencyManagerProxy);
        currencyManagerProxy.SetMethodHandler("GetEnumerator", _ => new List<ICurrency> { currency }.GetEnumerator());
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);

        var check = new PrerequisiteCheckCurrency();

        Assert.True(check.Meets(player, PrerequisiteComparison.GreaterThanOrEqual, 400u, (uint)CurrencyType.Credits, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 5000u, true)]
    [InlineData(PrerequisiteComparison.LessThan, 5000u, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 5000u, false)]
    public void AccountCurrencyAmount_ComparesAccountCurrencyAmount(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        IAccountCurrencyManager currencyManager = RecordingDispatchProxy<IAccountCurrencyManager>.Create(out var currencyProxy);
        currencyProxy.SetMethodReturn(nameof(IAccountCurrencyManager.GetCurrencyAmount), 4999ul);
        accountProxy.SetProperty(nameof(IAccount.CurrencyManager), currencyManager);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);

        var check = new PrerequisiteCheckAccountCurrencyAmount();

        bool result = check.Meets(player, comparison, value, (uint)AccountCurrencyType.CrimsonEssence, new PrerequisiteParameters());

        Assert.Equal(expected, result);
        RecordingDispatchProxy<IAccountCurrencyManager>.Invocation call = Assert.Single(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.GetCurrencyAmount)));
        Assert.Equal(AccountCurrencyType.CrimsonEssence, call.Arguments[0]);
    }

    [Fact]
    public void AccountCurrencyAmount_TreatsIdsOutsideClientSlotRangeAsZero()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        IAccountCurrencyManager currencyManager = RecordingDispatchProxy<IAccountCurrencyManager>.Create(out var currencyProxy);
        currencyProxy.SetMethodReturn(nameof(IAccountCurrencyManager.GetCurrencyAmount), 4999ul);
        accountProxy.SetProperty(nameof(IAccount.CurrencyManager), currencyManager);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);

        var check = new PrerequisiteCheckAccountCurrencyAmount();

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, value: 0u, objectId: 19u, new PrerequisiteParameters()));
        Assert.Empty(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.GetCurrencyAmount)));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.GreaterThan, 12000u, 12001ul, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 12000u, 12000ul, false)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 7750u, 7000ul, true)]
    public void LoyaltyRewards_ComparesCosmicRewardAmount(PrerequisiteComparison comparison, uint value, ulong currentAmount, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        IAccountCurrencyManager currencyManager = RecordingDispatchProxy<IAccountCurrencyManager>.Create(out var currencyProxy);
        currencyProxy.SetMethodReturn(nameof(IAccountCurrencyManager.GetCurrencyAmount), currentAmount);
        accountProxy.SetProperty(nameof(IAccount.CurrencyManager), currencyManager);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);

        var check = new PrerequisiteCheckLoyaltyRewards();

        bool result = check.Meets(player, comparison, value, objectId: 0u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
        RecordingDispatchProxy<IAccountCurrencyManager>.Invocation call = Assert.Single(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.GetCurrencyAmount)));
        Assert.Equal(AccountCurrencyType.CosmicReward, call.Arguments[0]);
    }

    [Fact]
    public void ItemQuantity_ComparesInventoryStackCount()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out var inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IInventory.GetItemCount), 3u);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);

        var check = new PrerequisiteCheckItemQuantity();

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 3u, 42u, new PrerequisiteParameters()));
    }

    [Fact]
    public void Guild_ComparesMembershipInGuildId()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IGuildBase guild = RecordingDispatchProxy<IGuildBase>.Create(out var guildProxy);
        guildProxy.SetProperty(nameof(IGuildBase.Id), 77ul);
        IGuildManager guildManager = RecordingDispatchProxy<IGuildManager>.Create(out var guildManagerProxy);
        guildManagerProxy.SetMethodHandler("GetEnumerator", _ => new List<IGuildBase> { guild }.GetEnumerator());
        playerProxy.SetProperty(nameof(IPlayer.GuildManager), guildManager);

        var check = new PrerequisiteCheckGuild();

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 1u, 77u, new PrerequisiteParameters()));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 1u, 78u, new PrerequisiteParameters()));
    }

    [Fact]
    public void IsObjectiveActive_UsesQuestManagerActiveObjectiveId()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out var questProxy);
        questProxy.SetMethodReturn(nameof(IQuestManager.IsActiveObjectiveId), true);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);

        var check = new PrerequisiteCheckIsObjectiveActive();

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 1u, 99u, new PrerequisiteParameters()));
    }

    [Fact]
    public void QuestObjective_ComparesObjectiveCompletion()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out var questProxy);
        IQuest quest = RecordingDispatchProxy<IQuest>.Create(out var questEntityProxy);
        IQuestObjective objective = RecordingDispatchProxy<IQuestObjective>.Create(out var objectiveProxy);
        IQuestObjectiveInfo objectiveInfo = RecordingDispatchProxy<IQuestObjectiveInfo>.Create(out var infoProxy);
        infoProxy.SetProperty(nameof(IQuestObjectiveInfo.Id), 55u);
        objectiveProxy.SetProperty(nameof(IQuestObjective.ObjectiveInfo), objectiveInfo);
        objectiveProxy.SetMethodReturn(nameof(IQuestObjective.IsComplete), true);
        questEntityProxy.SetMethodHandler("GetEnumerator", _ => new List<IQuestObjective> { objective }.GetEnumerator());
        questProxy.SetMethodReturn(nameof(IQuestManager.GetActiveQuests), new List<IQuest> { quest });
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);

        var check = new PrerequisiteCheckQuestObjective();

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 1u, 55u, new PrerequisiteParameters()));
    }

    [Fact]
    public void CreatureState_ComparesTargetUnitState()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out var targetProxy);
        targetProxy.SetMethodReturn(nameof(IUnitEntity.HasUnitState), true);

        var check = new PrerequisiteCheckCreatureState();
        var parameters = new PrerequisiteParameters { Target = target };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 12u, 0u, parameters));
    }

    [Fact]
    public void HouseOwnership_ComparesOwnedResidenceProperty()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 10ul);
        IResidenceManager residenceManager = RecordingDispatchProxy<IResidenceManager>.Create(out var residenceProxy);
        IResidence residence = RecordingDispatchProxy<IResidence>.Create(out var homeProxy);
        homeProxy.SetProperty(nameof(IResidence.OwnerId), 10ul);
        homeProxy.SetProperty(nameof(IResidence.PropertyInfoId), PropertyInfoId.Residence);
        residenceProxy.SetProperty(nameof(IResidenceManager.Residence), residence);
        playerProxy.SetProperty(nameof(IPlayer.ResidenceManager), residenceManager);

        var check = new PrerequisiteCheckHouseOwnership();

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 1u, (uint)PropertyInfoId.Residence, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true, true)]
    [InlineData(PrerequisiteComparison.Equal, false, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true, false)]
    [InlineData(PrerequisiteComparison.NotEqual, false, true)]
    public void HousingResidenceLoaded_ComparesCurrentResidencePresence(PrerequisiteComparison comparison, bool hasResidence, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IWorldEntity.Guid), 1u);
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out var targetProxy);
        targetProxy.SetProperty(nameof(IWorldEntity.Guid), 2u);

        IResidenceManager residenceManager = RecordingDispatchProxy<IResidenceManager>.Create(out var residenceProxy);
        residenceProxy.SetProperty(nameof(IResidenceManager.Residence), hasResidence ? RecordingDispatchProxy<IResidence>.Create(out _) : null);
        playerProxy.SetProperty(nameof(IPlayer.ResidenceManager), residenceManager);

        var check = new PrerequisiteCheckHousingResidenceLoaded();
        var parameters = new PrerequisiteParameters { Target = target };

        bool result = check.Meets(player, comparison, hasResidence ? 1u : 0u, objectId: 0u, parameters);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true, true)]
    [InlineData(PrerequisiteComparison.Equal, false, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true, false)]
    [InlineData(PrerequisiteComparison.NotEqual, false, true)]
    public void HousingNeighborResidence_ComparesNeighborPresence(
        PrerequisiteComparison comparison,
        bool hasNeighbor,
        bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IWorldEntity.Guid), 1u);
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out var targetProxy);
        targetProxy.SetProperty(nameof(IWorldEntity.Guid), 2u);

        IResidence residence = RecordingDispatchProxy<IResidence>.Create(out var residenceProxy);
        IEnumerable<(ulong CharacterId, byte PermissionLevel)> neighbors = hasNeighbor
            ? new List<(ulong, byte)> { (99ul, 1) }
            : Array.Empty<(ulong, byte)>();
        residenceProxy.SetMethodHandler(nameof(IResidence.GetNeighbors), _ => neighbors);

        IResidenceManager residenceManager = RecordingDispatchProxy<IResidenceManager>.Create(out var residenceManagerProxy);
        residenceManagerProxy.SetProperty(nameof(IResidenceManager.Residence), residence);
        playerProxy.SetProperty(nameof(IPlayer.ResidenceManager), residenceManager);

        var check = new PrerequisiteCheckHousingNeighborResidence();
        bool result = check.Meets(player, comparison, value: 1u, objectId: 0u, new PrerequisiteParameters { Target = target });

        Assert.Equal(expected, result);
    }

    [Fact]
    public void HousingNeighborResidence_ReturnsFalseWithoutNpcTarget()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        var check = new PrerequisiteCheckHousingNeighborResidence();

        bool result = check.Meets(player, PrerequisiteComparison.Equal, 1u, objectId: 0u, new PrerequisiteParameters());

        Assert.False(result);
    }

    [Fact]
    public void EvaluatedEntityPresent_ReturnsTrueForNotEqualWhenTargetEntityPresent()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out _);
        var check = new PrerequisiteCheckEvaluatedEntityPresent();

        bool result = check.Meets(
            player,
            PrerequisiteComparison.NotEqual,
            value: 18u,
            objectId: 0u,
            new PrerequisiteParameters { Target = target });

        Assert.True(result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal)]
    [InlineData(PrerequisiteComparison.GreaterThan)]
    public void EvaluatedEntityPresent_ReturnsFalseForNonNotEqualOrMissingTarget(PrerequisiteComparison comparison)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        var check = new PrerequisiteCheckEvaluatedEntityPresent();
        var parameters = new PrerequisiteParameters();

        Assert.False(check.Meets(player, comparison, value: 18u, objectId: 0u, parameters));
    }

    [Fact]
    public void Spell4EffectGroupListContainsGroupId_MatchesListSlot()
    {
        var entry = new Spell4EffectGroupListEntry
        {
            Id                     = 10u,
            Spell4EffectGroupId02  = 77u
        };

        Assert.True(NexusForever.Game.Spell.Spell.Spell4EffectGroupListContainsGroupId(entry, 77u));
        Assert.False(NexusForever.Game.Spell.Spell.Spell4EffectGroupListContainsGroupId(entry, 99u));
    }

    [Fact]
    public void Spell4GroupListContainsGroupId_MatchesListSlot()
    {
        var entry = new Spell4GroupListEntry
        {
            Id             = 1378u,
            SpellGroupId00 = 576u
        };

        Assert.True(NexusForever.Game.Spell.Spell.Spell4GroupListContainsGroupId(entry, 576u));
        Assert.False(NexusForever.Game.Spell.Spell.Spell4GroupListContainsGroupId(entry, 556u));
    }

    [Fact]
    public void Spell4GroupListsOverlap_MatchesSharedGroupSlot()
    {
        var rentalUnlockList = new Spell4GroupListEntry
        {
            Id             = 1378u,
            SpellGroupId00 = 576u
        };

        var rentalSummonList = new Spell4GroupListEntry
        {
            Id             = 9001u,
            SpellGroupId03 = 576u
        };

        var unrelatedList = new Spell4GroupListEntry
        {
            Id             = 9002u,
            SpellGroupId00 = 346u
        };

        Assert.True(NexusForever.Game.Spell.Spell.Spell4GroupListsOverlap(rentalUnlockList, rentalSummonList));
        Assert.False(NexusForever.Game.Spell.Spell.Spell4GroupListsOverlap(unrelatedList, rentalSummonList));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true, true)]
    [InlineData(PrerequisiteComparison.Equal, false, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true, false)]
    [InlineData(PrerequisiteComparison.NotEqual, false, true)]
    public void ActiveSpellEffectOnUnit_UsesTrackedSpellStateFallback(PrerequisiteComparison comparison, bool active, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetMethodReturn(nameof(IPlayer.HasTrackedSpellState), active);

        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out _);
        var check = new PrerequisiteCheckActiveSpellEffectOnUnit(NullLogger<PrerequisiteCheckActiveSpellEffectOnUnit>.Instance, tables);

        bool result = check.Meets(player, comparison, value: 77357u, objectId: 0u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 5u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 5u, true)]
    public void Spell4EffectCategoryOnUnit_WithoutActiveEffects(PrerequisiteComparison comparison, uint groupId, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.Spell4EffectGroupList), CreateGameTable<Spell4EffectGroupListEntry>());

        var check = new PrerequisiteCheckSpell4EffectCategoryOnUnit(tables);
        bool result = check.Meets(player, comparison, groupId, objectId: 0u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true)]
    [InlineData(PrerequisiteComparison.NotEqual, false)]
    public void InTriggerVolume_ComparesWorldLocationContainment(PrerequisiteComparison comparison, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Position), new Vector3(10f, 0f, 10f));
        playerProxy.SetProperty(nameof(IPlayer.HitRadius), 1f);

        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.WorldLocation2), CreateGameTable(new WorldLocation2Entry
        {
            Id                 = 99u,
            Position0          = 10f,
            Position1          = 0f,
            Position2          = 10f,
            Radius             = 5f,
            MaxVerticalDistance = 0f
        }));

        var check = new PrerequisiteCheckInTriggerVolume(tables);
        bool result = check.Meets(player, comparison, value: 1u, objectId: 99u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true, true)]
    [InlineData(PrerequisiteComparison.NotEqual, true, false)]
    public void ScanCreature_ComparesPathManagerScanCredit(PrerequisiteComparison comparison, bool hasScanCredit, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IDatacubeManager datacubeManager = RecordingDispatchProxy<IDatacubeManager>.Create(out var datacubeProxy);
        datacubeProxy.SetMethodReturn(nameof(IDatacubeManager.GetScientistCreatureScanProgress), hasScanCredit ? 1u : 0u);
        playerProxy.SetProperty(nameof(IPlayer.DatacubeManager), datacubeManager);

        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.PathScientistCreatureInfo), CreateGameTable(new PathScientistCreatureInfoEntry { Id = 34u }));

        var check = new PrerequisiteCheckScanCreature(tables);
        bool result = check.Meets(player, comparison, value: 1u, objectId: 34u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 0u, 1u, true)]
    [InlineData(PrerequisiteComparison.Equal, 1u, 1u, false)]
    [InlineData(PrerequisiteComparison.Equal, 2u, 5u, true)]
    [InlineData(PrerequisiteComparison.Equal, 2u, 3u, false)]
    public void ScanCreature_WithMultiStepChecklist_ComparesChecklistBit(
        PrerequisiteComparison comparison,
        uint checklistIndex,
        uint progress,
        bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IDatacubeManager datacubeManager = RecordingDispatchProxy<IDatacubeManager>.Create(out var datacubeProxy);
        datacubeProxy.SetMethodReturn(nameof(IDatacubeManager.GetScientistCreatureScanProgress), progress);
        playerProxy.SetProperty(nameof(IPlayer.DatacubeManager), datacubeManager);

        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.PathScientistCreatureInfo), CreateGameTable(new PathScientistCreatureInfoEntry
        {
            Id             = 50u,
            ChecklistCount = 3u
        }));

        var check = new PrerequisiteCheckScanCreature(tables);
        bool result = check.Meets(player, comparison, checklistIndex, objectId: 50u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true)]
    [InlineData(PrerequisiteComparison.NotEqual, false)]
    public void Waypoint_ComparesPositionalRequirementCone(PrerequisiteComparison comparison, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Position), new Vector3(0f, 0f, 5f));

        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out var targetProxy);
        targetProxy.SetProperty(nameof(IUnitEntity.Position), Vector3.Zero);
        targetProxy.SetProperty(nameof(IUnitEntity.Rotation), new Vector3(0f, 0f, 0f));

        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.PositionalRequirement), CreateGameTable(new PositionalRequirementEntry
        {
            Id          = 7u,
            AngleCenter = 90u,
            AngleRange  = 180u
        }));

        var check = new PrerequisiteCheckWaypoint(tables);
        var parameters = new PrerequisiteParameters { Target = target };
        bool result = check.Meets(player, comparison, value: 1u, objectId: 7u, parameters);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0u, false)]
    [InlineData(1u, true)]
    public void PositionalRequirementBetweenCasterAndTarget_HonoursCasterTargetSwapFlag(uint flags, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Position), new Vector3(0f, 0f, 5f));
        playerProxy.SetProperty(nameof(IPlayer.Rotation), Vector3.Zero);

        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out var targetProxy);
        targetProxy.SetProperty(nameof(IUnitEntity.Position), Vector3.Zero);
        targetProxy.SetProperty(nameof(IUnitEntity.Rotation), Vector3.Zero);

        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.PositionalRequirement), CreateGameTable(new PositionalRequirementEntry
        {
            Id          = 6u,
            AngleCenter = 0u,
            AngleRange  = 60u,
            Flags       = flags
        }));

        var check = new PrerequisiteCheckPositionalRequirementBetweenCasterAndTarget(
            NullLogger<PrerequisiteCheckPositionalRequirementBetweenCasterAndTarget>.Instance,
            tables);
        var parameters = new PrerequisiteParameters { Target = target };
        bool result = check.Meets(player, PrerequisiteComparison.Equal, value: 0u, objectId: 6u, parameters);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void PositionalRequirementBetweenCasterAndTarget_ReturnsTrueForNotEqualWhenNativeInputsMissing()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.PositionalRequirement), CreateGameTable<PositionalRequirementEntry>());

        var check = new PrerequisiteCheckPositionalRequirementBetweenCasterAndTarget(
            NullLogger<PrerequisiteCheckPositionalRequirementBetweenCasterAndTarget>.Instance,
            tables);

        Assert.True(check.Meets(player, PrerequisiteComparison.NotEqual, value: 0u, objectId: 6u, new PrerequisiteParameters()));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, value: 0u, objectId: 6u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true)]
    [InlineData(PrerequisiteComparison.NotEqual, false)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, false)]
    public void IsLocalPlayerEntity_TreatsSessionPlayerAsNativeLocalEntity(PrerequisiteComparison comparison, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        var check = new PrerequisiteCheckIsLocalPlayerEntity();

        bool result = check.Meets(player, comparison, value: 0u, objectId: 0u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 42u, true)]
    [InlineData(PrerequisiteComparison.Equal, 43u, false)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 40u, true)]
    public void EvalContextFloatByObjectId_ComparesItemEvalStatScalarWhenItemPresent(
        PrerequisiteComparison comparison,
        uint required,
        bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.StatEntry), new ItemStatEntry
        {
            ItemStatTypeEnum = [ItemStatType.Unknown4],
            ItemStatData     = [42u]
        });
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);

        var check = new PrerequisiteCheckEvalContextFloatByObjectId();
        var parameters = new PrerequisiteParameters { Item = item };
        bool result = check.Meets(player, comparison, required, objectId: (uint)ItemStatType.Unknown4, parameters);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void EvalContextFloatByObjectId_ReturnsFalseWithoutItemEvalContext()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        var check = new PrerequisiteCheckEvalContextFloatByObjectId();

        bool result = check.Meets(player, PrerequisiteComparison.Equal, 1u, objectId: 4u, new PrerequisiteParameters());

        Assert.False(result);
    }

    [Fact]
    public void AccountItemListItem2CountNpc_RequiresNpcTarget()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out _);
        var check = new PrerequisiteCheckAccountItemListItem2CountNpc(tables);

        bool result = check.Meets(
            player,
            PrerequisiteComparison.Equal,
            value: 1u,
            objectId: 500u,
            new PrerequisiteParameters());

        Assert.False(result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 1u, true)]
    [InlineData(PrerequisiteComparison.Equal, 2u, false)]
    public void AccountItemListItem2Count_ComparesAccountRowsForItem2(
        PrerequisiteComparison comparison,
        uint required,
        bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        IAccountInventoryManager inventory = RecordingDispatchProxy<IAccountInventoryManager>.Create(out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy);
        accountProxy.SetProperty(nameof(IAccount.InventoryManager), inventory);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);

        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.Item), CreateGameTable(new Item2Entry
        {
            Id            = 500u,
            MaxStackCount = 99u
        }));

        IAccountInventoryItem accountItem = RecordingDispatchProxy<IAccountInventoryItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IAccountInventoryItem.Entry), new AccountItemEntry { Item2Id = 500u });

        inventoryProxy.SetMethodHandler(
            nameof(IEnumerable<IAccountInventoryItem>.GetEnumerator),
            _ => new List<IAccountInventoryItem> { accountItem }.GetEnumerator());

        var check = new PrerequisiteCheckAccountItemListItem2Count(tables);
        bool result = check.Meets(player, comparison, required, objectId: 500u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.NotEqual, 0u, true)]
    [InlineData(PrerequisiteComparison.NotEqual, 1u, false)]
    [InlineData(PrerequisiteComparison.Equal, 1u, true)]
    [InlineData(PrerequisiteComparison.Equal, 0u, false)]
    public void DoesNotOwnAccountItemOnCharacter_ComparesCharacterItem2FromValue(
        PrerequisiteComparison comparison,
        uint characterItemCount,
        bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out var inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IInventory.GetItemCount), characterItemCount);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);

        var check = new PrerequisiteCheckDoesNotOwnAccountItemOnCharacter();
        bool result = check.Meets(player, comparison, value: 92102u, objectId: 0u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Fact]
    public void DoesNotOwnAccountItemOnCharacter_DoesNotCountAccountInventoryRow()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out var inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IInventory.GetItemCount), 0u);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);

        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        IAccountInventoryManager accountInventory = RecordingDispatchProxy<IAccountInventoryManager>.Create(out var accountInventoryProxy);
        IAccountInventoryItem accountItem = RecordingDispatchProxy<IAccountInventoryItem>.Create(out var accountItemProxy);
        accountItemProxy.SetProperty(nameof(IAccountInventoryItem.Entry), new AccountItemEntry { Item2Id = 92102u });
        accountInventoryProxy.SetMethodHandler(
            nameof(IEnumerable<IAccountInventoryItem>.GetEnumerator),
            _ => new List<IAccountInventoryItem> { accountItem }.GetEnumerator());
        accountProxy.SetProperty(nameof(IAccount.InventoryManager), accountInventory);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);

        var check = new PrerequisiteCheckDoesNotOwnAccountItemOnCharacter();
        bool result = check.Meets(player, PrerequisiteComparison.NotEqual, value: 92102u, objectId: 0u, new PrerequisiteParameters());

        Assert.True(result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 0x8u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 0x8u, true)]
    public void WrongSpellMechanic_WithoutActiveSpells(PrerequisiteComparison comparison, uint flags, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        var check = new PrerequisiteCheckWrongSpellMechanic();
        bool result = check.Meets(player, comparison, flags, objectId: 0u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 0x4u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 0x4u, true)]
    public void ActiveSpellTargetMechanic_WithoutActiveSpells(PrerequisiteComparison comparison, uint flags, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        var check = new PrerequisiteCheckActiveSpellTargetMechanic();
        bool result = check.Meets(player, comparison, flags, objectId: 0u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 3u, true)]
    [InlineData(PrerequisiteComparison.Equal, 2u, false)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 3u, true)]
    public void SpellTier_ComparesSpellManagerTier(PrerequisiteComparison comparison, uint requiredTier, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out var spellProxy);
        spellProxy.SetMethodReturn(nameof(ISpellManager.GetSpell), RecordingDispatchProxy<ICharacterSpell>.Create(out _));
        spellProxy.SetMethodReturn(nameof(ISpellManager.GetSpellTier), (byte)3);
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);

        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.Spell4), CreateGameTable(new Spell4Entry
        {
            Id                     = 9001u,
            Spell4BaseIdBaseSpell  = 100u
        }));

        var check = new PrerequisiteCheckSpellTier(tables);
        bool result = check.Meets(player, comparison, requiredTier, objectId: 9001u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 3u, true)]
    [InlineData(PrerequisiteComparison.Equal, 4u, true)]
    [InlineData(PrerequisiteComparison.Equal, 2u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 3u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 4u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 2u, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 3u, false)]
    public void SpellTierUnlocked_ComparesSpell4TierIndexAndIgnoresValue(PrerequisiteComparison comparison, byte currentTier, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out var spellProxy);
        spellProxy.SetMethodReturn(nameof(ISpellManager.GetSpell), RecordingDispatchProxy<ICharacterSpell>.Create(out _));
        spellProxy.SetMethodReturn(nameof(ISpellManager.GetSpellTier), currentTier);
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);

        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.Spell4), CreateGameTable(new Spell4Entry
        {
            Id                    = 9001u,
            Spell4BaseIdBaseSpell = 100u,
            TierIndex             = 3u
        }));

        var check = new PrerequisiteCheckSpellTierUnlocked(tables);
        bool result = check.Meets(player, comparison, value: uint.MaxValue, objectId: 9001u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true, true)]
    [InlineData(PrerequisiteComparison.Equal, false, false)]
    public void UnderSpellOnTarget_UsesTargetActiveSpellState(PrerequisiteComparison comparison, bool active, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out var targetProxy);
        targetProxy.SetMethodReturn(nameof(IUnitEntity.HasTrackedSpellState), active);

        var check = new PrerequisiteCheckUnderSpellOnTarget(NullLogger<PrerequisiteCheckUnderSpellOnTarget>.Instance);
        var parameters = new PrerequisiteParameters { Target = target };
        bool result = check.Meets(player, comparison, value: 42u, objectId: 0u, parameters);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 1u, true)]
    [InlineData(PrerequisiteComparison.Equal, 0u, false)]
    public void TargetEntityLookupHit_ComparesMapEntityResolution(PrerequisiteComparison comparison, uint required, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out var targetProxy);
        targetProxy.SetProperty(nameof(IWorldEntity.Guid), 9001u);
        if (expected)
            playerProxy.SetMethodReturn(nameof(IPlayer.GetVisible), target);
        else
            playerProxy.SetMethodReturn(nameof(IPlayer.GetVisible), null);

        var check = new PrerequisiteCheckTargetEntityLookupHit();
        var parameters = new PrerequisiteParameters { Target = target };
        bool result = check.Meets(player, comparison, required, objectId: 0u, parameters);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 4u, true)]
    [InlineData(PrerequisiteComparison.Equal, 8u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 4u, false)]
    public void Datacube_ComparesProgressBitmask(PrerequisiteComparison comparison, uint requiredMask, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IDatacubeManager datacubeManager = RecordingDispatchProxy<IDatacubeManager>.Create(out var datacubeProxy);
        IDatacube datacube = RecordingDispatchProxy<IDatacube>.Create(out var datacubeEntryProxy);
        datacubeEntryProxy.SetProperty(nameof(IDatacube.Progress), 6u);
        datacubeProxy.SetMethodReturn(nameof(IDatacubeManager.GetDatacube), datacube);
        playerProxy.SetProperty(nameof(IPlayer.DatacubeManager), datacubeManager);

        var check = new PrerequisiteCheckDatacube();
        bool result = check.Meets(player, comparison, requiredMask, objectId: 12u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Volume_UsesJournalDatacubeType()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IDatacubeManager datacubeManager = RecordingDispatchProxy<IDatacubeManager>.Create(out var datacubeProxy);
        IDatacube datacube = RecordingDispatchProxy<IDatacube>.Create(out _);
        datacubeProxy.SetMethodReturn(nameof(IDatacubeManager.GetDatacube), datacube);
        playerProxy.SetProperty(nameof(IPlayer.DatacubeManager), datacubeManager);

        var check = new PrerequisiteCheckVolume();
        check.Meets(player, PrerequisiteComparison.Equal, value: 0u, objectId: 5u, new PrerequisiteParameters());

        RecordingDispatchProxy<IDatacubeManager>.Invocation call =
            Assert.Single(datacubeProxy.GetInvocations(nameof(IDatacubeManager.GetDatacube)));
        Assert.Equal(DatacubeType.Journal, call.Arguments[1]);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 1u, true)]
    [InlineData(PrerequisiteComparison.Equal, 0u, false)]
    public void PetMatchTarget_ComparesVanityPetCreatureId(PrerequisiteComparison comparison, uint required, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.VanityPetGuid), 1u);
        IPlayer target = RecordingDispatchProxy<IPlayer>.Create(out var targetProxy);
        targetProxy.SetProperty(nameof(IPlayer.VanityPetGuid), 2u);
        IPetEntity playerPet = RecordingDispatchProxy<IPetEntity>.Create(out var playerPetProxy);
        playerPetProxy.SetProperty(nameof(IWorldEntity.CreatureId), 77u);
        IPetEntity targetPet = RecordingDispatchProxy<IPetEntity>.Create(out var targetPetProxy);
        targetPetProxy.SetProperty(nameof(IWorldEntity.CreatureId), 77u);
        playerProxy.SetMethodReturn(nameof(IPlayer.GetVisible), playerPet);
        targetProxy.SetMethodReturn(nameof(IPlayer.GetVisible), targetPet);

        var check = new PrerequisiteCheckPetMatchTarget();
        var parameters = new PrerequisiteParameters { Target = target };
        bool result = check.Meets(player, comparison, required, objectId: 0u, parameters);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, (uint)ModeType.Swim, true)]
    [InlineData(PrerequisiteComparison.Equal, (uint)ModeType.Walk, false)]
    public void MovementMode_ComparesMovementManagerMode(PrerequisiteComparison comparison, uint requiredMode, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out var movementProxy);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetMode), ModeType.Swim);
        playerProxy.SetProperty(nameof(IWorldEntity.MovementManager), movementManager);

        var check = new PrerequisiteCheckMovementMode();
        bool result = check.Meets(player, comparison, requiredMode, objectId: 0u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 1u, true)]
    [InlineData(PrerequisiteComparison.Equal, 0u, false)]
    public void Mount_ComparesPlatformEntityType(PrerequisiteComparison comparison, uint required, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.PlatformGuid), 42u);
        IMountEntity mount = RecordingDispatchProxy<IMountEntity>.Create(out var mountProxy);
        mountProxy.SetProperty(nameof(IWorldEntity.Type), EntityType.Mount);
        playerProxy.SetMethodReturn(nameof(IPlayer.GetVisible), mount);

        var check = new PrerequisiteCheckMount();
        bool result = check.Meets(player, comparison, required, objectId: 0u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 1u, true)]
    [InlineData(PrerequisiteComparison.Equal, 0u, false)]
    public void Jump_ComparesJumpStateFlag(PrerequisiteComparison comparison, uint required, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out var movementProxy);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetState), StateFlags.Jump);
        playerProxy.SetProperty(nameof(IWorldEntity.MovementManager), movementManager);

        var check = new PrerequisiteCheckJump();
        bool result = check.Meets(player, comparison, required, objectId: 0u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 3u, true)]
    [InlineData(PrerequisiteComparison.Equal, 2u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 3u, false)]
    public void InPhase_ComparesPublicEventPhase(PrerequisiteComparison comparison, uint requiredPhase, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IWorldEntity.PublicEventPhase), 3u);

        var check = new PrerequisiteCheckInPhase();
        bool result = check.Meets(player, comparison, requiredPhase, objectId: 0u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 1u, true)]
    [InlineData(PrerequisiteComparison.Equal, 0u, false)]
    public void CanSeePhase_ComparesPlayerAndTargetPhase(PrerequisiteComparison comparison, uint required, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IWorldEntity.PublicEventPhase), 5u);
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out var targetProxy);
        targetProxy.SetProperty(nameof(IWorldEntity.PublicEventPhase), 5u);

        var check = new PrerequisiteCheckCanSeePhase();
        var parameters = new PrerequisiteParameters { Target = target };
        bool result = check.Meets(player, comparison, required, objectId: 0u, parameters);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Distance_ComparesPlayerToTargetPosition()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IGridEntity.Position), Vector3.Zero);
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out var targetProxy);
        targetProxy.SetProperty(nameof(IGridEntity.Position), new Vector3(3f, 4f, 0f));

        var check = new PrerequisiteCheckDistance();
        var parameters = new PrerequisiteParameters { Target = target };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 5u, 0u, parameters));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 6u, 0u, parameters));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 2u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 2u, false)]
    public void ChallengeTier_ComparesChallengeCompletionCount(PrerequisiteComparison comparison, uint requiredTier, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IChallengeManager challengeManager = RecordingDispatchProxy<IChallengeManager>.Create(out var challengeProxy);
        challengeProxy.SetMethodReturn(nameof(IChallengeManager.GetCompletionCount), 2u);
        playerProxy.SetProperty(nameof(IPlayer.ChallengeManager), challengeManager);

        var check = new PrerequisiteCheckChallengeTier();
        bool result = check.Meets(player, comparison, requiredTier, objectId: 42u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CreatureDifficulty_ComparesTargetCreatureDifficultyId()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out var targetProxy);
        targetProxy.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry { Creature2DifficultyId = 8u });

        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.Creature2Difficulty), CreateGameTable(
            new Creature2DifficultyEntry { Id = 8u, RankValue = 2u }));

        var check = new PrerequisiteCheckCreatureDifficulty(tables);
        var parameters = new PrerequisiteParameters { Target = target };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 8u, parameters));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 0u, 9u, parameters));
    }

    [Fact]
    public void ItemStatData_ComparesFirstStandardStatDataSlot()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.StatEntry), new ItemStatEntry
        {
            ItemStatTypeEnum = [ItemStatType.Standard, ItemStatType.None],
            ItemStatData     = [9001u, 0u]
        });

        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);

        var check = new PrerequisiteCheckItemStatData();
        var parameters = new PrerequisiteParameters { Item = item };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 9001u, 0u, parameters));
        Assert.True(check.Meets(player, PrerequisiteComparison.NotEqual, 1u, 0u, parameters));
    }

    private static IGameTableManager CreateItemRuneInstanceTablesForPrereq()
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);
        proxy.SetProperty(nameof(IGameTableManager.ItemRuneInstance), CreateGameTable(new ItemRuneInstanceEntry
        {
            Id                    = 2u,
            DefinedSocketCount    = 1u,
            DefinedSocketType00   = 7u,
        }));
        return tables;
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
        FieldInfo idField = typeof(T).GetField("Id", BindingFlags.Public | BindingFlags.Instance);
        return idField == null ? 0u : (uint)idField.GetValue(entry)!;
    }

    private static void SetAutoProperty<T>(T target, string propertyName, object value)
    {
        PropertyInfo property = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        property?.SetValue(target, value);
    }

    private static void SetPrivateField<T>(T target, string fieldName, object value)
    {
        FieldInfo field = typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(target, value);
    }
}
