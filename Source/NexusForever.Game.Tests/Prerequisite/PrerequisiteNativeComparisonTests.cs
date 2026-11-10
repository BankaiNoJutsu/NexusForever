using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Game.Static.Spell;
using Path = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_NATIVE_COMPARISONS_20261107.md):
//  - type 11 OtherPrerequisite (evaluator case 0xb): comparison 2 returns the NOT of the nested prerequisite, every other comparison returns the nested result. Retail uses comparison 1 (3,559
//    slots), 2 (322) and 0 (22).
//  - type 52 Path (evaluator case 0x34, FUN_14049fd50): the player's path equals Value; comparison 1 returns that, 2 its negation, any other comparison false. Retail: Equal 538, NotEqual 14.
//  - type 105 SpellEffectTypeOnUnit (evaluator case 0x69, inline; Decomp/Analysis/PREREQUISITE_105_SPELL_EFFECT_TYPE_20261110.md): the id compared with each active node is the slot's
//    ObjectId, not its Value; comparison 1 returns the match, 2 its negation, any other comparison false. Retail: 7 slots, ObjectId 0/3/31/83/100/112, Value always 0.
//  - type 116 Stealth (evaluator case 0x74, inline): the flag is 1 < unit+0x54, Equal returns it, NotEqual its negation, anything else is false; ObjectId and Value are not read. Retail: 32 slots,
//    22 with Value 28455 (Spell4 "Stalker - Optic Camouflage", a Stealth effect), which the client ignores.
//  - type 190 PetFlair (1404a14d0): the per-player unlock count for PetFlair ObjectId is compared with the count the row expects; Equal / NotEqual only, Value not read. Retail: 298 slots, all NotEqual,
//    referenced by AccountItem and ItemSpecial rows ("usable while the flair is not unlocked").
//  - types 194 MountUsage / 195 HoverboardUsage (14049ced0): the player's zone and its parents are walked; WorldZone flag 0x10 anywhere on the chain denies. Equal / NotEqual only.
//  - type 178 (14049d2c0): ObjectId is an ItemSlot id; the flag is "an item is equipped at the index of the lowest bit of equippedSlotFlags". Retail: ItemSlot 46 ArmorGadget (0x800 -> index 11)
//    unlocks the gadget action slot; Value (100 on that row) is not read.
//  - type 104 (14046a210): the unit has an active effect in spell effect group ObjectId. Retail row 23816: NotEqual group 12 (housing statue / warhorn buffs) on the plugs that grant them.
//  - type 289 (14049f0c0): bit 0 of the client's RealmDataCenter row flags (PTR rows only). Not a test realm here, so Equal is false and NotEqual true.
public class PrerequisiteNativeComparisonTests
{
    [Theory]
    [InlineData(PrerequisiteComparison.Equal, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, false)]
    public void TestDataCenterFlagIsFalseOnThisServer(PrerequisiteComparison comparison, bool expected)
    {
        Assert.Equal(expected, new PrerequisiteCheckTestDataCenter().Meets(null, comparison, 0u, 0u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true, true)]
    [InlineData(PrerequisiteComparison.NotEqual, true, false)]
    [InlineData(PrerequisiteComparison.NotEqual, false, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, true, false)]
    public void SpellEffectGroupFlagIsEqualOrNotEqualOnly(PrerequisiteComparison comparison, bool hasEffectInGroup, bool expected)
    {
        Assert.Equal(expected, PrerequisiteCheckSpellCooldownNodeInServiceSet.Evaluate(comparison, hasEffectInGroup));
    }

    [Fact]
    public void SpellEffectGroupWithoutAUnitIsFalseForEveryComparison()
    {
        var check = new PrerequisiteCheckSpellCooldownNodeInServiceSet(null);
        Assert.False(check.MeetsUnit(null, PrerequisiteComparison.NotEqual, 0u, 12u, new PrerequisiteParameters()));
        Assert.False(check.MeetsUnit(null, PrerequisiteComparison.Equal, 0u, 12u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(46u, true, PrerequisiteComparison.Equal, true)]
    [InlineData(46u, false, PrerequisiteComparison.Equal, false)]
    [InlineData(46u, false, PrerequisiteComparison.NotEqual, true)]
    [InlineData(46u, true, PrerequisiteComparison.GreaterThanOrEqual, false)]
    [InlineData(99u, true, PrerequisiteComparison.Equal, false)]      // no ItemSlot row: NotEqual only
    [InlineData(99u, true, PrerequisiteComparison.NotEqual, true)]
    public void ItemSlotEquippedTestsTheEquippedIndexOfTheItemSlotRow(uint itemSlotId, bool gadgetEquipped, PrerequisiteComparison comparison, bool expected)
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.ItemSlot), NexusForever.Game.Tests.Transport.PlayerBindPointTests.Table(
            new ItemSlotEntry { Id = 46u, EquippedSlotFlags = 0x800u }));
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out var inventoryProxy);
        IItem gadget = RecordingDispatchProxy<IItem>.Create(out _);
        inventoryProxy.SetMethodHandler(nameof(IInventory.GetItem),
            args => gadgetEquipped && args.Length == 2 && args[0] is InventoryLocation.Equipped && (uint)args[1] == (uint)EquippedItem.Gadget ? gadget : null);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);

        Assert.Equal(expected, new PrerequisiteCheckItemSlotEquipped(tables).Meets(player, comparison, 100u, itemSlotId, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(0u, 0u, PrerequisiteComparison.Equal, true)]         // open zone
    [InlineData(0xD2u, 0u, PrerequisiteComparison.Equal, false)]     // the zone itself denies (retail interior rows are 0xD2)
    [InlineData(0x100u, 0x10u, PrerequisiteComparison.Equal, false)] // a parent denies; the allow bit on the child does not override it
    [InlineData(0u, 0x10u, PrerequisiteComparison.NotEqual, true)]
    [InlineData(0u, 0u, PrerequisiteComparison.GreaterThan, false)]
    public void MountAndHoverboardUsageWalkTheZoneChainForTheDenyBit(uint zoneFlags, uint parentFlags, PrerequisiteComparison comparison, bool expected)
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.WorldZone), NexusForever.Game.Tests.Transport.PlayerBindPointTests.Table(
            new WorldZoneEntry { Id = 7u, Flags = parentFlags }));
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Zone), new WorldZoneEntry { Id = 8u, ParentZoneId = 7u, Flags = zoneFlags });

        Assert.Equal(expected, new PrerequisiteCheckMountUsage(tables).Meets(player, comparison, 0u, 0u, new PrerequisiteParameters()));
        Assert.Equal(expected, new PrerequisiteCheckHoverboardUsage(tables).Meets(player, comparison, 0u, 0u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.NotEqual, false, true)]
    [InlineData(PrerequisiteComparison.NotEqual, true, false)]
    [InlineData(PrerequisiteComparison.Equal, true, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, true, false)]
    public void PetFlairTestsWhetherTheFlairIsUnlocked(PrerequisiteComparison comparison, bool unlocked, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IPetCustomisationManager pets = RecordingDispatchProxy<IPetCustomisationManager>.Create(out var petProxy);
        petProxy.SetMethodHandler(nameof(IPetCustomisationManager.HasFlair), args => unlocked && (ushort)args[0] == 337);
        playerProxy.SetProperty(nameof(IPlayer.PetCustomisationManager), pets);

        var check = new PrerequisiteCheckPetFlair();

        // Value is non-zero on purpose: it must not be read
        Assert.Equal(expected, check.Meets(player, comparison, 5u, 337u, new PrerequisiteParameters()));
        Assert.Empty(petProxy.GetInvocations(nameof(IPetCustomisationManager.GetCustomisation)));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 28455u, true, true)]
    [InlineData(PrerequisiteComparison.Equal, 28455u, false, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 28455u, true, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 0u, false, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 0u, true, false)]
    public void StealthReturnsTheStealthFlagAndIgnoresValue(PrerequisiteComparison comparison, uint value, bool stealthed, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IUnitEntity.IsStealthed), stealthed);
        IUnitEntity creature = RecordingDispatchProxy<IUnitEntity>.Create(out var creatureProxy);
        creatureProxy.SetProperty(nameof(IUnitEntity.IsStealthed), stealthed);

        var check = new PrerequisiteCheckStealth();

        Assert.Equal(expected, check.Meets(player, comparison, value, 0u, new PrerequisiteParameters()));
        Assert.Equal(expected, check.MeetsUnit(creature, comparison, value, 0u, new PrerequisiteParameters()));
        Assert.False(check.MeetsUnit(null, comparison, value, 0u, new PrerequisiteParameters()));
    }

    // Retail row 7405 is (Equal, ObjectId 83 Stealth, Value 0); row 37231 is three NotEqual slots with ObjectId 31, 100 and 112 (the disguise effects) and Value 0.
    [Theory]
    [InlineData(PrerequisiteComparison.Equal, SpellEffectType.Stealth, true)]
    [InlineData(PrerequisiteComparison.Equal, SpellEffectType.Disguise, false)]
    [InlineData(PrerequisiteComparison.NotEqual, SpellEffectType.Stealth, false)]
    [InlineData(PrerequisiteComparison.NotEqual, SpellEffectType.Disguise, true)]
    [InlineData(PrerequisiteComparison.NotEqual, SpellEffectType.MimicDisguise, true)]
    [InlineData(PrerequisiteComparison.NotEqual, SpellEffectType.DisguiseOutfit, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, SpellEffectType.Stealth, false)]
    [InlineData((PrerequisiteComparison)0, SpellEffectType.Stealth, false)]
    public void SpellEffectTypeOnUnitReadsTheEffectTypeFromObjectId(PrerequisiteComparison comparison, SpellEffectType objectId, bool expected)
    {
        // the unit only has an active Stealth effect
        var asked = new List<SpellEffectType>();
        bool result = PrerequisiteCheckSpellEffectTypeOnUnit.Evaluate(comparison, (uint)objectId, type =>
        {
            asked.Add(type);
            return type == SpellEffectType.Stealth;
        });

        Assert.Equal(expected, result);
        Assert.Equal([objectId], asked);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true, true)]
    [InlineData(PrerequisiteComparison.Equal, false, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true, false)]
    [InlineData(PrerequisiteComparison.NotEqual, false, true)]
    [InlineData((PrerequisiteComparison)0, true, true)]                    // 22 retail slots use comparison 0
    [InlineData((PrerequisiteComparison)0, false, false)]
    [InlineData(PrerequisiteComparison.GreaterThan, true, true)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, false, false)]
    public void OtherPrerequisiteNegatesOnlyForComparisonTwo(PrerequisiteComparison comparison, bool nested, bool expected)
    {
        IPrerequisiteManager manager = RecordingDispatchProxy<IPrerequisiteManager>.Create(out var proxy);
        proxy.SetMethodHandler(nameof(IPrerequisiteManager.Meets), _ => nested);
        proxy.SetMethodHandler(nameof(IPrerequisiteManager.MeetsUnit), _ => nested);
        var check = new PrerequisiteCheckOtherPrerequisite(NullLogger<PrerequisiteCheckOtherPrerequisite>.Instance, manager);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        Assert.Equal(expected, check.Meets(player, comparison, 0u, 1234u, new PrerequisiteParameters()));
        Assert.Equal(expected, check.MeetsUnit(player, comparison, 0u, 1234u, new PrerequisiteParameters()));
    }

    // Native type 3 (FUN_14049c560) compares the class for equality or inequality and returns false for any other comparison; the server returned true.
    [Theory]
    [InlineData(PrerequisiteComparison.GreaterThan)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual)]
    [InlineData((PrerequisiteComparison)0)]
    public void ClassNeverMeetsAnUnsupportedComparison(PrerequisiteComparison comparison)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.Class), Class.Warrior);

        var check = new PrerequisiteCheckClass(NullLogger<PrerequisiteCheckClass>.Instance);
        Assert.False(check.Meets(player, comparison, (uint)Class.Warrior, 0u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, Path.Soldier, true)]
    [InlineData(PrerequisiteComparison.Equal, Path.Scientist, false)]
    [InlineData(PrerequisiteComparison.NotEqual, Path.Soldier, false)]
    [InlineData(PrerequisiteComparison.NotEqual, Path.Scientist, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, Path.Soldier, false)]
    [InlineData(PrerequisiteComparison.LessThan, Path.Scientist, false)]
    [InlineData((PrerequisiteComparison)0, Path.Soldier, false)]
    public void PathComparesTheActivePathWithValue(PrerequisiteComparison comparison, Path value, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IPathManager pathManager = RecordingDispatchProxy<IPathManager>.Create(out var pathProxy);
        pathProxy.SetMethodHandler(nameof(IPathManager.IsPathActive), args => (Path)args[0] == Path.Soldier);
        playerProxy.SetProperty(nameof(IPlayer.PathManager), pathManager);

        var check = new PrerequisiteCheckPath(NullLogger<PrerequisiteCheckPath>.Instance);

        Assert.Equal(expected, check.Meets(player, comparison, (uint)value, 0u, new PrerequisiteParameters()));
    }
}
