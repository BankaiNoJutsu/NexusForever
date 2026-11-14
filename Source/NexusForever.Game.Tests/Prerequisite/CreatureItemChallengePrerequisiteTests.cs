using NexusForever.Game.Abstract.Challenges;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_CREATURE_ITEM_CHALLENGE_20261107.md):
//  - 38 IsCreature (slot +0x350, FUN_14049ecc0): the unit's creature-info id (0 when it has none) is compared for equality with Value (a Creature2 id, for example 16623); Equal / NotEqual only. 1,182 slots.
//  - 69 ChallengeRequirement (+0x440, FUN_14049f990 -> FUN_14048d530): the challenge record's field +0x30 (the client's IsActivated accessor reads it); Equal returns it, NotEqual its negation, Value not read. 396 slots.
//  - 14 ItemOnCharacter (+0x128) and 13 ItemEquipped (+0x120): FUN_1403ac590 with a location mask (0x303 and 1) and the Item2 id Value; Equal returns "found", NotEqual its negation, ObjectId not read.
//    The mask bit n selects inventory location n: 13 is Equipped, 14 is Equipped, Inventory and locations 8 and 9 (not modelled here).
public class CreatureItemChallengePrerequisiteTests
{
    // ---- 38 ----

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 16623u, true)]
    [InlineData(PrerequisiteComparison.Equal, 20814u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 16623u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 20814u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 16623u, false)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 20814u, false)]
    public void IsCreatureComparesTheUnitsCreatureIdWithValue(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IUnitEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IWorldEntity.CreatureId), 16623u);

        var check = new PrerequisiteCheckIsCreature();
        Assert.Equal(expected, check.MeetsUnit(creature, comparison, value, 0u, new PrerequisiteParameters()));

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        // The primary player has no creature record (id zero); a relationship target must not replace it.
        Assert.Equal(comparison == PrerequisiteComparison.NotEqual,
            check.Meets(player, comparison, value, 0u, new PrerequisiteParameters { Target = creature }));
    }

    [Fact]
    public void AUnitWithoutACreatureHasIdZero()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        var check = new PrerequisiteCheckIsCreature();

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 0u, new PrerequisiteParameters()));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 16623u, 0u, new PrerequisiteParameters()));
        Assert.True(check.Meets(player, PrerequisiteComparison.NotEqual, 16623u, 0u, new PrerequisiteParameters()));
    }

    // ---- 69 ----

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 1078u, 0u, true)]
    [InlineData(PrerequisiteComparison.Equal, 1078u, 5u, true)]       // Value is not read
    [InlineData(PrerequisiteComparison.NotEqual, 1078u, 0u, false)]
    [InlineData(PrerequisiteComparison.Equal, 99u, 0u, false)]        // no record: not activated
    [InlineData(PrerequisiteComparison.NotEqual, 99u, 0u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 1078u, 0u, false)]
    public void ChallengeRequirementTestsWhetherTheChallengeIsActivated(PrerequisiteComparison comparison, uint challengeId, uint value, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IChallengeManager manager = RecordingDispatchProxy<IChallengeManager>.Create(out var managerProxy);
        managerProxy.SetMethodHandler(nameof(IChallengeManager.IsChallengeActivated), args => (ushort)args[0] == 1078);
        managerProxy.SetMethodHandler(nameof(IChallengeManager.GetCompletionCount), args => 7u);
        playerProxy.SetProperty(nameof(IPlayer.ChallengeManager), manager);

        var check = new PrerequisiteCheckChallengeRequirement(Microsoft.Extensions.Logging.Abstractions.NullLogger<PrerequisiteCheckChallengeRequirement>.Instance);
        Assert.Equal(expected, check.Meets(player, comparison, value, challengeId, new PrerequisiteParameters()));
    }

    // ---- 14 and 13 ----

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 81325u, true)]      // in the Inventory bag
    [InlineData(PrerequisiteComparison.Equal, 14405u, true)]      // equipped
    [InlineData(PrerequisiteComparison.Equal, 55555u, false)]     // only in the bank
    [InlineData(PrerequisiteComparison.Equal, 99u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 81325u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 99u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 81325u, false)]
    public void ItemOnCharacterFindsTheItemIdInEquippedAndInventoryBags(PrerequisiteComparison comparison, uint itemId, bool expected)
    {
        IPlayer player = PlayerWithBags();

        // ObjectId is not read: a stray value there must not change the result.
        Assert.Equal(expected, new PrerequisiteCheckItemOnCharacter().Meets(player, comparison, itemId, 5480u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 14405u, true)]      // equipped
    [InlineData(PrerequisiteComparison.Equal, 81325u, false)]     // inventory only
    [InlineData(PrerequisiteComparison.NotEqual, 14405u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 81325u, true)]
    public void ItemEquippedFindsTheItemIdInTheEquippedBagOnly(PrerequisiteComparison comparison, uint itemId, bool expected)
    {
        IPlayer player = PlayerWithBags();

        Assert.Equal(expected, new PrerequisiteCheckItemEquipped().Meets(player, comparison, itemId, 0u, new PrerequisiteParameters()));
    }

    private static IPlayer PlayerWithBags()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out var inventoryProxy);
        var bags = new List<IBag>
        {
            Bag(InventoryLocation.Equipped, 14405u),
            Bag(InventoryLocation.Inventory, 81325u),
            Bag(InventoryLocation.PlayerBank, 55555u)
        };
        inventoryProxy.SetMethodHandler("GetEnumerator", _ => bags.GetEnumerator());
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        return player;
    }

    private static IBag Bag(InventoryLocation location, params uint[] itemIds)
    {
        IBag bag = RecordingDispatchProxy<IBag>.Create(out var bagProxy);
        var items = itemIds.Select(id =>
        {
            IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
            itemProxy.SetProperty(nameof(IItem.Id), id);
            return item;
        }).ToList();
        bagProxy.SetProperty(nameof(IBag.Location), location);
        bagProxy.SetMethodHandler("GetEnumerator", _ => items.GetEnumerator());
        return bag;
    }
}
