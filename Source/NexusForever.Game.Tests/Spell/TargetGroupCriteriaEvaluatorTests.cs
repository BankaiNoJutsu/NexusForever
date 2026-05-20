using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Spell;

public class TargetGroupCriteriaEvaluatorTests
{
    // Builds a TargetGroupEntry with the given type and data values (zero-padded to 7 slots).
    private static TargetGroupEntry MakeEntry(uint type, params uint[] data)
    {
        uint[] entries = new uint[7];
        for (int i = 0; i < data.Length && i < 7; i++)
            entries[i] = data[i];
        return new TargetGroupEntry { Type = type, DataEntries = entries };
    }

    private static IWorldEntity MakeWorldEntity(Faction faction2 = default, uint creatureId = 0u)
    {
        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out var proxy);
        proxy.SetProperty("Faction2", faction2);
        proxy.SetProperty("CreatureId", creatureId);
        return entity;
    }

    private static IPlayer MakePlayer(Race race = default, Class @class = default, Faction faction2 = default)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty("Race", race);
        proxy.SetProperty("Class", @class);
        proxy.SetProperty("Faction2", faction2);
        return player;
    }

    // Builds a GameTable<TargetGroupEntry> populated with the given entries using reflection,
    // bypassing the file-loading constructor so unit tests need no game-table files.
    private static GameTable<TargetGroupEntry> CreateGameTable(params TargetGroupEntry[] entries)
    {
        var table = (GameTable<TargetGroupEntry>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<TargetGroupEntry>));

        typeof(GameTable<TargetGroupEntry>)
            .GetProperty("Entries", BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(table, entries);

        ulong maxId = entries.Length > 0 ? (ulong)(entries.Max(e => e.Id) + 1u) : 1ul;

        FieldInfo headerField = typeof(GameTable<TargetGroupEntry>)
            .GetField("header", BindingFlags.NonPublic | BindingFlags.Instance)!;
        GameTableHeader header = new GameTableHeader { MaxId = maxId };
        headerField.SetValue(table, header);

        int[] lookup = new int[(int)maxId];
        for (int i = 0; i < (int)maxId; i++)
            lookup[i] = -1;
        for (int i = 0; i < entries.Length; i++)
            lookup[(int)entries[i].Id] = i;

        typeof(GameTable<TargetGroupEntry>)
            .GetField("lookup", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(table, lookup);

        return table;
    }

    private static IGameTableManager MakeGameTableManager(params TargetGroupEntry[] subEntries)
    {
        IGameTableManager gtm = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty("TargetGroup", CreateGameTable(subEntries));
        return gtm;
    }

    #region Enum names

    [Fact]
    public void TargetGroupType_EvaluatorMappedPlaceholders_ExposeStableNames()
    {
        Assert.Equal(7, (int)TargetGroupType.PlayerClassIdGroup);
        Assert.Equal(9, (int)TargetGroupType.CreatureIdListGroup);
        Assert.Equal(13, (int)TargetGroupType.NotCreatureRaceIdGroup);
    }

    #endregion

    #region Null entry

    [Fact]
    public void Evaluate_NullEntry_ReturnsTrue()
    {
        IWorldEntity entity = MakeWorldEntity();
        Assert.True(TargetGroupCriteriaEvaluator.Evaluate(null, entity, null));
    }

    #endregion

    #region Types 1 & 2 — FactionGroupId (diagnostic-only, always pass)

    [Theory]
    [InlineData(1u)]
    [InlineData(2u)]
    public void Evaluate_FactionGroupType_AlwaysReturnsTrue(uint type)
    {
        IWorldEntity entity = MakeWorldEntity();
        TargetGroupEntry entry = MakeEntry(type, 1u, 2u, 3u);
        Assert.True(TargetGroupCriteriaEvaluator.Evaluate(entry, entity, null));
    }

    #endregion

    #region Type 3 — Faction2Id must-include

    // 166 = Faction.Dominion (in DataEntries) → pass; 167 = Faction.Exile (not in DataEntries) → fail
    [Theory]
    [InlineData(166u, true)]
    [InlineData(167u, false)]
    public void Evaluate_Type3_Faction2MustInclude_MatchDeterminesResult(uint faction2Id, bool expected)
    {
        IWorldEntity entity = MakeWorldEntity((Faction)faction2Id);
        TargetGroupEntry entry = MakeEntry(3u, 166u); // include Dominion only
        Assert.Equal(expected, TargetGroupCriteriaEvaluator.Evaluate(entry, entity, null));
    }

    [Theory]
    [InlineData(166u)] // Faction.Dominion
    [InlineData(167u)] // Faction.Exile
    public void Evaluate_Type3_MultiValueDataEntries_AnyMatchPasses(uint faction2Id)
    {
        IWorldEntity entity = MakeWorldEntity((Faction)faction2Id);
        TargetGroupEntry entry = MakeEntry(3u, 166u, 167u); // Dominion or Exile
        Assert.True(TargetGroupCriteriaEvaluator.Evaluate(entry, entity, null));
    }

    #endregion

    #region Type 4 — Faction2Id must-exclude

    // 166 = Dominion (in DataEntries) → rejected; 167 = Exile (not in DataEntries) → pass
    [Theory]
    [InlineData(166u, false)]
    [InlineData(167u, true)]
    public void Evaluate_Type4_Faction2MustExclude_MatchRejects(uint faction2Id, bool expected)
    {
        IWorldEntity entity = MakeWorldEntity((Faction)faction2Id);
        TargetGroupEntry entry = MakeEntry(4u, 166u); // exclude Dominion
        Assert.Equal(expected, TargetGroupCriteriaEvaluator.Evaluate(entry, entity, null));
    }

    #endregion

    #region Type 5 — RaceId must-include

    // Race.Human = 1 (in DataEntries) → pass; Race.Granok = 3 (not in DataEntries) → fail
    [Theory]
    [InlineData((byte)Race.Human, true)]
    [InlineData((byte)Race.Granok, false)]
    public void Evaluate_Type5_Player_RaceMustInclude_MatchDeterminesResult(byte raceId, bool expected)
    {
        IPlayer player = MakePlayer((Race)raceId);
        TargetGroupEntry entry = MakeEntry(5u, (uint)Race.Human);
        Assert.Equal(expected, TargetGroupCriteriaEvaluator.Evaluate(entry, player, null));
    }

    // Non-player entity → raceId = 0; ContainsValue treats 0 as terminator so 0 never matches.
    [Fact]
    public void Evaluate_Type5_NonPlayer_RaceZero_NeverMatchesNonZeroEntries_ReturnsFalse()
    {
        IWorldEntity entity = MakeWorldEntity();
        TargetGroupEntry entry = MakeEntry(5u, (uint)Race.Human);
        Assert.False(TargetGroupCriteriaEvaluator.Evaluate(entry, entity, null));
    }

    [Theory]
    [InlineData((byte)Race.Human)]
    [InlineData((byte)Race.Aurin)]
    public void Evaluate_Type5_MultiValueDataEntries_AnyMatchPasses(byte raceId)
    {
        IPlayer player = MakePlayer((Race)raceId);
        TargetGroupEntry entry = MakeEntry(5u, (uint)Race.Human, (uint)Race.Aurin);
        Assert.True(TargetGroupCriteriaEvaluator.Evaluate(entry, player, null));
    }

    #endregion

    #region Type 6 — RaceId must-exclude

    // Race.Human = 1 (in DataEntries) → rejected; Race.Granok = 3 (not in DataEntries) → pass
    [Theory]
    [InlineData((byte)Race.Human, false)]
    [InlineData((byte)Race.Granok, true)]
    public void Evaluate_Type6_Player_RaceMustExclude_MatchRejects(byte raceId, bool expected)
    {
        IPlayer player = MakePlayer((Race)raceId);
        TargetGroupEntry entry = MakeEntry(6u, (uint)Race.Human);
        Assert.Equal(expected, TargetGroupCriteriaEvaluator.Evaluate(entry, player, null));
    }

    // Non-player → raceId = 0 → ContainsValue([Human, 0, ...], 0) = false → !false = true
    [Fact]
    public void Evaluate_Type6_NonPlayer_RaceZero_NotExcluded_ReturnsTrue()
    {
        IWorldEntity entity = MakeWorldEntity();
        TargetGroupEntry entry = MakeEntry(6u, (uint)Race.Human);
        Assert.True(TargetGroupCriteriaEvaluator.Evaluate(entry, entity, null));
    }

    #endregion

    #region Type 7 — ClassId must-include

    // Class.Warrior = 1 (in DataEntries) → pass; Class.Esper = 3 (not in DataEntries) → fail
    [Theory]
    [InlineData((byte)Class.Warrior, true)]
    [InlineData((byte)Class.Esper, false)]
    public void Evaluate_Type7_Player_ClassMustInclude_MatchDeterminesResult(byte classId, bool expected)
    {
        IPlayer player = MakePlayer(@class: (Class)classId);
        TargetGroupEntry entry = MakeEntry(7u, (uint)Class.Warrior);
        Assert.Equal(expected, TargetGroupCriteriaEvaluator.Evaluate(entry, player, null));
    }

    [Theory]
    [InlineData((byte)Class.Warrior)]
    [InlineData((byte)Class.Medic)]
    public void Evaluate_Type7_MultiValueDataEntries_AnyMatchPasses(byte classId)
    {
        IPlayer player = MakePlayer(@class: (Class)classId);
        TargetGroupEntry entry = MakeEntry(7u, (uint)Class.Warrior, (uint)Class.Medic);
        Assert.True(TargetGroupCriteriaEvaluator.Evaluate(entry, player, null));
    }

    #endregion

    #region Type 8 — ClassId must-exclude

    // Class.Warrior = 1 (in DataEntries) → rejected; Class.Esper = 3 (not in DataEntries) → pass
    [Theory]
    [InlineData((byte)Class.Warrior, false)]
    [InlineData((byte)Class.Esper, true)]
    public void Evaluate_Type8_Player_ClassMustExclude_MatchRejects(byte classId, bool expected)
    {
        IPlayer player = MakePlayer(@class: (Class)classId);
        TargetGroupEntry entry = MakeEntry(8u, (uint)Class.Warrior);
        Assert.Equal(expected, TargetGroupCriteriaEvaluator.Evaluate(entry, player, null));
    }

    #endregion

    #region Type 9 — Creature2Id list-match

    [Theory]
    [InlineData(100u, true)]
    [InlineData(200u, false)]
    public void Evaluate_Type9_CreatureIdListMatch_MatchDeterminesResult(uint creatureId, bool expected)
    {
        IWorldEntity entity = MakeWorldEntity(creatureId: creatureId);
        TargetGroupEntry entry = MakeEntry(9u, 100u);
        Assert.Equal(expected, TargetGroupCriteriaEvaluator.Evaluate(entry, entity, null));
    }

    [Theory]
    [InlineData(100u)]
    [InlineData(200u)]
    public void Evaluate_Type9_MultiValueDataEntries_AnyMatchPasses(uint creatureId)
    {
        IWorldEntity entity = MakeWorldEntity(creatureId: creatureId);
        TargetGroupEntry entry = MakeEntry(9u, 100u, 200u);
        Assert.True(TargetGroupCriteriaEvaluator.Evaluate(entry, entity, null));
    }

    #endregion

    #region Type 10 — recursive AND (all sub-groups must pass)

    [Fact]
    public void Evaluate_Type10_AllSubGroupsPass_ReturnsTrue()
    {
        // Sub-entry 1 (id=1): type 3, include Dominion
        // Sub-entry 2 (id=2): type 3, include Dominion
        // Entity: Faction2 = Dominion → both pass → overall true
        IWorldEntity entity = MakeWorldEntity(Faction.Dominion);
        TargetGroupEntry sub1 = new TargetGroupEntry { Id = 1u, Type = 3u, DataEntries = new uint[] { 166u, 0u, 0u, 0u, 0u, 0u, 0u } };
        TargetGroupEntry sub2 = new TargetGroupEntry { Id = 2u, Type = 3u, DataEntries = new uint[] { 166u, 0u, 0u, 0u, 0u, 0u, 0u } };
        IGameTableManager gtm = MakeGameTableManager(sub1, sub2);
        TargetGroupEntry entry = MakeEntry(10u, 1u, 2u);
        Assert.True(TargetGroupCriteriaEvaluator.Evaluate(entry, entity, gtm));
    }

    [Fact]
    public void Evaluate_Type10_OneSubGroupFails_ReturnsFalse()
    {
        // Sub-entry 1 (id=1): type 3, include Dominion → Exile entity fails
        // Sub-entry 2 (id=2): type 3, include Exile   → Exile entity passes
        // Entity: Faction2 = Exile → sub1 fails → overall false
        IWorldEntity entity = MakeWorldEntity(Faction.Exile);
        TargetGroupEntry sub1 = new TargetGroupEntry { Id = 1u, Type = 3u, DataEntries = new uint[] { 166u, 0u, 0u, 0u, 0u, 0u, 0u } }; // Dominion only
        TargetGroupEntry sub2 = new TargetGroupEntry { Id = 2u, Type = 3u, DataEntries = new uint[] { 167u, 0u, 0u, 0u, 0u, 0u, 0u } }; // Exile only
        IGameTableManager gtm = MakeGameTableManager(sub1, sub2);
        TargetGroupEntry entry = MakeEntry(10u, 1u, 2u);
        Assert.False(TargetGroupCriteriaEvaluator.Evaluate(entry, entity, gtm));
    }

    #endregion

    #region Type 11 — recursive NOT (all sub-groups must fail → entity passes)

    [Fact]
    public void Evaluate_Type11_AllSubGroupsFail_ReturnsTrue()
    {
        // Sub-entry (id=1): type 3, include Dominion → Exile entity fails the sub-group
        // Entity: Faction2 = Exile → sub-group fails → type 11 passes (all must fail)
        IWorldEntity entity = MakeWorldEntity(Faction.Exile);
        TargetGroupEntry sub1 = new TargetGroupEntry { Id = 1u, Type = 3u, DataEntries = new uint[] { 166u, 0u, 0u, 0u, 0u, 0u, 0u } };
        IGameTableManager gtm = MakeGameTableManager(sub1);
        TargetGroupEntry entry = MakeEntry(11u, 1u);
        Assert.True(TargetGroupCriteriaEvaluator.Evaluate(entry, entity, gtm));
    }

    [Fact]
    public void Evaluate_Type11_OneSubGroupPasses_ReturnsFalse()
    {
        // Sub-entry (id=1): type 3, include Dominion → Dominion entity passes the sub-group
        // Entity: Faction2 = Dominion → sub-group passes → type 11 fails (at least one passed)
        IWorldEntity entity = MakeWorldEntity(Faction.Dominion);
        TargetGroupEntry sub1 = new TargetGroupEntry { Id = 1u, Type = 3u, DataEntries = new uint[] { 166u, 0u, 0u, 0u, 0u, 0u, 0u } };
        IGameTableManager gtm = MakeGameTableManager(sub1);
        TargetGroupEntry entry = MakeEntry(11u, 1u);
        Assert.False(TargetGroupCriteriaEvaluator.Evaluate(entry, entity, gtm));
    }

    #endregion

    #region Type 12 — UnitRaceId must-include

    [Theory]
    [InlineData(5u, true)]
    [InlineData(9u, false)]
    public void Evaluate_Type12_UnitRaceIdMustInclude_MatchDeterminesResult(uint unitRaceId, bool expected)
    {
        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out var proxy);
        proxy.SetProperty("CreatureEntry", new Creature2Entry { UnitRaceId = unitRaceId });
        TargetGroupEntry entry = MakeEntry(12u, 5u);
        Assert.Equal(expected, TargetGroupCriteriaEvaluator.Evaluate(entry, entity, null));
    }

    [Fact]
    public void Evaluate_Type12_NullCreatureEntry_ReturnsFalse()
    {
        // CreatureEntry is null → UnitRaceId defaults to 0 → 0 is terminator so never matches
        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out _);
        TargetGroupEntry entry = MakeEntry(12u, 5u);
        Assert.False(TargetGroupCriteriaEvaluator.Evaluate(entry, entity, null));
    }

    [Theory]
    [InlineData(5u)]
    [InlineData(7u)]
    public void Evaluate_Type12_MultiValueDataEntries_AnyMatchPasses(uint unitRaceId)
    {
        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out var proxy);
        proxy.SetProperty("CreatureEntry", new Creature2Entry { UnitRaceId = unitRaceId });
        TargetGroupEntry entry = MakeEntry(12u, 5u, 7u);
        Assert.True(TargetGroupCriteriaEvaluator.Evaluate(entry, entity, null));
    }

    #endregion

    #region Type 13 — UnitRaceId must-exclude

    [Theory]
    [InlineData(5u, false)]
    [InlineData(9u, true)]
    public void Evaluate_Type13_UnitRaceIdMustExclude_MatchRejects(uint unitRaceId, bool expected)
    {
        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out var proxy);
        proxy.SetProperty("CreatureEntry", new Creature2Entry { UnitRaceId = unitRaceId });
        TargetGroupEntry entry = MakeEntry(13u, 5u);
        Assert.Equal(expected, TargetGroupCriteriaEvaluator.Evaluate(entry, entity, null));
    }

    #endregion

    #region Depth cap

    [Fact]
    public void Evaluate_DepthExceedsMaxRecursion_ReturnsTrue()
    {
        // depth = 6 > MaxRecursionDepth(5) → returns true immediately without evaluating
        IWorldEntity entity = MakeWorldEntity();
        TargetGroupEntry entry = MakeEntry(10u, 99u);
        Assert.True(TargetGroupCriteriaEvaluator.Evaluate(entry, entity, null, depth: 6));
    }

    #endregion

    #region Default / unknown type

    [Fact]
    public void Evaluate_UnknownType_ReturnsTrue()
    {
        IWorldEntity entity = MakeWorldEntity();
        TargetGroupEntry entry = MakeEntry(99u, 1u);
        Assert.True(TargetGroupCriteriaEvaluator.Evaluate(entry, entity, null));
    }

    #endregion
}
