using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Entity;

namespace NexusForever.Game.Tests.Entity;

public class UnitEntityStackGroupCapTests
{
    private const uint Group = 7u;

    [Fact]
    public void StackGroupCap_WithoutSpell4Table_DoesNotThrowAndKeepsModifiers()
    {
        var unit = CreateUnit(RecordingDispatchProxy<IGameTableManager>.Create(out _), out var modifier);
        unit.AddSpellModifierProperty(modifier, 1u, 10u, 1u, 100u);

        unit.EnforceSpellPropertyStackGroupCap(Group, 1u);

        Assert.True(unit.HasSpellModifierProperty(Property.BaseHealth, 1u));
    }

    [Fact]
    public void StackGroupCap_AtCap_EvictsOnlyTheOldestCasting()
    {
        var tables = CreateTables(new Spell4Entry { Id = 10u, Spell4StackGroupId = Group },
            new Spell4Entry { Id = 11u, Spell4StackGroupId = Group },
            new Spell4Entry { Id = 12u, Spell4StackGroupId = Group + 1u });
        var unit = CreateUnit(tables, out var first);
        unit.AddSpellModifierProperty(first, 1u, 10u, 1u, 100u);
        unit.AddSpellModifierProperty(CreateModifier(), 2u, 11u, 2u, 200u);
        unit.AddSpellModifierProperty(CreateModifier(), 3u, 12u, 3u, 50u); // other group, older casting id

        unit.EnforceSpellPropertyStackGroupCap(Group, 2u);

        Assert.False(unit.HasSpellModifierProperty(Property.BaseHealth, 1u));
        Assert.True(unit.HasSpellModifierProperty(Property.BaseHealth, 2u));
        Assert.True(unit.HasSpellModifierProperty(Property.BaseHealth, 3u));
    }

    [Fact]
    public void StackGroupCap_BelowCap_EvictsNothing()
    {
        var tables = CreateTables(new Spell4Entry { Id = 10u, Spell4StackGroupId = Group });
        var unit = CreateUnit(tables, out var modifier);
        unit.AddSpellModifierProperty(modifier, 1u, 10u, 1u, 100u);

        unit.EnforceSpellPropertyStackGroupCap(Group, 2u);

        Assert.True(unit.HasSpellModifierProperty(Property.BaseHealth, 1u));
    }

    private static TestUnit CreateUnit(IGameTableManager tables, out ISpellPropertyModifier modifier)
    {
        var unit = new TestUnit();
        unit.InitialiseRuntimeDependencies(null, null, null, () => tables);
        modifier = CreateModifier();
        return unit;
    }

    private static ISpellPropertyModifier CreateModifier()
    {
        var modifier = RecordingDispatchProxy<ISpellPropertyModifier>.Create(out var proxy);
        proxy.SetProperty(nameof(ISpellPropertyModifier.Property), Property.BaseHealth);
        proxy.SetProperty(nameof(ISpellPropertyModifier.Alterations), new List<IPropertyModifier>());
        proxy.SetProperty(nameof(ISpellPropertyModifier.StackCount), 1u);
        return modifier;
    }

    private static IGameTableManager CreateTables(params Spell4Entry[] spells)
    {
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        var table = (GameTable<Spell4Entry>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<Spell4Entry>));
        typeof(GameTable<Spell4Entry>).GetProperty(nameof(GameTable<Spell4Entry>.Entries))!.GetSetMethod(true)!.Invoke(table, [spells]);
        typeof(GameTable<Spell4Entry>).GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(table, new GameTableHeader { MaxId = spells.Max(entry => entry.Id) + 1u });
        int[] lookup = Enumerable.Repeat(-1, (int)(spells.Max(entry => entry.Id) + 1u)).ToArray();
        for (int i = 0; i < spells.Length; i++) lookup[spells[i].Id] = i;
        typeof(GameTable<Spell4Entry>).GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, lookup);
        proxy.SetProperty(nameof(IGameTableManager.Spell4), table);
        return tables;
    }

    private sealed class TestUnit : UnitEntity
    {
        public override EntityType Type => EntityType.Simple;
        public override uint Health { get; protected set; }

        protected override IEntityModel BuildEntityModel() => null;

        protected override float CalculateDefaultProperty(Property property) => 0f;

        public TestUnit() : base(RecordingDispatchProxy<IMovementManager>.Create(out _))
        {
            var stats = typeof(WorldEntity).GetField("stats", BindingFlags.Instance | BindingFlags.NonPublic)!;
            stats.SetValue(this, Activator.CreateInstance(stats.FieldType));
        }

        public bool HasSpellModifierProperty(Property property, uint effectId)
        {
            var field = typeof(UnitEntity).GetField("spellProperties", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var dictionary = (System.Collections.IDictionary)field.GetValue(this)!;
            return dictionary.Contains(property) && ((System.Collections.IDictionary)dictionary[property]!).Contains(effectId);
        }
    }
}
