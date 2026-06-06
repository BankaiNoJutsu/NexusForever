using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;

if (args.Length > 0 && args[0].Equals("--riders-reef-crosswalk", StringComparison.OrdinalIgnoreCase))
    return await RunRidersReefCrosswalk(args.Skip(1).ToArray());

if (args.Length > 0 && args[0].Equals("--target-groups", StringComparison.OrdinalIgnoreCase))
    return await RunTargetGroups(args.Skip(1).ToArray());

if (args.Length > 0 && args[0].Equals("--spell-action-set", StringComparison.OrdinalIgnoreCase))
    return RunSpellActionSet(args.Skip(1).ToArray());

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  QuestTableInspector <tbl-path> [quest-id ...]");
    Console.Error.WriteLine("  QuestTableInspector --target-groups <tbl-path> <target-group-id ...>");
    Console.Error.WriteLine("  QuestTableInspector --spell-action-set <tbl-path> [spell-id ...] [--xp <total-xp>] [--shortcut-set-id <id>]");
    Console.Error.WriteLine("  QuestTableInspector --riders-reef-crosswalk <world-sql-path> [--out <artifact-path>]");
    return 1;
}

string gameTablePath = args[0];
ulong[] questIds = args.Length > 1
    ? args.Skip(1).Select(ulong.Parse).ToArray()
    : [10513UL, 10521UL, 10527UL, 10532UL];

var options = Options.Create(new GameTableConfig
{
    GameTablePath = gameTablePath
});

var gameTableManager = new GameTableManager(options);
await gameTableManager.Initialise();

foreach (ulong questId in questIds)
{
    Quest2Entry? quest = gameTableManager.Quest2.GetEntry(questId);
    if (quest is null)
    {
        Console.WriteLine($"QUEST {questId} missing");
        continue;
    }

    Console.WriteLine(
        $"QUEST {quest.Id} zone={quest.WorldZoneId} flags={quest.Flags} objectives=[{string.Join(",", quest.Objectives)}] " +
        $"receiverWl={quest.WorldLocation2IdReceiver} altReceiverWl=[{quest.WorldLocation2IdAltReceiver00},{quest.WorldLocation2IdAltReceiver01},{quest.WorldLocation2IdAltReceiver02}] " +
        $"altReceiverDir=[{quest.QuestDirectionIdAltReceiver00},{quest.QuestDirectionIdAltReceiver01},{quest.QuestDirectionIdAltReceiver02}] completionDir={quest.QuestDirectionIdCompletion}");

    HashSet<uint> referencedWorldLocations = [];

    AddWorldLocation(quest.WorldLocation2IdReceiver, referencedWorldLocations);
    AddWorldLocation(quest.WorldLocation2IdAltReceiver00, referencedWorldLocations);
    AddWorldLocation(quest.WorldLocation2IdAltReceiver01, referencedWorldLocations);
    AddWorldLocation(quest.WorldLocation2IdAltReceiver02, referencedWorldLocations);

    PrintDirection("QUEST-COMPLETION", quest.QuestDirectionIdCompletion, gameTableManager, referencedWorldLocations);
    PrintDirection("QUEST-ALT-RECEIVER00", quest.QuestDirectionIdAltReceiver00, gameTableManager, referencedWorldLocations);
    PrintDirection("QUEST-ALT-RECEIVER01", quest.QuestDirectionIdAltReceiver01, gameTableManager, referencedWorldLocations);
    PrintDirection("QUEST-ALT-RECEIVER02", quest.QuestDirectionIdAltReceiver02, gameTableManager, referencedWorldLocations);

    foreach (uint objectiveId in quest.Objectives.Where(id => id != 0))
    {
        QuestObjectiveEntry? objective = gameTableManager.QuestObjective.GetEntry(objectiveId);
        if (objective is null)
        {
            Console.WriteLine($"  OBJ {objectiveId} missing");
            continue;
        }

        int objectiveTypeValue = unchecked((int)objective.Type);
        string objectiveType = Enum.IsDefined(typeof(QuestObjectiveType), objectiveTypeValue)
            ? ((QuestObjectiveType)objectiveTypeValue).ToString()
            : $"Unknown({objective.Type})";

        Console.WriteLine(
            $"  OBJ {objective.Id} type={objectiveType}({objective.Type}) flags={objective.Flags} data={objective.Data} count={objective.Count} " +
            $"wl=[{objective.WorldLocationsIdIndicator00},{objective.WorldLocationsIdIndicator01},{objective.WorldLocationsIdIndicator02},{objective.WorldLocationsIdIndicator03}] " +
            $"targetGroup={objective.TargetGroupIdRewardPane} questDir={objective.QuestDirectionId}");

        PrintDirection($"OBJ-QUEST-DIR {objective.Id}", objective.QuestDirectionId, gameTableManager, referencedWorldLocations);

        if (objective.Type == (uint)QuestObjectiveType.EnterArea)
        {
            if (gameTableManager.QuestDirectionEntry is not null)
            {
                QuestDirectionEntryEntry? directionEntry = gameTableManager.QuestDirectionEntry.GetEntry(objective.Data);
                if (directionEntry is not null)
                {
                    PrintDirectionEntry(directionEntry, referencedWorldLocations);
                }
            }

            if (gameTableManager.QuestDirection is not null)
            {
                QuestDirectionEntry? direction = gameTableManager.QuestDirection.GetEntry(objective.Data);
                if (direction is not null)
                {
                    Console.WriteLine($"    DIR {direction.Id} flags={direction.QuestDirectionFlags} entries=[{string.Join(",", GetDirectionEntryIds(direction))}] excludedZone={direction.WorldZoneIdExcludedZone}");

                    if (gameTableManager.QuestDirectionEntry is not null)
                    {
                        foreach (uint directionEntryId in GetDirectionEntryIds(direction))
                        {
                            QuestDirectionEntryEntry? nestedDirectionEntry = gameTableManager.QuestDirectionEntry.GetEntry(directionEntryId);
                            if (nestedDirectionEntry is not null)
                                PrintDirectionEntry(nestedDirectionEntry, referencedWorldLocations);
                        }
                    }
                }
            }
        }

        AddWorldLocation(objective.WorldLocationsIdIndicator00, referencedWorldLocations);
        AddWorldLocation(objective.WorldLocationsIdIndicator01, referencedWorldLocations);
        AddWorldLocation(objective.WorldLocationsIdIndicator02, referencedWorldLocations);
        AddWorldLocation(objective.WorldLocationsIdIndicator03, referencedWorldLocations);
    }

    foreach (uint worldLocationId in referencedWorldLocations.OrderBy(id => id))
    {
        WorldLocation2Entry? worldLocation = gameTableManager.WorldLocation2.GetEntry(worldLocationId);
        if (worldLocation is null)
        {
            Console.WriteLine($"  WL {worldLocationId} missing");
            continue;
        }

        Console.WriteLine(
            $"  WL {worldLocation.Id} world={worldLocation.WorldId} zone={worldLocation.WorldZoneId} pos=({worldLocation.Position0}, {worldLocation.Position1}, {worldLocation.Position2}) " +
            $"radius={worldLocation.Radius} maxVertical={worldLocation.MaxVerticalDistance} phases={worldLocation.Phases}");
    }
}

return 0;

static void PrintDirection(string label, uint directionId, GameTableManager gameTableManager, ISet<uint> referencedWorldLocations)
{
    if (directionId == 0 || gameTableManager.QuestDirection is null)
        return;

    QuestDirectionEntry? direction = gameTableManager.QuestDirection.GetEntry(directionId);
    if (direction is null)
    {
        Console.WriteLine($"    {label} DIR {directionId} missing");
        return;
    }

    Console.WriteLine(
        $"    {label} DIR {direction.Id} flags={direction.QuestDirectionFlags} " +
        $"entries=[{string.Join(",", GetDirectionEntryIds(direction))}] excludedZone={direction.WorldZoneIdExcludedZone}");

    if (gameTableManager.QuestDirectionEntry is null)
        return;

    foreach (uint directionEntryId in GetDirectionEntryIds(direction))
    {
        QuestDirectionEntryEntry? directionEntry = gameTableManager.QuestDirectionEntry.GetEntry(directionEntryId);
        if (directionEntry is not null)
            PrintDirectionEntry(directionEntry, referencedWorldLocations);
    }
}

static void AddWorldLocation(uint worldLocationId, ISet<uint> referencedWorldLocations)
{
    if (worldLocationId != 0)
        referencedWorldLocations.Add(worldLocationId);
}

static IEnumerable<uint> GetDirectionEntryIds(QuestDirectionEntry direction)
{
    uint[] directionEntryIds =
    [
        direction.QuestDirectionEntryId00,
        direction.QuestDirectionEntryId01,
        direction.QuestDirectionEntryId02,
        direction.QuestDirectionEntryId03,
        direction.QuestDirectionEntryId04,
        direction.QuestDirectionEntryId05,
        direction.QuestDirectionEntryId06,
        direction.QuestDirectionEntryId07,
        direction.QuestDirectionEntryId08,
        direction.QuestDirectionEntryId09,
        direction.QuestDirectionEntryId10,
        direction.QuestDirectionEntryId11,
        direction.QuestDirectionEntryId12,
        direction.QuestDirectionEntryId13,
        direction.QuestDirectionEntryId14,
        direction.QuestDirectionEntryId15
    ];

    return directionEntryIds.Where(id => id != 0u);
}

static void PrintDirectionEntry(QuestDirectionEntryEntry directionEntry, ISet<uint> referencedWorldLocations)
{
    Console.WriteLine(
        $"    DIR-ENTRY {directionEntry.Id} wlActive={directionEntry.WorldLocation2Id} wlInactive={directionEntry.WorldLocation2IdInactive} " +
        $"worldZone={directionEntry.WorldZoneId} flags={directionEntry.QuestDirectionEntryFlags} faction={directionEntry.QuestDirectionFactionEnum}");

    AddWorldLocation(directionEntry.WorldLocation2Id, referencedWorldLocations);
    AddWorldLocation(directionEntry.WorldLocation2IdInactive, referencedWorldLocations);
}

static async Task<int> RunRidersReefCrosswalk(string[] args)
{
    if (args.Length == 0)
    {
        Console.Error.WriteLine("Usage: QuestTableInspector --riders-reef-crosswalk <world-sql-path> [--out <artifact-path>]");
        return 1;
    }

    string worldSqlPath = args[0];
    string? outputPath = GetOption(args, "--out");

    if (!File.Exists(worldSqlPath))
    {
        Console.Error.WriteLine($"World SQL file not found: {worldSqlPath}");
        return 1;
    }

    string sql = await File.ReadAllTextAsync(worldSqlPath);
    IReadOnlyList<RiderReefEntityRow> rows = ParseRiderReefWorldSql(sql);
    string crosswalk = BuildRiderReefCrosswalk(rows, worldSqlPath);

    if (!string.IsNullOrWhiteSpace(outputPath))
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(outputPath, crosswalk);
    }

    Console.Write(crosswalk);
    return 0;
}

static string? GetOption(string[] args, string optionName)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i].Equals(optionName, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    }

    return null;
}

static int RunSpellActionSet(string[] args)
{
    if (args.Length == 0)
    {
        Console.Error.WriteLine("Usage: QuestTableInspector --spell-action-set <tbl-path> [spell-id ...] [--xp <total-xp>] [--shortcut-set-id <id>]");
        return 1;
    }

    string gameTablePath = Path.GetFullPath(args[0]);
    if (!Directory.Exists(gameTablePath))
    {
        Console.Error.WriteLine($"Table directory not found: {gameTablePath}");
        return 1;
    }

    string? spell4BasePath = GetRequiredTablePath(gameTablePath, "Spell4Base.tbl");
    string? spell4Path = GetRequiredTablePath(gameTablePath, "Spell4.tbl");
    if (spell4BasePath is null || spell4Path is null)
        return 1;

    uint[] spellIds = GetSpellActionSetSpellIds(args);
    uint totalXp = GetUIntOption(args, "--xp", 4806u);
    uint shortcutSetId = GetUIntOption(args, "--shortcut-set-id", 1553u);

    Console.WriteLine($"TableRoot={gameTablePath}");

    var spell4BaseTable = new GameTable<Spell4BaseEntry>(spell4BasePath);
    var spell4Table = new GameTable<Spell4Entry>(spell4Path);

    string xpPerLevelPath = Path.Combine(gameTablePath, "XpPerLevel.tbl");
    if (File.Exists(xpPerLevelPath))
        PrintXpLevel(xpPerLevelPath, totalXp);

    foreach (uint spellId in spellIds)
        PrintSpellActionSetSpell(spellId, spell4BaseTable, spell4Table);

    string actionSlotPrereqPath = Path.Combine(gameTablePath, "ActionSlotPrereq.tbl");
    if (File.Exists(actionSlotPrereqPath))
        PrintActionSlotPrerequisites(gameTablePath, actionSlotPrereqPath);

    string actionBarShortcutSetPath = Path.Combine(gameTablePath, "ActionBarShortcutSet.tbl");
    if (File.Exists(actionBarShortcutSetPath))
        PrintActionBarShortcutSets(actionBarShortcutSetPath, spellIds, shortcutSetId);

    return 0;
}

static string? GetRequiredTablePath(string gameTablePath, string tableFileName)
{
    string tablePath = Path.Combine(gameTablePath, tableFileName);
    if (File.Exists(tablePath))
        return tablePath;

    Console.Error.WriteLine($"Required table file not found: {tablePath}");
    return null;
}

static uint GetUIntOption(string[] args, string optionName, uint defaultValue)
{
    string? value = GetOption(args, optionName);
    return value is null
        ? defaultValue
        : uint.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
}

static uint[] GetSpellActionSetSpellIds(string[] args)
{
    List<uint> spellIds = [];
    for (int i = 1; i < args.Length; i++)
    {
        if (args[i].Equals("--xp", StringComparison.OrdinalIgnoreCase) ||
            args[i].Equals("--shortcut-set-id", StringComparison.OrdinalIgnoreCase))
        {
            i++;
            continue;
        }

        if (args[i].StartsWith("--", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Unknown --spell-action-set option: {args[i]}");

        spellIds.Add(uint.Parse(args[i], NumberStyles.Integer, CultureInfo.InvariantCulture));
    }

    return spellIds.Count == 0
        ? [37968u, 19778u, 18309u, 18359u, 38017u]
        : spellIds.ToArray();
}

static void PrintXpLevel(string xpPerLevelPath, uint totalXp)
{
    var xpPerLevelTable = new GameTable<XpPerLevelEntry>(xpPerLevelPath);
    XpPerLevelEntry? xpLevel = xpPerLevelTable.Entries
        .Where(entry => entry.MinXpForLevel <= totalXp)
        .OrderBy(entry => entry.Id)
        .LastOrDefault();

    Console.WriteLine($"TotalXp={totalXp} maps to level {xpLevel?.Id.ToString(CultureInfo.InvariantCulture) ?? "-"}");
}

static void PrintSpellActionSetSpell(uint spellId, GameTable<Spell4BaseEntry> spell4BaseTable, GameTable<Spell4Entry> spell4Table)
{
    Spell4BaseEntry? directBase = spell4BaseTable.GetEntry(spellId);
    Spell4Entry? spell4 = spell4Table.GetEntry(spellId);
    Spell4BaseEntry? mappedBase = spell4 is null ? null : spell4BaseTable.GetEntry(spell4.Spell4BaseIdBaseSpell);

    Console.WriteLine(
        string.Join(
            " | ",
            $"Id={spellId}",
            $"DirectBase={directBase?.Id.ToString(CultureInfo.InvariantCulture) ?? "-"}",
            $"DirectWeaponSlot={directBase?.WeaponSlot.ToString(CultureInfo.InvariantCulture) ?? "-"}",
            $"DirectSpellType={directBase?.Spell4SpellTypesIdSpellType.ToString(CultureInfo.InvariantCulture) ?? "-"}",
            $"DirectSpellClass={directBase?.SpellClass.ToString(CultureInfo.InvariantCulture) ?? "-"}",
            $"DirectClass={directBase?.ClassIdPlayer.ToString(CultureInfo.InvariantCulture) ?? "-"}",
            $"DirectIcon={directBase?.Icon ?? "-"}",
            $"Spell4Base={spell4?.Spell4BaseIdBaseSpell.ToString(CultureInfo.InvariantCulture) ?? "-"}",
            $"Spell4Tier={spell4?.TierIndex.ToString(CultureInfo.InvariantCulture) ?? "-"}",
            $"MappedWeaponSlot={mappedBase?.WeaponSlot.ToString(CultureInfo.InvariantCulture) ?? "-"}",
            $"MappedSpellType={mappedBase?.Spell4SpellTypesIdSpellType.ToString(CultureInfo.InvariantCulture) ?? "-"}",
            $"MappedSpellClass={mappedBase?.SpellClass.ToString(CultureInfo.InvariantCulture) ?? "-"}",
            $"MappedClass={mappedBase?.ClassIdPlayer.ToString(CultureInfo.InvariantCulture) ?? "-"}",
            $"MappedIcon={mappedBase?.Icon ?? "-"}"));
}

static void PrintActionSlotPrerequisites(string gameTablePath, string actionSlotPrereqPath)
{
    Console.WriteLine("ActionSlotPrereq");
    var actionSlotPrereqTable = new GameTable<ActionSlotPrereqEntry>(actionSlotPrereqPath);
    List<uint> prerequisiteIds = [];
    foreach (ActionSlotPrereqEntry entry in actionSlotPrereqTable.Entries.OrderBy(entry => entry.SlotIndex))
    {
        Console.WriteLine($"Id={entry.Id} | SlotIndex={entry.SlotIndex} | PrerequisiteIdUnlock={entry.PrerequisiteIdUnlock}");
        if (entry.PrerequisiteIdUnlock != 0u)
            prerequisiteIds.Add(entry.PrerequisiteIdUnlock);
    }

    string prerequisitePath = Path.Combine(gameTablePath, "Prerequisite.tbl");
    if (!File.Exists(prerequisitePath))
        return;

    Console.WriteLine("ActionSlotPrereq prerequisites");
    var prerequisiteTable = new GameTable<PrerequisiteEntry>(prerequisitePath);
    foreach (uint prerequisiteId in prerequisiteIds.Distinct().Order())
    {
        PrerequisiteEntry? entry = prerequisiteTable.GetEntry(prerequisiteId);
        Console.WriteLine(
            entry is null
                ? $"PrerequisiteId={prerequisiteId} | missing"
                : string.Join(
                    " | ",
                    $"PrerequisiteId={entry.Id}",
                    $"Flags={entry.Flags}",
                    $"Type0={entry.PrerequisiteTypeId[0]}",
                    $"Comp0={entry.PrerequisiteComparisonId[0]}",
                    $"Object0={entry.ObjectId[0]}",
                    $"Value0={entry.Value[0]}",
                    $"Type1={entry.PrerequisiteTypeId[1]}",
                    $"Comp1={entry.PrerequisiteComparisonId[1]}",
                    $"Object1={entry.ObjectId[1]}",
                    $"Value1={entry.Value[1]}",
                    $"Type2={entry.PrerequisiteTypeId[2]}",
                    $"Comp2={entry.PrerequisiteComparisonId[2]}",
                    $"Object2={entry.ObjectId[2]}",
                    $"Value2={entry.Value[2]}"));
    }
}

static void PrintActionBarShortcutSets(string actionBarShortcutSetPath, IEnumerable<uint> spellIds, uint shortcutSetId)
{
    Console.WriteLine("ActionBarShortcutSet sample");
    var actionBarShortcutSetTable = new GameTable<ActionBarShortcutSetEntry>(actionBarShortcutSetPath);
    HashSet<uint> spellIdSet = spellIds.ToHashSet();
    foreach (ActionBarShortcutSetEntry entry in actionBarShortcutSetTable.Entries
        .Where(entry => entry.Id <= 20u || entry.Id == shortcutSetId || HasShortcutObject(entry, spellIdSet))
        .OrderBy(entry => entry.Id)
        .Take(80))
    {
        Console.WriteLine($"Id={entry.Id} | {FormatShortcutSet(entry)}");
    }
}

static bool HasShortcutObject(ActionBarShortcutSetEntry entry, ISet<uint> objectIds)
{
    return Enumerable.Range(0, 12)
        .Select(index => GetFieldValue<uint>(entry, $"ObjectId{index:00}"))
        .Any(objectIds.Contains);
}

static string FormatShortcutSet(ActionBarShortcutSetEntry entry)
{
    return string.Join(
        " | ",
        Enumerable.Range(0, 12)
            .Select(index =>
            {
                uint shortcutType = GetFieldValue<uint>(entry, $"ShortcutType{index:00}");
                uint objectId = GetFieldValue<uint>(entry, $"ObjectId{index:00}");
                return $"[{index}] type={shortcutType} object={objectId}";
            })
            .Where(value => !value.EndsWith("type=0 object=0", StringComparison.Ordinal)));
}

static T GetFieldValue<T>(object instance, string fieldName)
{
    System.Reflection.FieldInfo? field = instance.GetType().GetField(fieldName);
    if (field is null)
        throw new InvalidOperationException($"Field '{fieldName}' was not found on {instance.GetType().Name}.");

    return (T)field.GetValue(instance)!;
}

static async Task<int> RunTargetGroups(string[] args)
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Usage: QuestTableInspector --target-groups <tbl-path> <target-group-id ...>");
        return 1;
    }

    string gameTablePath = args[0];
    uint[] targetGroupIds = args.Skip(1).Select(uint.Parse).ToArray();

    var options = Options.Create(new GameTableConfig
    {
        GameTablePath = gameTablePath
    });

    var gameTableManager = new GameTableManager(options);
    await gameTableManager.Initialise();

    foreach (uint targetGroupId in targetGroupIds)
        PrintTargetGroup(targetGroupId, gameTableManager, new HashSet<uint>(), 0);

    return 0;
}

static void PrintTargetGroup(uint targetGroupId, GameTableManager gameTableManager, ISet<uint> visited, int depth)
{
    string indent = new(' ', depth * 2);
    if (!visited.Add(targetGroupId))
    {
        Console.WriteLine($"{indent}TG {targetGroupId} already printed");
        return;
    }

    TargetGroupEntry? entry = gameTableManager.TargetGroup.GetEntry(targetGroupId);
    if (entry is null)
    {
        Console.WriteLine($"{indent}TG {targetGroupId} missing");
        return;
    }

    string targetGroupType = Enum.IsDefined(typeof(TargetGroupType), unchecked((int)entry.Type))
        ? ((TargetGroupType)entry.Type).ToString()
        : $"Unknown({entry.Type})";

    uint[] dataEntries = entry.DataEntries.Where(id => id != 0u).ToArray();
    Console.WriteLine($"{indent}TG {entry.Id} type={targetGroupType}({entry.Type}) data=[{string.Join(",", dataEntries)}] text={entry.LocalizedTextIdDisplayString}");

    if ((TargetGroupType)entry.Type is not (TargetGroupType.OtherTargetGroup or TargetGroupType.OtherTargetGroupCreatures))
        return;

    foreach (uint childTargetGroupId in dataEntries)
        PrintTargetGroup(childTargetGroupId, gameTableManager, visited, depth + 1);
}

static IReadOnlyList<RiderReefEntityRow> ParseRiderReefWorldSql(string sql)
{
    var rows = new List<RiderReefEntityRow>();
    var rowsByBlockOffset = new Dictionary<(int Block, int Offset), RiderReefEntityRow>();
    string[] lines = sql.Replace("\r\n", "\n").Split('\n');

    int currentWorldId = 0;
    int entityBlock = 0;
    int latestEntityBlock = 0;
    string currentSection = "";

    for (int i = 0; i < lines.Length; i++)
    {
        string trimmed = lines[i].Trim();
        if (trimmed.StartsWith("--", StringComparison.Ordinal))
        {
            string comment = trimmed[2..].Trim();
            if (!string.IsNullOrWhiteSpace(comment) && comment.Any(c => c != '-'))
                currentSection = comment;

            continue;
        }

        Match worldMatch = Regex.Match(trimmed, @"^SET\s+@WORLD\s*=\s*(\d+)\s*;", RegexOptions.IgnoreCase);
        if (worldMatch.Success)
        {
            currentWorldId = int.Parse(worldMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            continue;
        }

        if (!trimmed.StartsWith("INSERT INTO", StringComparison.OrdinalIgnoreCase))
            continue;

        string statement = CollectSqlStatement(lines, ref i);
        Match insertMatch = Regex.Match(
            statement,
            @"INSERT\s+INTO\s+`(?<table>entity|entity_script)`\s*\((?<columns>.*?)\)\s*VALUES\s*(?<values>.*);",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (!insertMatch.Success)
            continue;

        string table = insertMatch.Groups["table"].Value;
        string[] columns = ParseColumnList(insertMatch.Groups["columns"].Value);
        List<IReadOnlyList<string>> tuples = ExtractSqlTuples(insertMatch.Groups["values"].Value)
            .Select(SplitSqlTuple)
            .ToList();

        if (table.Equals("entity", StringComparison.OrdinalIgnoreCase))
        {
            entityBlock++;
            latestEntityBlock = entityBlock;

            foreach (IReadOnlyList<string> tuple in tuples)
            {
                Dictionary<string, string> values = BuildValueMap(columns, tuple);
                var row = new RiderReefEntityRow
                {
                    Block = entityBlock,
                    Offset = GetGuidOffset(GetValue(values, "Id")),
                    Section = currentSection,
                    Type = (EntityType)GetUInt(values, "Type"),
                    Creature = GetUInt(values, "Creature"),
                    World = GetWorldValue(values, currentWorldId),
                    Area = GetUInt(values, "Area"),
                    X = GetFloat(values, "X"),
                    Y = GetFloat(values, "Y"),
                    Z = GetFloat(values, "Z"),
                    Rx = GetFloat(values, "RX"),
                    Ry = GetFloat(values, "RY"),
                    Rz = GetFloat(values, "RZ"),
                    DisplayInfo = GetUInt(values, "DisplayInfo"),
                    OutfitInfo = GetUShort(values, "OutfitInfo"),
                    Faction1 = GetUShort(values, "Faction1"),
                    Faction2 = GetUShort(values, "Faction2"),
                    QuestChecklistIdx = GetNullableByte(values, "QuestChecklistIdx"),
                    Mode = GetNullableByte(values, "Mode"),
                    ActivePropId = GetNullableULong(values, "ActivePropId")
                };

                rows.Add(row);
                rowsByBlockOffset[(row.Block, row.Offset)] = row;
            }
        }
        else if (latestEntityBlock != 0)
        {
            foreach (IReadOnlyList<string> tuple in tuples)
            {
                Dictionary<string, string> values = BuildValueMap(columns, tuple);
                int offset = GetGuidOffset(GetValue(values, "id"));
                if (rowsByBlockOffset.TryGetValue((latestEntityBlock, offset), out RiderReefEntityRow? row))
                    row.ScriptNames.Add(UnquoteSqlString(GetValue(values, "scriptName")));
            }
        }
    }

    return rows
        .OrderBy(r => r.Block)
        .ThenBy(r => r.Offset)
        .ToList();
}

static string CollectSqlStatement(string[] lines, ref int index)
{
    var builder = new StringBuilder();
    for (; index < lines.Length; index++)
    {
        builder.AppendLine(lines[index]);
        if (lines[index].Contains(';'))
            break;
    }

    return builder.ToString();
}

static string[] ParseColumnList(string columnList)
{
    return columnList
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(c => c.Trim().Trim('`'))
        .ToArray();
}

static List<string> ExtractSqlTuples(string values)
{
    var tuples = new List<string>();
    bool inString = false;
    int depth = 0;
    int tupleStart = -1;

    for (int i = 0; i < values.Length; i++)
    {
        char c = values[i];
        if (c == '\'')
        {
            if (inString && i + 1 < values.Length && values[i + 1] == '\'')
            {
                i++;
                continue;
            }

            inString = !inString;
            continue;
        }

        if (inString)
            continue;

        if (c == '(')
        {
            if (depth == 0)
                tupleStart = i + 1;

            depth++;
        }
        else if (c == ')')
        {
            depth--;
            if (depth == 0 && tupleStart >= 0)
            {
                tuples.Add(values[tupleStart..i]);
                tupleStart = -1;
            }
        }
    }

    return tuples;
}

static IReadOnlyList<string> SplitSqlTuple(string tuple)
{
    var values = new List<string>();
    bool inString = false;
    int valueStart = 0;

    for (int i = 0; i < tuple.Length; i++)
    {
        char c = tuple[i];
        if (c == '\'')
        {
            if (inString && i + 1 < tuple.Length && tuple[i + 1] == '\'')
            {
                i++;
                continue;
            }

            inString = !inString;
            continue;
        }

        if (c == ',' && !inString)
        {
            values.Add(tuple[valueStart..i].Trim());
            valueStart = i + 1;
        }
    }

    values.Add(tuple[valueStart..].Trim().TrimEnd(';'));
    return values;
}

static Dictionary<string, string> BuildValueMap(string[] columns, IReadOnlyList<string> values)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (int i = 0; i < columns.Length && i < values.Count; i++)
        result[columns[i]] = values[i];

    return result;
}

static string BuildRiderReefCrosswalk(IReadOnlyList<RiderReefEntityRow> rows, string sourcePath)
{
    var builder = new StringBuilder();
    var importedCreatureIds = rows
        .Select(r => r.Creature)
        .Distinct()
        .Order()
        .ToArray();

    var importedManagedCreatureIds = importedCreatureIds
        .Where(RiderReefCrosswalkConstants.ManagedCreatureIds.Contains)
        .ToArray();

    var managedDeltaCreatureIds = importedManagedCreatureIds
        .Where(id => !RiderReefCrosswalkConstants.LegacyPlayerManagedCreatureIds.Contains(id))
        .ToArray();

    builder.AppendLine("# Rider's Reef World 3460 Entity Crosswalk");
    builder.AppendLine();
    builder.AppendLine($"Source SQL: `{sourcePath}`");
    builder.AppendLine();
    builder.AppendLine("## Summary");
    builder.AppendLine();
    builder.AppendLine($"- Imported entity rows: {rows.Count}");
    builder.AppendLine($"- Imported creature groups: {importedCreatureIds.Length}");
    builder.AppendLine($"- Rows with entity_script bindings: {rows.Count(r => r.ScriptNames.Count != 0)}");
    builder.AppendLine($"- Legacy Player managed creature groups present in SQL: {string.Join(", ", importedCreatureIds.Where(RiderReefCrosswalkConstants.LegacyPlayerManagedCreatureIds.Contains))}");
    builder.AppendLine($"- Imported managed delta beyond the old Player cluster: {string.Join(", ", managedDeltaCreatureIds)}");
    builder.AppendLine();

    builder.AppendLine("## Creature Summary");
    builder.AppendLine();
    builder.AppendLine("| Creature | Count | Runtime bucket | QuestChecklistIdx | Mode | ActivePropId | Scripts | Sections |");
    builder.AppendLine("| ---: | ---: | --- | --- | --- | --- | --- | --- |");
    foreach (IGrouping<uint, RiderReefEntityRow> creatureGroup in rows.GroupBy(r => r.Creature).OrderBy(g => g.Key))
    {
        string checklist = FormatDistinct(creatureGroup.Select(r => r.QuestChecklistIdx?.ToString(CultureInfo.InvariantCulture)));
        string modes = FormatDistinct(creatureGroup.Select(r => r.Mode?.ToString(CultureInfo.InvariantCulture)));
        string activePropIds = FormatDistinct(creatureGroup.Select(r => r.ActivePropId?.ToString(CultureInfo.InvariantCulture)));
        string scripts = FormatDistinct(creatureGroup.SelectMany(r => r.ScriptNames));
        string sections = FormatDistinct(creatureGroup.Select(r => r.Section), maxItems: 3);

        builder.AppendLine($"| {creatureGroup.Key} | {creatureGroup.Count()} | {GetRuntimeBucket(creatureGroup.Key)} | {checklist} | {modes} | {activePropIds} | {scripts} | {EscapeMarkdown(sections)} |");
    }

    builder.AppendLine();
    builder.AppendLine("## Entity Rows");
    builder.AppendLine();
    builder.AppendLine("| Row | Creature | Type | Position | QuestChecklistIdx | Mode | ActivePropId | Scripts | Runtime bucket | Section |");
    builder.AppendLine("| --- | ---: | --- | --- | ---: | ---: | ---: | --- | --- | --- |");
    foreach (RiderReefEntityRow row in rows)
    {
        string scripts = row.ScriptNames.Count == 0 ? "" : string.Join(", ", row.ScriptNames);
        builder.AppendLine(
            $"| {row.RowKey} | {row.Creature} | {row.Type} | ({FormatFloat(row.X)}, {FormatFloat(row.Y)}, {FormatFloat(row.Z)}) | " +
            $"{FormatNullableByte(row.QuestChecklistIdx)} | {FormatNullableByte(row.Mode)} | {FormatNullableULong(row.ActivePropId)} | {scripts} | {GetRuntimeBucket(row.Creature)} | {EscapeMarkdown(row.Section)} |");
    }

    return builder.ToString();
}

static string FormatDistinct(IEnumerable<string?> values, int maxItems = 16)
{
    string[] distinct = values
        .Where(v => !string.IsNullOrWhiteSpace(v))
        .Select(v => v!)
        .Distinct()
        .Order(StringComparer.Ordinal)
        .Take(maxItems + 1)
        .ToArray();

    if (distinct.Length == 0)
        return "";

    if (distinct.Length <= maxItems)
        return string.Join(", ", distinct);

    return string.Join(", ", distinct.Take(maxItems)) + ", ...";
}

static string GetRuntimeBucket(uint creatureId)
{
    if (RiderReefCrosswalkConstants.LegacyPlayerManagedCreatureIds.Contains(creatureId))
        return "legacy-player-cluster";

    if (RiderReefCrosswalkConstants.ManagedCreatureIds.Contains(creatureId))
        return "imported-course-managed";

    return "imported-world-only";
}

static string EscapeMarkdown(string value)
{
    return string.IsNullOrWhiteSpace(value)
        ? ""
        : value.Replace("|", "\\|");
}

static string FormatFloat(float value)
{
    return value.ToString("0.###", CultureInfo.InvariantCulture);
}

static string FormatNullableByte(byte? value)
{
    return value?.ToString(CultureInfo.InvariantCulture) ?? "";
}

static string FormatNullableULong(ulong? value)
{
    return value?.ToString(CultureInfo.InvariantCulture) ?? "";
}

static string GetValue(IReadOnlyDictionary<string, string> values, string column)
{
    return values.TryGetValue(column, out string? value) ? value : "";
}

static int GetGuidOffset(string expression)
{
    Match match = Regex.Match(expression ?? "", @"@GUID\s*\+\s*(\d+)", RegexOptions.IgnoreCase);
    return match.Success ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
}

static int GetWorldValue(IReadOnlyDictionary<string, string> values, int currentWorldId)
{
    string value = GetValue(values, "World");
    if (value.Equals("@WORLD", StringComparison.OrdinalIgnoreCase))
        return currentWorldId;

    return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
        ? parsed
        : currentWorldId;
}

static uint GetUInt(IReadOnlyDictionary<string, string> values, string column)
{
    string value = GetValue(values, column);
    return uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint parsed)
        ? parsed
        : 0u;
}

static ushort GetUShort(IReadOnlyDictionary<string, string> values, string column)
{
    string value = GetValue(values, column);
    return ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort parsed)
        ? parsed
        : (ushort)0;
}

static byte? GetNullableByte(IReadOnlyDictionary<string, string> values, string column)
{
    string value = GetValue(values, column);
    return byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte parsed)
        ? parsed
        : null;
}

static ulong? GetNullableULong(IReadOnlyDictionary<string, string> values, string column)
{
    string value = GetValue(values, column);
    return ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong parsed)
        ? parsed
        : null;
}

static float GetFloat(IReadOnlyDictionary<string, string> values, string column)
{
    string value = GetValue(values, column);
    return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
        ? parsed
        : 0f;
}

static string UnquoteSqlString(string value)
{
    value = value?.Trim() ?? "";
    if (value.Length >= 2 && value[0] == '\'' && value[^1] == '\'')
        return value[1..^1].Replace("''", "'");

    return value;
}

static class RiderReefCrosswalkConstants
{
    public static readonly HashSet<uint> LegacyPlayerManagedCreatureIds =
    [
        70939u,
        72051u,
        73416u,
        73419u,
        73595u,
        73610u,
        73619u,
        73707u,
        73735u,
        73736u
    ];

    public static readonly HashSet<uint> ManagedCreatureIds =
    [
        70939u,
        72051u,
        73416u,
        73419u,
        73461u,
        73463u,
        73595u,
        73610u,
        73619u,
        73667u,
        73668u,
        73707u,
        73735u,
        73736u,
        74767u,
        74768u,
        74769u,
        75094u,
        75096u
    ];
}

sealed class RiderReefEntityRow
{
    public int Block { get; init; }
    public int Offset { get; init; }
    public string RowKey => $"block{Block:000}+{Offset}";
    public string Section { get; init; } = "";
    public EntityType Type { get; init; }
    public uint Creature { get; init; }
    public int World { get; init; }
    public uint Area { get; init; }
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }
    public float Rx { get; init; }
    public float Ry { get; init; }
    public float Rz { get; init; }
    public uint DisplayInfo { get; init; }
    public ushort OutfitInfo { get; init; }
    public ushort Faction1 { get; init; }
    public ushort Faction2 { get; init; }
    public byte? QuestChecklistIdx { get; init; }
    public byte? Mode { get; init; }
    public ulong? ActivePropId { get; init; }
    public List<string> ScriptNames { get; } = [];
}
