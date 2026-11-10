using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Spell;

/// <summary>
/// Which spells equipped items grant. Row shapes are the retail ones: ItemSpecial 9260 grants Spell4 81880 ("Rune Special - Head - DPS - Untouchable", a Proc row and a Proxy
/// row that ticks every 1000 ms with effect flag 2); runes are family 33 items with an ItemRuneInstance that names an item set and a power of 1 to 3.
/// </summary>
public class EquipmentSpellRulesTests
{
    private const uint GearItem = 100u, RuneA = 200u, RuneB = 201u, PlainRune = 202u;
    private const uint UntouchableSpecial = 9260u, UntouchableSpell = 81880u;
    private const uint GadgetSpecial = 50u, GadgetSpell = 83589u;
    private const uint SetId = 6u, SetBonusSpell = 87345u, SetBonusSpellHigh = 87346u;

    [Fact]
    public void AnEquippedItemGrantsItsOwnSpecialAndTheSpecialsOfItsSocketedRunes()
    {
        IGameTableManager tables = Tables();
        IItem gear = Item(GearItem, RuneA, PlainRune);

        uint[] granted = EquipmentSpellRules.GetGrantedSpells([gear], tables).Select(g => g.Spell4Id).ToArray();

        Assert.Contains(GadgetSpell, granted);         // the item's own special
        Assert.Contains(UntouchableSpell, granted);    // the special of the rune socketed into it
    }

    [Theory]
    [InlineData(new[] { RuneA }, false, false)]                 // power 2
    [InlineData(new[] { RuneA, RuneB }, true, false)]           // power 2 + 1 = 3 reaches the first bonus
    [InlineData(new[] { RuneA, RuneB, RuneA }, true, true)]     // power 5 reaches both
    public void RuneSetBonusesAreGrantedOnceTheSocketedRunesOfTheSetReachTheirPower(uint[] runes, bool expectFirst, bool expectSecond)
    {
        IGameTableManager tables = Tables();
        // spread over two items: the power of a set adds up across everything equipped
        IItem first = Item(GearItem, runes.Take(1).ToArray());
        IItem second = Item(GearItem, runes.Skip(1).ToArray());

        uint[] granted = EquipmentSpellRules.GetGrantedSpells([first, second], tables).Select(g => g.Spell4Id).ToArray();

        Assert.Equal(expectFirst, granted.Contains(SetBonusSpell));
        Assert.Equal(expectSecond, granted.Contains(SetBonusSpellHigh));
    }

    // 158 ItemSetBonus rows grant a property: an offset (for example +0.004) or a scalar (for example 1.003 on base health), never both.
    [Fact]
    public void RuneSetPropertyBonusesFollowTheSamePowerThresholds()
    {
        IGameTableManager tables = Tables();

        var low = EquipmentSpellRules.GetSetPropertyBonuses([Item(GearItem, RuneA)], tables).ToArray();              // power 2
        Assert.Equal(((NexusForever.Game.Static.Entity.Property)154, 0f, 0.004f), Assert.Single(low));

        var high = EquipmentSpellRules.GetSetPropertyBonuses([Item(GearItem, RuneA, RuneB)], tables).ToArray();      // power 3
        Assert.Equal(2, high.Length);
        Assert.Contains((NexusForever.Game.Static.Entity.Property.BaseHealth, 1.003f, 0f), high);

        Assert.Empty(EquipmentSpellRules.GetSetPropertyBonuses([Item(GearItem, PlainRune)], tables));
    }

    [Fact]
    public void NothingEquippedGrantsNothing()
    {
        Assert.Empty(EquipmentSpellRules.GetGrantedSpells([], Tables()));
        Assert.Empty(EquipmentSpellRules.GetGrantedSpells(null, Tables()));
    }

    // Only spells the server can also take away again are granted.
    [Theory]
    [InlineData(UntouchableSpell, true)]     // Proc plus a ticking Proxy marked as living with the spell
    [InlineData(GadgetSpell, true)]          // Proc only
    [InlineData(58678u, false)]              // "Dungeon Test" gear: property modifiers plus ItemVisualSwap
    [InlineData(70000u, false)]              // a ticking Proxy that is not marked as living with the spell
    [InlineData(70001u, false)]              // a spell with a finite duration
    [InlineData(99999u, false)]              // no such spell
    public void OnlySpellsThatCanBeRevokedAreSupported(uint spell4Id, bool expected)
    {
        Assert.Equal(expected, EquipmentSpellRules.IsSupported(spell4Id, Tables()));
    }

    [Fact]
    public void ASpellWithAProcOrATickLeavesSomethingToCheckItsPresenceBy()
    {
        IGameTableManager tables = Tables();
        Assert.True(EquipmentSpellRules.LeavesPresence(UntouchableSpell, tables));
        Assert.False(EquipmentSpellRules.LeavesPresence(70002u, tables));    // a visual only
    }

    // Class.spell4IdInnateAbilityPassive runs parallel to the stance list. Retail shapes: Warrior Juggernaut (44932) is property modifiers, a one-off resource reset,
    // a SpellForceRemove and a Proxy ticking every 5000 ms; Engineer Eradicate (45000) carries a UnitPropertyConversion row, which the unit stores with its spell property
    // modifiers and so can be revoked by spell. A passive with a row that cannot be revoked (an ItemVisualSwap here) takes the whole class out.
    [Theory]
    [InlineData(new uint[] { 80000u, 80001u, 0u }, (byte)0, 80000u)]
    [InlineData(new uint[] { 80000u, 80001u, 0u }, (byte)1, 80001u)]
    [InlineData(new uint[] { 80000u, 80001u, 0u }, (byte)2, 0u)]          // no passive for that stance
    [InlineData(new uint[] { 80000u, 80002u, 0u }, (byte)0, 0u)]          // one passive of the class cannot be revoked: the class is not served at all
    [InlineData(new uint[] { 0u, 0u, 0u }, (byte)0, 0u)]
    [InlineData(new uint[] { 80003u, 80001u, 0u }, (byte)0, 80003u)]      // a property conversion can be revoked
    public void TheStancePassiveIsThePassiveOfTheCurrentStanceWhenTheWholeClassCanBeRevoked(uint[] passives, byte innateIndex, uint expected)
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.Spell4), CreateGameTable(
            new Spell4Entry { Id = 80000u }, new Spell4Entry { Id = 80001u }, new Spell4Entry { Id = 80002u }, new Spell4Entry { Id = 80003u }));
        proxy.SetProperty(nameof(IGameTableManager.Spell4Effects), CreateGameTable(
            new Spell4EffectsEntry { Id = 1u, SpellId = 80000u, EffectType = SpellEffectType.UnitPropertyModifier, Flags = 2u },
            new Spell4EffectsEntry { Id = 2u, SpellId = 80000u, EffectType = SpellEffectType.VitalModifier },
            new Spell4EffectsEntry { Id = 3u, SpellId = 80000u, EffectType = SpellEffectType.SpellForceRemove },
            new Spell4EffectsEntry { Id = 4u, SpellId = 80001u, EffectType = SpellEffectType.Proxy, Flags = 2u, TickTime = 5000u, DataBits01 = 83330u },
            new Spell4EffectsEntry { Id = 5u, SpellId = 80002u, EffectType = SpellEffectType.ItemVisualSwap, Flags = 2u },
            new Spell4EffectsEntry { Id = 6u, SpellId = 80003u, EffectType = SpellEffectType.UnitPropertyConversion, Flags = 2u }));

        Assert.Equal(expected, EquipmentSpellRules.GetStancePassive(new ClassEntry { Id = 1u, Spell4IdInnateAbilityPassive = passives }, innateIndex, tables));
        // an item special with the same one-off rows is still not granted: those rows are only accepted for stance passives
        Assert.False(EquipmentSpellRules.IsSupported(80000u, tables));
    }

    private static IItem Item(uint item2Id, params uint[] runeItem2Ids)
    {
        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        IItemInfo info = RecordingDispatchProxy<IItemInfo>.Create(out var infoProxy);
        infoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = item2Id, ItemSpecialId00 = item2Id == GearItem ? GadgetSpecial : 0u });
        itemProxy.SetProperty(nameof(IItem.Info), info);
        itemProxy.SetProperty(nameof(IItem.RuneSlots), (IList<ItemRuneSlot>)runeItem2Ids.Select(id => new ItemRuneSlot(RuneType.Fire, id)).ToList());
        return item;
    }

    private static IGameTableManager Tables()
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.Item), CreateGameTable(
            new Item2Entry { Id = GearItem, ItemSpecialId00 = GadgetSpecial },
            new Item2Entry { Id = RuneA, ItemSpecialId00 = UntouchableSpecial, ItemRuneInstanceId = 1u },
            new Item2Entry { Id = RuneB, ItemRuneInstanceId = 2u },
            new Item2Entry { Id = PlainRune }));
        proxy.SetProperty(nameof(IGameTableManager.ItemSpecial), CreateGameTable(
            new ItemSpecialEntry { Id = GadgetSpecial, Spell4IdOnEquip = GadgetSpell },
            new ItemSpecialEntry { Id = UntouchableSpecial, Spell4IdOnEquip = UntouchableSpell }));
        proxy.SetProperty(nameof(IGameTableManager.ItemRuneInstance), CreateGameTable(
            new ItemRuneInstanceEntry { Id = 1u, ItemSetId = SetId, ItemSetPower = 2u },
            new ItemRuneInstanceEntry { Id = 2u, ItemSetId = SetId, ItemSetPower = 1u }));
        proxy.SetProperty(nameof(IGameTableManager.ItemSet), CreateGameTable(
            new ItemSetEntry { Id = SetId, ItemSetBonusId00 = 1u, ItemSetBonusId01 = 2u, ItemSetBonusId02 = 3u, ItemSetBonusId03 = 4u }));
        proxy.SetProperty(nameof(IGameTableManager.ItemSetBonus), CreateGameTable(
            new ItemSetBonusEntry { Id = 1u, RequiredPower = 3u, Spell4Id = SetBonusSpell },
            new ItemSetBonusEntry { Id = 2u, RequiredPower = 5u, Spell4Id = SetBonusSpellHigh },
            new ItemSetBonusEntry { Id = 3u, RequiredPower = 1u, UnitProperty2Id = 154u, Offset = 0.004f },    // a property bonus, not a spell
            new ItemSetBonusEntry { Id = 4u, RequiredPower = 3u, UnitProperty2Id = 7u, Scalar = 1.003f }));
        proxy.SetProperty(nameof(IGameTableManager.Spell4), CreateGameTable(
            new Spell4Entry { Id = UntouchableSpell },
            new Spell4Entry { Id = GadgetSpell },
            new Spell4Entry { Id = 58678u },
            new Spell4Entry { Id = 70000u },
            new Spell4Entry { Id = 70001u, SpellDuration = 8000u },
            new Spell4Entry { Id = 70002u }));
        proxy.SetProperty(nameof(IGameTableManager.Spell4Effects), CreateGameTable(
            new Spell4EffectsEntry { Id = 1u, SpellId = UntouchableSpell, EffectType = SpellEffectType.Proc, Flags = 2u },
            new Spell4EffectsEntry { Id = 2u, SpellId = UntouchableSpell, EffectType = SpellEffectType.Proxy, Flags = 2u, TickTime = 1000u, DataBits01 = 81881u },
            new Spell4EffectsEntry { Id = 3u, SpellId = GadgetSpell, EffectType = SpellEffectType.Proc, Flags = 2u },
            new Spell4EffectsEntry { Id = 4u, SpellId = 58678u, EffectType = SpellEffectType.UnitPropertyModifier, Flags = 2u },
            new Spell4EffectsEntry { Id = 5u, SpellId = 58678u, EffectType = SpellEffectType.ItemVisualSwap, Flags = 2u },
            new Spell4EffectsEntry { Id = 6u, SpellId = 70000u, EffectType = SpellEffectType.Proxy, TickTime = 1000u },
            new Spell4EffectsEntry { Id = 7u, SpellId = 70001u, EffectType = SpellEffectType.UnitPropertyModifier },
            new Spell4EffectsEntry { Id = 8u, SpellId = 70002u, EffectType = SpellEffectType.Fluff }));
        return tables;
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        typeof(GameTable<T>).GetField($"<{nameof(GameTable<T>.Entries)}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, entries);
        FieldInfo idField = typeof(T).GetField("Id", BindingFlags.Instance | BindingFlags.Public)!;
        uint maxId = entries.Length == 0 ? 0u : entries.Max(e => (uint)idField.GetValue(e)!);
        typeof(GameTable<T>).GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, new GameTableHeader { MaxId = maxId + 1u });
        int[] lookup = Enumerable.Repeat(-1, (int)maxId + 1).ToArray();
        for (int i = 0; i < entries.Length; i++)
            lookup[(uint)idField.GetValue(entries[i])!] = i;
        typeof(GameTable<T>).GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, lookup);
        return table;
    }
}
