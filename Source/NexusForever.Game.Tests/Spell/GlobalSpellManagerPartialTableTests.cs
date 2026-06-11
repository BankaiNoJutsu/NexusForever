using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Spell;

public class GlobalSpellManagerPartialTableTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CacheSpellEntries_WithMissingPrimaryTablesUsesEmptyCaches(bool includeEmptyTables)
    {
        var manager = CreateManager(gameTableManager =>
        {
            if (!includeEmptyTables)
                return;

            SetTable(gameTableManager, nameof(GameTableManager.Spell4), CreateGameTable<Spell4Entry>());
            SetTable(gameTableManager, nameof(GameTableManager.Spell4Effects), CreateGameTable<Spell4EffectsEntry>());
            SetTable(gameTableManager, nameof(GameTableManager.Spell4Telegraph), CreateGameTable<Spell4TelegraphEntry>());
            SetTable(gameTableManager, nameof(GameTableManager.TelegraphDamage), CreateGameTable<TelegraphDamageEntry>());
            SetTable(gameTableManager, nameof(GameTableManager.Spell4Thresholds), CreateGameTable<Spell4ThresholdsEntry>());
        });

        Invoke(manager, "CacheSpellEntries");

        Assert.Empty(manager.GetSpell4Entries(55u));
        Assert.Empty(manager.GetSpell4EffectEntries(101u));
        Assert.Empty(manager.GetTelegraphDamageEntries(101u));
        Assert.Empty(manager.GetSpell4ThresholdEntries(101u));
    }

    [Fact]
    public void CacheSpellEntries_WithTableBackedRowsPreservesOrderingAndLookups()
    {
        var manager = CreateManager(gameTableManager =>
        {
            SetTable(gameTableManager, nameof(GameTableManager.Spell4), CreateGameTable(
                new Spell4Entry { Id = 101u, Spell4BaseIdBaseSpell = 55u, TierIndex = 1u },
                new Spell4Entry { Id = 102u, Spell4BaseIdBaseSpell = 55u, TierIndex = 3u },
                new Spell4Entry { Id = 103u, Spell4BaseIdBaseSpell = 56u, TierIndex = 2u }));
            SetTable(gameTableManager, nameof(GameTableManager.Spell4Effects), CreateGameTable(
                new Spell4EffectsEntry { Id = 201u, SpellId = 101u, OrderIndex = 2u },
                new Spell4EffectsEntry { Id = 202u, SpellId = 101u, OrderIndex = 1u },
                new Spell4EffectsEntry { Id = 203u, SpellId = 102u, OrderIndex = 1u }));
            SetTable(gameTableManager, nameof(GameTableManager.Spell4Telegraph), CreateGameTable(
                new Spell4TelegraphEntry { Id = 301u, Spell4Id = 101u, TelegraphDamageId = 401u }));
            SetTable(gameTableManager, nameof(GameTableManager.TelegraphDamage), CreateGameTable(
                new TelegraphDamageEntry { Id = 401u, DisplayGroup = 7u }));
            SetTable(gameTableManager, nameof(GameTableManager.Spell4Thresholds), CreateGameTable(
                new Spell4ThresholdsEntry { Id = 501u, Spell4IdParent = 101u, OrderIndex = 2u },
                new Spell4ThresholdsEntry { Id = 502u, Spell4IdParent = 101u, OrderIndex = 1u }));
        });

        Invoke(manager, "CacheSpellEntries");

        Assert.Equal([102u, 101u], manager.GetSpell4Entries(55u).Select(e => e.Id));
        Assert.Equal([202u, 201u], manager.GetSpell4EffectEntries(101u).Select(e => e.Id));
        Assert.Equal([401u], manager.GetTelegraphDamageEntries(101u).Select(e => e.Id));
        Assert.Equal([502u, 501u], manager.GetSpell4ThresholdEntries(101u).Select(e => e.Id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InitialiseSpellInfo_WithMissingSpell4BaseTableUsesEmptyStore(bool includeEmptyTable)
    {
        var manager = CreateManager(gameTableManager =>
        {
            if (includeEmptyTable)
                SetTable(gameTableManager, nameof(GameTableManager.Spell4Base), CreateGameTable<Spell4BaseEntry>());
        });

        Invoke(manager, "InitialiseSpellInfo");

        Assert.Throws<ArgumentOutOfRangeException>(() => manager.GetSpellBaseInfo(77u));
    }

    [Fact]
    public void GetSpellBaseInfo_WithMissingDependencyTablesMaterialisesNullableSpellInfo()
    {
        var manager = CreateManager(gameTableManager =>
        {
            SetTable(gameTableManager, nameof(GameTableManager.Spell4Base), CreateGameTable(new Spell4BaseEntry
            {
                Id                                = 77u,
                Spell4HitResultId                = 1u,
                Spell4TargetMechanicId           = 2u,
                Spell4TargetAngleId              = 3u,
                Spell4PrerequisiteId             = 4u,
                Spell4ValidTargetId              = 5u,
                TargetGroupIdCastGroup           = 6u,
                Creature2IdPositionalAoe         = 7u,
                TargetGroupIdAoeGroup            = 8u,
                Spell4BaseIdPrerequisiteSpell    = 9u,
                Spell4SpellTypesIdSpellType      = 10u
            }));
            SetTable(gameTableManager, nameof(GameTableManager.Spell4), CreateGameTable(new Spell4Entry
            {
                Id                                      = 101u,
                Spell4BaseIdBaseSpell                  = 77u,
                TierIndex                              = 1u,
                Spell4AoeTargetConstraintsId           = 11u,
                Spell4ConditionsIdCaster               = 12u,
                Spell4ConditionsIdTarget               = 13u,
                Spell4CCConditionsIdCaster             = 14u,
                Spell4CCConditionsIdTarget             = 15u,
                SpellCoolDownIdGlobal                  = 16u,
                Spell4StackGroupId                     = 17u,
                PrerequisiteIdCasterCast               = 18u,
                PrerequisiteIdTargetCast               = 19u,
                PrerequisiteIdCasterPersistence        = 20u,
                PrerequisiteIdTargetPersistence        = 21u,
                PrerequisiteIdRunners                  = [22u, 0u]
            }));
        });

        Invoke(manager, "CacheSpellEntries");

        var baseInfo = manager.GetSpellBaseInfo(77u);
        var spellInfo = baseInfo.GetSpellInfo(1);

        Assert.Null(baseInfo.HitResult);
        Assert.Null(baseInfo.TargetMechanics);
        Assert.Null(baseInfo.TargetAngle);
        Assert.Null(baseInfo.Prerequisites);
        Assert.Equal(0u, (uint)baseInfo.PrerequisiteFlags);
        Assert.Null(baseInfo.ValidTargets);
        Assert.Null(baseInfo.CastGroup);
        Assert.Null(baseInfo.PositionalAoe);
        Assert.Null(baseInfo.AoeGroup);
        Assert.Null(baseInfo.PrerequisiteSpell);
        Assert.Null(baseInfo.SpellType);

        Assert.Null(spellInfo.ServiceTokenCostEntry);
        Assert.Null(spellInfo.AoeTargetConstraints);
        Assert.Null(spellInfo.CasterConditions);
        Assert.Null(spellInfo.TargetConditions);
        Assert.Null(spellInfo.CasterCCConditions);
        Assert.Null(spellInfo.TargetCCConditions);
        Assert.Null(spellInfo.GlobalCooldown);
        Assert.Null(spellInfo.StackGroup);
        Assert.Null(spellInfo.CasterCastPrerequisite);
        Assert.Null(spellInfo.TargetCastPrerequisites);
        Assert.Null(spellInfo.CasterPersistencePrerequisites);
        Assert.Null(spellInfo.TargetPersistencePrerequisites);
        Assert.Empty(spellInfo.Telegraphs);
        Assert.Empty(spellInfo.Effects);
        Assert.Empty(spellInfo.Thresholds);
        Assert.Collection(spellInfo.PrerequisiteRunners, Assert.Null);
    }

    [Fact]
    public void GetSpellBaseInfo_WithMissingSpell4TableReturnsNullSpellTier()
    {
        var manager = CreateManager(gameTableManager =>
        {
            SetTable(gameTableManager, nameof(GameTableManager.Spell4Base), CreateGameTable(new Spell4BaseEntry
            {
                Id = 77u
            }));
        });

        Invoke(manager, "CacheSpellEntries");

        var baseInfo = manager.GetSpellBaseInfo(77u);

        Assert.Null(baseInfo.GetSpellInfo(1));
    }

    [Fact]
    public void GetSpellInfo_WithSparseOrOutOfRangeTierReturnsNull()
    {
        var manager = CreateManager(gameTableManager =>
        {
            SetTable(gameTableManager, nameof(GameTableManager.Spell4Base), CreateGameTable(new Spell4BaseEntry
            {
                Id = 77u
            }));
            SetTable(gameTableManager, nameof(GameTableManager.Spell4), CreateGameTable(new Spell4Entry
            {
                Id                     = 101u,
                Spell4BaseIdBaseSpell = 77u,
                TierIndex             = 2u
            }));
        });

        Invoke(manager, "CacheSpellEntries");

        var baseInfo = manager.GetSpellBaseInfo(77u);

        Assert.Null(baseInfo.GetSpellInfo(1));
        Assert.NotNull(baseInfo.GetSpellInfo(2));
        Assert.Null(baseInfo.GetSpellInfo(3));
    }

    private static NexusForever.Game.Spell.GlobalSpellManager CreateManager(Action<GameTableManager> configure)
    {
        var gameTableManager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        configure(gameTableManager);

        return new NexusForever.Game.Spell.GlobalSpellManager(gameTableManager);
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0ul : entries.Max(GetEntryId) + 1ul
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static int[] BuildLookup<T>(T[] entries) where T : class, new()
    {
        if (entries.Length == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1ul)).ToArray();
        for (int i = 0; i < entries.Length; i++)
            lookup[(int)GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static ulong GetEntryId<T>(T entry)
    {
        FieldInfo idField = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0];
        return Convert.ToUInt64(idField.GetValue(entry));
    }

    private static void Invoke(NexusForever.Game.Spell.GlobalSpellManager manager, string methodName)
    {
        typeof(NexusForever.Game.Spell.GlobalSpellManager)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(manager, null);
    }

    private static void SetTable<T>(GameTableManager gameTableManager, string propertyName, GameTable<T> table) where T : class, new()
    {
        SetAutoProperty(gameTableManager, propertyName, table);
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
}
