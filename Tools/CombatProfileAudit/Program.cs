using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.VisualBasic.FileIO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NexusForever.Database;
using NexusForever.Database.Configuration.Model;
using NexusForever.Database.World;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Script.Main.AI;

return await CombatProfileAuditCommand.Run(args);

internal static class CombatProfileAuditCommand
{
    private const int DefaultMaxReviewRows = 50;
    private static readonly string DefaultCreatureSpellMapPath = Path.Combine("Tools", "DataMapping", "output", "creature_spell_map.csv");

    public static async Task<int> Run(string[] args)
    {
        AuditOptions options;
        try
        {
            options = AuditOptions.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            WriteUsage(Console.Error);
            return 1;
        }

        if (options.ShowHelp)
        {
            WriteUsage(Console.Out);
            return 0;
        }

        try
        {
            options = options.ResolveRuntimeConfig().ResolveInputPaths();
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            WriteUsage(Console.Error);
            return 1;
        }

        if (!Directory.Exists(options.GameTablePath))
        {
            Console.Error.WriteLine($"Game-table path was not found: {options.GameTablePath}");
            return 2;
        }

        Directory.CreateDirectory(options.OutputDirectory);

        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = options.GameTablePath
        }));
        await gameTableManager.Initialise();

        DefaultCombatProfileProvider provider = DefaultCombatProfileProvider.ForGameTables(gameTableManager);
        CombatProfileAuditDetails details = provider.GetAuditDetails();

        IReadOnlyList<SpawnPriorityRow> spawnPriorityRows = [];
        if (!string.IsNullOrWhiteSpace(options.WorldConnectionString))
        {
            IReadOnlyList<WorldSpawnSummary> spawnSummaries = await LoadWorldSpawnSummaries(options.WorldConnectionString);
            spawnPriorityRows = BuildSpawnPriorityRows(details, spawnSummaries, gameTableManager);
        }

        IReadOnlyDictionary<uint, CreatureSpellSignature> creatureSpellSignatures = new Dictionary<uint, CreatureSpellSignature>();
        IReadOnlyList<CombatKitGroupCandidateRow> combatKitGroupCandidates = [];
        if (!string.IsNullOrWhiteSpace(options.CreatureSpellMapPath))
        {
            creatureSpellSignatures = LoadReviewedCreatureSpellSignatures(options.CreatureSpellMapPath, gameTableManager);
            combatKitGroupCandidates = BuildCombatKitGroupCandidates(details, spawnPriorityRows, gameTableManager, creatureSpellSignatures);
        }
        IReadOnlyList<CombatActionRuleCandidateRow> combatActionRuleCandidates = BuildCombatActionRuleCandidates(details, spawnPriorityRows, gameTableManager, creatureSpellSignatures);

        string markdown = BuildMarkdownReport(details, options, spawnPriorityRows, combatKitGroupCandidates, combatActionRuleCandidates);
        string creatureCsv = BuildCreatureCsv(details.CreatureRows);
        string actionCsv = BuildActionCsv(details.ActionRows);
        string spawnPriorityCsv = BuildSpawnPriorityCsv(spawnPriorityRows);
        string combatKitGroupCandidateCsv = BuildCombatKitGroupCandidateCsv(combatKitGroupCandidates);
        string combatActionRuleCandidateCsv = BuildCombatActionRuleCandidateCsv(combatActionRuleCandidates);

        string markdownPath = Path.Combine(options.OutputDirectory, "combat-profile-audit.md");
        string creaturePath = Path.Combine(options.OutputDirectory, "combat-profile-creatures.csv");
        string actionPath = Path.Combine(options.OutputDirectory, "combat-profile-actions.csv");
        string spawnPriorityPath = Path.Combine(options.OutputDirectory, "combat-profile-spawn-priority.csv");
        string combatKitGroupCandidatePath = Path.Combine(options.OutputDirectory, "combat-kit-group-candidates.csv");
        string combatActionRuleCandidatePath = Path.Combine(options.OutputDirectory, "combat-action-rule-candidates.csv");
        await File.WriteAllTextAsync(markdownPath, markdown, Encoding.UTF8);
        await File.WriteAllTextAsync(creaturePath, creatureCsv, Encoding.UTF8);
        await File.WriteAllTextAsync(actionPath, actionCsv, Encoding.UTF8);
        await File.WriteAllTextAsync(spawnPriorityPath, spawnPriorityCsv, Encoding.UTF8);
        await File.WriteAllTextAsync(combatKitGroupCandidatePath, combatKitGroupCandidateCsv, Encoding.UTF8);
        await File.WriteAllTextAsync(combatActionRuleCandidatePath, combatActionRuleCandidateCsv, Encoding.UTF8);

        Console.WriteLine($"Wrote combat profile audit to {options.OutputDirectory}");
        Console.WriteLine($"Mapped creatures: {details.Summary.MappedCreatureCount}");
        Console.WriteLine($"Unmapped creatures: {details.Summary.UnmappedCreatureCount}");
        if (spawnPriorityRows.Count != 0)
        {
            Console.WriteLine($"Runtime spawned creature ids: {spawnPriorityRows.Count}");
            Console.WriteLine($"Runtime spawned unmapped creature ids: {spawnPriorityRows.Count(row => row.Source == CombatProfileResolutionSource.None)}");
            Console.WriteLine($"Runtime spawned combat-signal unmapped creature ids: {spawnPriorityRows.Count(row => row.Source == CombatProfileResolutionSource.None && row.CombatSignalCount != 0)}");
        }
        if (combatKitGroupCandidates.Count != 0)
            Console.WriteLine($"Combat kit group candidates: {combatKitGroupCandidates.Count}");
        if (combatActionRuleCandidates.Count != 0)
            Console.WriteLine($"Combat action rule candidate clusters: {combatActionRuleCandidates.Count}");
        Console.WriteLine($"Unknown Creature2Action rows: {details.Summary.ActionUnknownRowCount}");
        Console.WriteLine($"Rejected Creature2Action rules: {details.Summary.ActionRejectedRuleRowCount}");
        Console.WriteLine($"Missing Spell4 rows: {details.Summary.ActionMissingSpellRowCount}");
        return 0;
    }

    private static void WriteUsage(TextWriter writer)
    {
        writer.WriteLine("Usage:");
        writer.WriteLine("  CombatProfileAudit <game-table-path> [--world-connection <connection-string>] [--creature-spell-map <path>] [--output-dir <path>] [--max-review-rows <count>]");
        writer.WriteLine("  CombatProfileAudit --world-config <WorldServer.json> [--creature-spell-map <path>] [--output-dir <path>] [--max-review-rows <count>]");
        writer.WriteLine();
        writer.WriteLine("Examples:");
        writer.WriteLine("  dotnet run --project Tools\\CombatProfileAudit\\CombatProfileAudit.csproj -- C:\\WildStar\\Patch\\ClientData");
        writer.WriteLine("  dotnet run --project Tools\\CombatProfileAudit\\CombatProfileAudit.csproj -- --world-config Source\\NexusForever.WorldServer\\bin\\Debug\\net10.0\\WorldServer.json");
    }

    private static string BuildMarkdownReport(
        CombatProfileAuditDetails details,
        AuditOptions options,
        IReadOnlyList<SpawnPriorityRow> spawnPriorityRows,
        IReadOnlyList<CombatKitGroupCandidateRow> combatKitGroupCandidates,
        IReadOnlyList<CombatActionRuleCandidateRow> combatActionRuleCandidates)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Combat Profile Audit");
        builder.AppendLine();
        builder.AppendLine($"- Generated UTC: `{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}`");
        builder.AppendLine($"- Game-table path: `{options.GameTablePath}`");
        builder.AppendLine($"- Runtime world context: `{GetWorldContextDescription(options, spawnPriorityRows)}`");
        builder.AppendLine($"- Creature spell map: `{GetCreatureSpellMapDescription(options)}`");
        builder.AppendLine("- Runtime safety: uses game tables, committed combat catalog assets, and optional runtime-owned world entity data only.");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine("| Metric | Count |");
        builder.AppendLine("| --- | ---: |");
        AppendMetric(builder, "Manual overrides", details.Summary.ManualOverrideCreatureCount);
        AppendMetric(builder, "Reviewed kit mappings", details.Summary.ReviewedKitMappingCreatureCount);
        AppendMetric(builder, "Action-derived profiles", details.Summary.ActionDerivedProfileCount);
        AppendMetric(builder, "Mapped creatures", details.Summary.MappedCreatureCount);
        AppendMetric(builder, "Unmapped creatures", details.Summary.UnmappedCreatureCount);
        AppendMetric(builder, "Visual-only action rows", details.Summary.ActionVisualOnlyRowCount);
        AppendMetric(builder, "Ignored-rule action rows", details.Summary.ActionIgnoredRuleRowCount);
        AppendMetric(builder, "Rejected-rule action rows", details.Summary.ActionRejectedRuleRowCount);
        AppendMetric(builder, "Unknown action rows", details.Summary.ActionUnknownRowCount);
        AppendMetric(builder, "Missing Spell4 action rows", details.Summary.ActionMissingSpellRowCount);
        if (spawnPriorityRows.Count != 0)
        {
            AppendMetric(builder, "Runtime spawned creature ids", spawnPriorityRows.Count);
            AppendMetric(builder, "Runtime spawned unmapped creature ids", spawnPriorityRows.Count(row => row.Source == CombatProfileResolutionSource.None));
            AppendMetric(builder, "Runtime spawned combat-signal unmapped creature ids", spawnPriorityRows.Count(row => row.Source == CombatProfileResolutionSource.None && row.CombatSignalCount != 0));
            AppendMetric(builder, "Runtime non-player spawns", spawnPriorityRows.Sum(row => row.SpawnCount));
        }
        if (combatKitGroupCandidates.Count != 0)
        {
            AppendMetric(builder, "Combat kit group candidates", combatKitGroupCandidates.Count);
            AppendMetric(builder, "Existing-kit propagation candidates", combatKitGroupCandidates.Count(row => !string.IsNullOrWhiteSpace(row.MappedKitIds)));
        }
        if (combatActionRuleCandidates.Count != 0)
            AppendMetric(builder, "Combat action rule candidate clusters", combatActionRuleCandidates.Count);

        AppendSourceBreakdown(builder, details);
        AppendExistingKitPropagationQueue(builder, combatKitGroupCandidates, options.MaxReviewRows);
        AppendCombatKitGroupCandidateQueue(builder, combatKitGroupCandidates, options.MaxReviewRows);
        AppendCombatActionRuleCandidateQueue(builder, combatActionRuleCandidates, options.MaxReviewRows);
        AppendCombatSignalQueue(builder, spawnPriorityRows, options.MaxReviewRows);
        AppendSpawnPriorityQueue(builder, spawnPriorityRows, options.MaxReviewRows);
        AppendUnmappedQueue(builder, details, options.MaxReviewRows);
        AppendActionReviewQueue(builder, details, options.MaxReviewRows);

        return builder.ToString();
    }

    private static string GetWorldContextDescription(AuditOptions options, IReadOnlyList<SpawnPriorityRow> spawnPriorityRows)
    {
        if (spawnPriorityRows.Count == 0)
            return string.IsNullOrWhiteSpace(options.WorldConnectionString)
                ? "not supplied"
                : "supplied; no non-player spawns found";

        if (!string.IsNullOrWhiteSpace(options.WorldConfigPath))
            return options.WorldConfigPath;

        return "explicit connection string";
    }

    private static string GetCreatureSpellMapDescription(AuditOptions options)
    {
        return string.IsNullOrWhiteSpace(options.CreatureSpellMapPath)
            ? "not supplied"
            : options.CreatureSpellMapPath;
    }

    private static void AppendMetric(StringBuilder builder, string name, int count)
    {
        builder.AppendLine($"| {name} | {count.ToString(CultureInfo.InvariantCulture)} |");
    }

    private static void AppendSourceBreakdown(StringBuilder builder, CombatProfileAuditDetails details)
    {
        builder.AppendLine();
        builder.AppendLine("## Resolution Sources");
        builder.AppendLine();
        builder.AppendLine("| Source | Creatures |");
        builder.AppendLine("| --- | ---: |");

        foreach (IGrouping<CombatProfileResolutionSource, CombatProfileCreatureAuditRow> group in details.CreatureRows.GroupBy(row => row.Source).OrderBy(group => group.Key))
            builder.AppendLine($"| {group.Key} | {group.Count().ToString(CultureInfo.InvariantCulture)} |");
    }

    private static void AppendExistingKitPropagationQueue(StringBuilder builder, IReadOnlyList<CombatKitGroupCandidateRow> rows, int maxRows)
    {
        List<CombatKitGroupCandidateRow> propagationRows = rows
            .Where(row => !string.IsNullOrWhiteSpace(row.MappedKitIds))
            .OrderByDescending(row => row.CandidateRuntimeSpawnCount)
            .ThenByDescending(row => row.CandidateUnmappedCreatureCount)
            .ThenByDescending(row => row.CandidateCreatureCount)
            .ThenBy(row => GetSignalPriority(row.GroupType))
            .ThenBy(row => row.GroupId)
            .Take(maxRows)
            .ToList();

        if (propagationRows.Count == 0)
            return;

        builder.AppendLine();
        builder.AppendLine("## Existing Kit Propagation Candidates");
        builder.AppendLine();
        builder.AppendLine("| Existing Kit | Group | Id | Candidates | Unmapped | Spawns | Spell4Ids | Sample Creatures |");
        builder.AppendLine("| --- | --- | ---: | ---: | ---: | ---: | --- | --- |");

        foreach (CombatKitGroupCandidateRow row in propagationRows)
        {
            builder.AppendLine(
                $"| {EscapeMarkdown(row.MappedKitIds)} | {row.GroupType} | {row.GroupId} | {row.CandidateCreatureCount} | " +
                $"{row.CandidateUnmappedCreatureCount} | {row.CandidateRuntimeSpawnCount} | {EscapeMarkdown(row.Spell4Ids)} | " +
                $"{EscapeMarkdown(row.SampleCreatureNames)} |");
        }
    }

    private static void AppendCombatKitGroupCandidateQueue(StringBuilder builder, IReadOnlyList<CombatKitGroupCandidateRow> rows, int maxRows)
    {
        if (rows.Count == 0)
            return;

        builder.AppendLine();
        builder.AppendLine("## Combat Kit Group Candidates");
        builder.AppendLine();
        builder.AppendLine("| Group | Id | Candidates | Unmapped | Spawned | Spawns | Spell4Ids | Sample Creatures | Recommendation |");
        builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | --- | --- | --- |");

        foreach (CombatKitGroupCandidateRow row in rows.Take(maxRows))
        {
            builder.AppendLine(
                $"| {row.GroupType} | {row.GroupId} | {row.CandidateCreatureCount} | {row.CandidateUnmappedCreatureCount} | " +
                $"{row.CandidateSpawnedCreatureCount} | {row.CandidateRuntimeSpawnCount} | {EscapeMarkdown(row.Spell4Ids)} | " +
                $"{EscapeMarkdown(row.SampleCreatureNames)} | {EscapeMarkdown(row.Recommendation)} |");
        }
    }

    private static void AppendCombatActionRuleCandidateQueue(StringBuilder builder, IReadOnlyList<CombatActionRuleCandidateRow> rows, int maxRows)
    {
        if (rows.Count == 0)
            return;

        builder.AppendLine();
        builder.AppendLine("## Combat Action Rule Candidate Clusters");
        builder.AppendLine();
        builder.AppendLine("| State | Event | Action | Rows | Creatures | Spawns | Data Shape | Spell Bridge Hits | Data00 | Data01 | Sample Creatures | Recommendation |");
        builder.AppendLine("| ---: | ---: | ---: | ---: | ---: | ---: | --- | ---: | --- | --- | --- | --- |");

        foreach (CombatActionRuleCandidateRow row in rows.Take(maxRows))
        {
            builder.AppendLine(
                $"| {row.State} | {row.Event} | {row.Action} | {row.CreatureRowCount} | {row.CreatureCount} | " +
                $"{row.RuntimeSpawnCount} | {EscapeMarkdown(row.DataShape)} | {row.ActionData00SpellBridgeHitCount + row.ActionData01SpellBridgeHitCount} | " +
                $"{EscapeMarkdown(row.ActionData00Values)} | {EscapeMarkdown(row.ActionData01Values)} | " +
                $"{EscapeMarkdown(row.SampleCreatureNames)} | {EscapeMarkdown(row.Recommendation)} |");
        }
    }

    private static void AppendCombatSignalQueue(StringBuilder builder, IReadOnlyList<SpawnPriorityRow> spawnPriorityRows, int maxRows)
    {
        if (spawnPriorityRows.Count == 0)
            return;

        List<SpawnPriorityRow> rows = spawnPriorityRows
            .Where(row => row.Source == CombatProfileResolutionSource.None && row.CombatSignalCount != 0)
            .OrderByDescending(row => row.ReviewActionRows != 0)
            .ThenByDescending(row => row.Creature2ActionSetId != 0u)
            .ThenByDescending(row => row.SoundEventIdAggro != 0u)
            .ThenByDescending(row => row.SoundCombatLoopId != 0u)
            .ThenByDescending(row => row.SpawnCount)
            .ThenBy(row => row.Creature2Id)
            .Take(maxRows)
            .ToList();

        builder.AppendLine();
        builder.AppendLine("## Spawned Combat-Signal Queue");
        builder.AppendLine();
        builder.AppendLine("| Creature2Id | Name | Spawns | Worlds | Areas | ActionSetId | Unknown Actions | Visual Actions | Signals |");
        builder.AppendLine("| ---: | --- | ---: | --- | --- | ---: | ---: | ---: | --- |");

        foreach (SpawnPriorityRow row in rows)
        {
            builder.AppendLine(
                $"| {row.Creature2Id} | {EscapeMarkdown(row.CreatureName)} | {row.SpawnCount} | {EscapeMarkdown(row.Worlds)} | {EscapeMarkdown(row.Areas)} | " +
                $"{row.Creature2ActionSetId} | {row.UnknownActionRows} | {row.VisualOnlyActionRows} | {EscapeMarkdown(row.PrioritySignals)} |");
        }
    }

    private static void AppendSpawnPriorityQueue(StringBuilder builder, IReadOnlyList<SpawnPriorityRow> spawnPriorityRows, int maxRows)
    {
        if (spawnPriorityRows.Count == 0)
            return;

        List<SpawnPriorityRow> rows = spawnPriorityRows
            .Where(row => row.Source == CombatProfileResolutionSource.None)
            .Take(maxRows)
            .ToList();

        builder.AppendLine();
        builder.AppendLine("## Spawn-Prioritized Unmapped Queue");
        builder.AppendLine();
        builder.AppendLine("| Creature2Id | Name | Spawns | Worlds | Areas | ActionSetId | Unknown Actions | Visual Actions | Note |");
        builder.AppendLine("| ---: | --- | ---: | --- | --- | ---: | ---: | ---: | --- |");

        foreach (SpawnPriorityRow row in rows)
        {
            string note = row.Creature2ActionSetId == 0u
                ? "No Creature2Action set"
                : "Has Creature2Action set";
            builder.AppendLine(
                $"| {row.Creature2Id} | {EscapeMarkdown(row.CreatureName)} | {row.SpawnCount} | {EscapeMarkdown(row.Worlds)} | {EscapeMarkdown(row.Areas)} | " +
                $"{row.Creature2ActionSetId} | {row.UnknownActionRows} | {row.VisualOnlyActionRows} | {note} |");
        }
    }

    private static void AppendUnmappedQueue(StringBuilder builder, CombatProfileAuditDetails details, int maxRows)
    {
        List<CombatProfileCreatureAuditRow> rows = details.CreatureRows
            .Where(row => row.Source == CombatProfileResolutionSource.None)
            .OrderByDescending(row => row.Creature2ActionSetId != 0u)
            .ThenBy(row => row.Creature2Id)
            .Take(maxRows)
            .ToList();

        builder.AppendLine();
        builder.AppendLine("## Unmapped Creature Review Queue");
        builder.AppendLine();
        builder.AppendLine("| Creature2Id | Name | ActionSetId | Note |");
        builder.AppendLine("| ---: | --- | ---: | --- |");

        foreach (CombatProfileCreatureAuditRow row in rows)
        {
            string note = row.Creature2ActionSetId == 0u
                ? "No Creature2Action set"
                : "Has Creature2Action set";
            builder.AppendLine($"| {row.Creature2Id} | {EscapeMarkdown(row.CreatureName)} | {row.Creature2ActionSetId} | {note} |");
        }
    }

    private static void AppendActionReviewQueue(StringBuilder builder, CombatProfileAuditDetails details, int maxRows)
    {
        CombatActionAuditStatus[] reviewStatuses =
        [
            CombatActionAuditStatus.Unknown,
            CombatActionAuditStatus.RejectedRule,
            CombatActionAuditStatus.MissingSpell
        ];
        List<CombatProfileActionAuditRow> rows = details.ActionRows
            .Where(row => reviewStatuses.Contains(row.Status))
            .OrderBy(row => row.Status)
            .ThenBy(row => row.Creature2Id)
            .ThenBy(row => row.Creature2ActionId)
            .Take(maxRows)
            .ToList();

        builder.AppendLine();
        builder.AppendLine("## Creature2Action Review Queue");
        builder.AppendLine();
        builder.AppendLine("| Creature2Id | Name | ActionSetId | ActionRowId | State | Event | Action | Status | Spell4 | Diagnostic |");
        builder.AppendLine("| ---: | --- | ---: | ---: | ---: | ---: | ---: | --- | ---: | --- |");

        foreach (CombatProfileActionAuditRow row in rows)
        {
            builder.AppendLine(
                $"| {row.Creature2Id} | {EscapeMarkdown(row.CreatureName)} | {row.Creature2ActionSetId} | {row.Creature2ActionId} | " +
                $"{row.State} | {row.Event} | {row.Action} | {row.Status} | {row.Spell4Id} | {EscapeMarkdown(row.Diagnostic)} |");
        }
    }

    private static string BuildCreatureCsv(IEnumerable<CombatProfileCreatureAuditRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(Csv(
            "creature2_id",
            "creature_name",
            "creature2_action_set_id",
            "source",
            "profile_id",
            "kit_id",
            "auto_attack_count",
            "special_attack_count",
            "diagnostics"));

        foreach (CombatProfileCreatureAuditRow row in rows)
        {
            builder.AppendLine(Csv(
                row.Creature2Id,
                row.CreatureName,
                row.Creature2ActionSetId,
                row.Source,
                row.ProfileId,
                row.KitId,
                row.AutoAttackCount,
                row.SpecialAttackCount,
                string.Join("; ", row.Diagnostics ?? [])));
        }

        return builder.ToString();
    }

    private static string BuildActionCsv(IEnumerable<CombatProfileActionAuditRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(Csv(
            "creature2_id",
            "creature_name",
            "creature2_action_set_id",
            "creature2_action_id",
            "order_index",
            "state",
            "event",
            "action",
            "visual_effect_id",
            "action_data_00",
            "action_data_01",
            "status",
            "rule_source",
            "rule_evidence",
            "spell4_id",
            "diagnostic"));

        foreach (CombatProfileActionAuditRow row in rows)
        {
            builder.AppendLine(Csv(
                row.Creature2Id,
                row.CreatureName,
                row.Creature2ActionSetId,
                row.Creature2ActionId,
                row.OrderIndex,
                row.State,
                row.Event,
                row.Action,
                row.VisualEffectId,
                row.ActionData00,
                row.ActionData01,
                row.Status,
                row.RuleSource,
                row.RuleEvidence,
                row.Spell4Id,
                row.Diagnostic));
        }

        return builder.ToString();
    }

    private static string BuildSpawnPriorityCsv(IEnumerable<SpawnPriorityRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(Csv(
            "creature2_id",
            "creature_name",
            "spawn_count",
            "worlds",
            "areas",
            "source",
            "profile_id",
            "kit_id",
            "creature2_action_set_id",
            "auto_attack_count",
            "special_attack_count",
            "unknown_action_rows",
            "visual_only_action_rows",
            "review_action_rows",
            "combat_signal_count",
            "priority_signals",
            "creature2_difficulty_id",
            "creature2_archetype_id",
            "creature2_tier_id",
            "sound_event_id_aggro",
            "sound_combat_loop_id",
            "diagnostics"));

        foreach (SpawnPriorityRow row in rows)
        {
            builder.AppendLine(Csv(
                row.Creature2Id,
                row.CreatureName,
                row.SpawnCount,
                row.Worlds,
                row.Areas,
                row.Source,
                row.ProfileId,
                row.KitId,
                row.Creature2ActionSetId,
                row.AutoAttackCount,
                row.SpecialAttackCount,
                row.UnknownActionRows,
                row.VisualOnlyActionRows,
                row.ReviewActionRows,
                row.CombatSignalCount,
                row.PrioritySignals,
                row.Creature2DifficultyId,
                row.Creature2ArcheTypeId,
                row.Creature2TierId,
                row.SoundEventIdAggro,
                row.SoundCombatLoopId,
                row.Diagnostics));
        }

        return builder.ToString();
    }

    private static string BuildCombatKitGroupCandidateCsv(IEnumerable<CombatKitGroupCandidateRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(Csv(
            "group_type",
            "group_id",
            "group_creature_count",
            "group_spell_bridge_creature_count",
            "candidate_creature_count",
            "candidate_unmapped_creature_count",
            "candidate_mapped_creature_count",
            "candidate_spawned_creature_count",
            "candidate_runtime_spawn_count",
            "spell_count",
            "spell4_ids",
            "spell_names",
            "match_statuses",
            "mapped_kit_ids",
            "sample_creature2_ids",
            "sample_creature_names",
            "recommendation"));

        foreach (CombatKitGroupCandidateRow row in rows)
        {
            builder.AppendLine(Csv(
                row.GroupType,
                row.GroupId,
                row.GroupCreatureCount,
                row.GroupSpellBridgeCreatureCount,
                row.CandidateCreatureCount,
                row.CandidateUnmappedCreatureCount,
                row.CandidateMappedCreatureCount,
                row.CandidateSpawnedCreatureCount,
                row.CandidateRuntimeSpawnCount,
                row.SpellCount,
                row.Spell4Ids,
                row.SpellNames,
                row.MatchStatuses,
                row.MappedKitIds,
                row.SampleCreature2Ids,
                row.SampleCreatureNames,
                row.Recommendation));
        }

        return builder.ToString();
    }

    private static string BuildCombatActionRuleCandidateCsv(IEnumerable<CombatActionRuleCandidateRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(Csv(
            "state",
            "event",
            "action",
            "statuses",
            "creature_row_count",
            "creature_count",
            "unmapped_creature_count",
            "mapped_creature_count",
            "runtime_spawned_creature_count",
            "runtime_spawn_count",
            "action_set_count",
            "action_row_count",
            "visual_effect_count",
            "visual_effect_ids",
            "zero_data_count",
            "same_data_pair_count",
            "action_data_00_nonzero_count",
            "action_data_00_distinct_count",
            "action_data_00_spell4_hit_count",
            "action_data_00_spell_bridge_hit_count",
            "action_data_00_values",
            "action_data_01_nonzero_count",
            "action_data_01_distinct_count",
            "action_data_01_spell4_hit_count",
            "action_data_01_spell_bridge_hit_count",
            "action_data_01_values",
            "data_shape",
            "sample_action_sets",
            "sample_action_row_ids",
            "sample_creature2_ids",
            "sample_creature_names",
            "recommendation"));

        foreach (CombatActionRuleCandidateRow row in rows)
        {
            builder.AppendLine(Csv(
                row.State,
                row.Event,
                row.Action,
                row.Statuses,
                row.CreatureRowCount,
                row.CreatureCount,
                row.UnmappedCreatureCount,
                row.MappedCreatureCount,
                row.RuntimeSpawnedCreatureCount,
                row.RuntimeSpawnCount,
                row.ActionSetCount,
                row.ActionRowCount,
                row.VisualEffectCount,
                row.VisualEffectIds,
                row.ZeroDataCount,
                row.SameDataPairCount,
                row.ActionData00NonZeroCount,
                row.ActionData00DistinctCount,
                row.ActionData00Spell4HitCount,
                row.ActionData00SpellBridgeHitCount,
                row.ActionData00Values,
                row.ActionData01NonZeroCount,
                row.ActionData01DistinctCount,
                row.ActionData01Spell4HitCount,
                row.ActionData01SpellBridgeHitCount,
                row.ActionData01Values,
                row.DataShape,
                row.SampleActionSets,
                row.SampleActionRowIds,
                row.SampleCreature2Ids,
                row.SampleCreatureNames,
                row.Recommendation));
        }

        return builder.ToString();
    }

    private static async Task<IReadOnlyList<WorldSpawnSummary>> LoadWorldSpawnSummaries(string connectionString)
    {
        await using var context = new WorldContext(new DatabaseConnectionString
        {
            Provider = DatabaseProvider.MySql,
            ConnectionString = connectionString
        });

        List<WorldSpawnFact> facts = await context.Entity
            .AsNoTracking()
            .Where(entity => entity.Type == EntityType.NonPlayer && entity.Creature != 0u)
            .Select(entity => new WorldSpawnFact(entity.Creature, entity.World, entity.Area))
            .ToListAsync();

        return facts
            .GroupBy(fact => fact.Creature2Id)
            .Select(group => new WorldSpawnSummary(
                group.Key,
                group.Count(),
                group.Select(fact => fact.World).Distinct().OrderBy(world => world).ToArray(),
                group.Select(fact => fact.Area).Distinct().OrderBy(area => area).ToArray()))
            .OrderByDescending(row => row.SpawnCount)
            .ThenBy(row => row.Creature2Id)
            .ToList();
    }

    private static IReadOnlyList<SpawnPriorityRow> BuildSpawnPriorityRows(CombatProfileAuditDetails details, IReadOnlyList<WorldSpawnSummary> spawnSummaries, IGameTableManager gameTableManager)
    {
        Dictionary<uint, CombatProfileCreatureAuditRow> creatureRows = details.CreatureRows.ToDictionary(row => row.Creature2Id);
        Dictionary<uint, ActionAuditCounts> actionCounts = details.ActionRows
            .GroupBy(row => row.Creature2Id)
            .ToDictionary(
                group => group.Key,
                group => new ActionAuditCounts(
                    group.Count(row => row.Status == CombatActionAuditStatus.Unknown),
                    group.Count(row => row.Status == CombatActionAuditStatus.VisualOnly),
                    group.Count(row => row.Status is CombatActionAuditStatus.Unknown or CombatActionAuditStatus.RejectedRule or CombatActionAuditStatus.MissingSpell)));

        return spawnSummaries
            .Select(summary => BuildSpawnPriorityRow(summary, creatureRows, actionCounts, gameTableManager))
            .OrderBy(row => row.Source == CombatProfileResolutionSource.None ? 0 : 1)
            .ThenByDescending(row => row.SpawnCount)
            .ThenByDescending(row => row.Creature2ActionSetId != 0u)
            .ThenByDescending(row => row.CombatSignalCount)
            .ThenByDescending(row => row.ReviewActionRows)
            .ThenBy(row => row.Creature2Id)
            .ToList();
    }

    private static SpawnPriorityRow BuildSpawnPriorityRow(
        WorldSpawnSummary summary,
        IReadOnlyDictionary<uint, CombatProfileCreatureAuditRow> creatureRows,
        IReadOnlyDictionary<uint, ActionAuditCounts> actionCounts,
        IGameTableManager gameTableManager)
    {
        creatureRows.TryGetValue(summary.Creature2Id, out CombatProfileCreatureAuditRow? creatureRow);
        actionCounts.TryGetValue(summary.Creature2Id, out ActionAuditCounts? counts);
        var creatureEntry = gameTableManager.Creature2.GetEntry(summary.Creature2Id);
        List<string> prioritySignals = BuildPrioritySignals(creatureRow, counts, creatureEntry);

        return new SpawnPriorityRow(
            summary.Creature2Id,
            creatureRow?.CreatureName ?? "(not found in Creature2.tbl)",
            creatureRow?.Source ?? CombatProfileResolutionSource.None,
            creatureRow?.ProfileId ?? string.Empty,
            creatureRow?.KitId ?? string.Empty,
            creatureRow?.Creature2ActionSetId ?? 0u,
            summary.SpawnCount,
            FormatIdList(summary.Worlds),
            FormatIdList(summary.Areas),
            counts?.UnknownActionRows ?? 0,
            counts?.VisualOnlyActionRows ?? 0,
            counts?.ReviewActionRows ?? 0,
            prioritySignals.Count,
            string.Join(";", prioritySignals),
            creatureEntry?.Creature2DifficultyId ?? 0u,
            creatureEntry?.Creature2ArcheTypeId ?? 0u,
            creatureEntry?.Creature2TierId ?? 0u,
            creatureEntry?.SoundEventIdAggro ?? 0u,
            creatureEntry?.SoundCombatLoopId ?? 0u,
            creatureRow?.AutoAttackCount ?? 0,
            creatureRow?.SpecialAttackCount ?? 0,
            string.Join("; ", creatureRow?.Diagnostics ?? []));
    }

    private static List<string> BuildPrioritySignals(CombatProfileCreatureAuditRow? creatureRow, ActionAuditCounts? counts, Creature2Entry? creatureEntry)
    {
        var signals = new List<string>();
        if ((creatureRow?.Creature2ActionSetId ?? 0u) != 0u)
            signals.Add("action-set");
        if ((counts?.UnknownActionRows ?? 0) != 0)
            signals.Add("unknown-action");
        if ((counts?.VisualOnlyActionRows ?? 0) != 0)
            signals.Add("visual-action");
        if ((creatureEntry?.SoundEventIdAggro ?? 0u) != 0u)
            signals.Add("aggro-sound");
        if ((creatureEntry?.SoundCombatLoopId ?? 0u) != 0u)
            signals.Add("combat-loop");

        return signals;
    }

    private static IReadOnlyList<CombatActionRuleCandidateRow> BuildCombatActionRuleCandidates(
        CombatProfileAuditDetails details,
        IReadOnlyList<SpawnPriorityRow> spawnPriorityRows,
        IGameTableManager gameTableManager,
        IReadOnlyDictionary<uint, CreatureSpellSignature> creatureSpellSignatures)
    {
        CombatActionAuditStatus[] candidateStatuses =
        [
            CombatActionAuditStatus.Unknown,
            CombatActionAuditStatus.RejectedRule,
            CombatActionAuditStatus.MissingSpell
        ];
        Dictionary<uint, CombatProfileCreatureAuditRow> creatureRows = details.CreatureRows.ToDictionary(row => row.Creature2Id);
        Dictionary<uint, SpawnPriorityRow> spawnRows = spawnPriorityRows.ToDictionary(row => row.Creature2Id);

        return details.ActionRows
            .Where(row => candidateStatuses.Contains(row.Status))
            .GroupBy(row => new CombatActionRuleCandidateKey(row.State, row.Event, row.Action))
            .Select(group => BuildCombatActionRuleCandidateRow(group.Key, group, creatureRows, spawnRows, gameTableManager, creatureSpellSignatures))
            .OrderByDescending(row => row.ActionData00SpellBridgeHitCount + row.ActionData01SpellBridgeHitCount)
            .ThenByDescending(row => row.RuntimeSpawnCount)
            .ThenByDescending(row => row.CreatureCount)
            .ThenByDescending(row => row.ActionData00Spell4HitCount + row.ActionData01Spell4HitCount)
            .ThenBy(row => row.State)
            .ThenBy(row => row.Event)
            .ThenBy(row => row.Action)
            .ToList();
    }

    private static CombatActionRuleCandidateRow BuildCombatActionRuleCandidateRow(
        CombatActionRuleCandidateKey key,
        IEnumerable<CombatProfileActionAuditRow> rows,
        IReadOnlyDictionary<uint, CombatProfileCreatureAuditRow> creatureRows,
        IReadOnlyDictionary<uint, SpawnPriorityRow> spawnRows,
        IGameTableManager gameTableManager,
        IReadOnlyDictionary<uint, CreatureSpellSignature> creatureSpellSignatures)
    {
        List<CombatProfileActionAuditRow> orderedRows = rows
            .OrderByDescending(row => spawnRows.TryGetValue(row.Creature2Id, out SpawnPriorityRow? spawnRow) ? spawnRow.SpawnCount : 0)
            .ThenBy(row => row.Creature2Id)
            .ThenBy(row => row.Creature2ActionId)
            .ToList();
        uint[] creatureIds = orderedRows.Select(row => row.Creature2Id).Distinct().ToArray();
        int mappedCreatureCount = creatureIds.Count(creature2Id => creatureRows.TryGetValue(creature2Id, out CombatProfileCreatureAuditRow? row) && row.Source != CombatProfileResolutionSource.None);
        int runtimeSpawnedCreatureCount = creatureIds.Count(creature2Id => spawnRows.TryGetValue(creature2Id, out SpawnPriorityRow? row) && row.SpawnCount > 0);
        int runtimeSpawnCount = creatureIds.Sum(creature2Id => spawnRows.TryGetValue(creature2Id, out SpawnPriorityRow? row) ? row.SpawnCount : 0);
        int actionData00Spell4HitCount = orderedRows.Count(row => IsSpell4Id(gameTableManager, row.ActionData00));
        int actionData01Spell4HitCount = orderedRows.Count(row => IsSpell4Id(gameTableManager, row.ActionData01));
        int actionData00SpellBridgeHitCount = orderedRows.Count(row => IsReviewedCreatureSpell(row.Creature2Id, row.ActionData00, creatureSpellSignatures));
        int actionData01SpellBridgeHitCount = orderedRows.Count(row => IsReviewedCreatureSpell(row.Creature2Id, row.ActionData01, creatureSpellSignatures));
        string dataShape = ClassifyActionDataShape(orderedRows, actionData00Spell4HitCount, actionData01Spell4HitCount, actionData00SpellBridgeHitCount, actionData01SpellBridgeHitCount);

        return new CombatActionRuleCandidateRow(
            key.State,
            key.Event,
            key.Action,
            string.Join(";", orderedRows.Select(row => row.Status.ToString()).Distinct(StringComparer.Ordinal).OrderBy(status => status, StringComparer.Ordinal)),
            orderedRows.Count,
            creatureIds.Length,
            creatureIds.Length - mappedCreatureCount,
            mappedCreatureCount,
            runtimeSpawnedCreatureCount,
            runtimeSpawnCount,
            orderedRows.Select(row => row.Creature2ActionSetId).Distinct().Count(),
            orderedRows.Select(row => row.Creature2ActionId).Distinct().Count(),
            orderedRows.Select(row => row.VisualEffectId).Where(id => id != 0u).Distinct().Count(),
            FormatIdList(orderedRows.Select(row => row.VisualEffectId).Where(id => id != 0u).Distinct().OrderBy(id => id).Take(16)),
            orderedRows.Count(row => row.ActionData00 == 0u && row.ActionData01 == 0u),
            orderedRows.Count(row => row.ActionData00 != 0u && row.ActionData00 == row.ActionData01),
            orderedRows.Count(row => row.ActionData00 != 0u),
            orderedRows.Select(row => row.ActionData00).Where(value => value != 0u).Distinct().Count(),
            actionData00Spell4HitCount,
            actionData00SpellBridgeHitCount,
            FormatActionDataValues(orderedRows.Select(row => row.ActionData00), gameTableManager),
            orderedRows.Count(row => row.ActionData01 != 0u),
            orderedRows.Select(row => row.ActionData01).Where(value => value != 0u).Distinct().Count(),
            actionData01Spell4HitCount,
            actionData01SpellBridgeHitCount,
            FormatActionDataValues(orderedRows.Select(row => row.ActionData01), gameTableManager),
            dataShape,
            FormatIdList(orderedRows.Select(row => row.Creature2ActionSetId).Distinct().OrderBy(id => id).Take(16)),
            FormatIdList(orderedRows.Select(row => row.Creature2ActionId).Distinct().OrderBy(id => id).Take(16)),
            FormatIdList(creatureIds.Take(16)),
            string.Join("; ", orderedRows
                .GroupBy(row => row.Creature2Id)
                .Select(group => group.First())
                .Take(16)
                .Select(row => GetCandidateSampleName(row.Creature2Id, row.CreatureName))),
            BuildActionRuleCandidateRecommendation(dataShape, actionData00SpellBridgeHitCount + actionData01SpellBridgeHitCount, actionData00Spell4HitCount + actionData01Spell4HitCount));
    }

    private static string ClassifyActionDataShape(
        IReadOnlyList<CombatProfileActionAuditRow> rows,
        int actionData00Spell4HitCount,
        int actionData01Spell4HitCount,
        int actionData00SpellBridgeHitCount,
        int actionData01SpellBridgeHitCount)
    {
        if (rows.All(row => row.ActionData00 == 0u && row.ActionData01 == 0u))
            return "empty-data";

        if (actionData00SpellBridgeHitCount != 0 || actionData01SpellBridgeHitCount != 0)
            return "reviewed-spell-bridge-overlap";

        if (actionData00Spell4HitCount != 0 || actionData01Spell4HitCount != 0)
            return "spell4-id-collision";

        if (rows.All(row => row.ActionData00 != 0u && row.ActionData00 == row.ActionData01))
            return "same-opaque-data-pair";

        return "opaque-data";
    }

    private static string BuildActionRuleCandidateRecommendation(string dataShape, int spellBridgeHitCount, int spell4HitCount)
    {
        if (spellBridgeHitCount != 0)
            return "evidence-needed:prove-action-data-spell-field";

        if (string.Equals(dataShape, "empty-data", StringComparison.Ordinal))
            return "diagnostic-only:classify-empty-action";

        if (spell4HitCount != 0)
            return "diagnostic-only:audit-spell4-id-collisions";

        return "diagnostic-only:classify-action-semantics";
    }

    private static bool IsSpell4Id(IGameTableManager gameTableManager, uint spell4Id)
    {
        return spell4Id != 0u && gameTableManager.Spell4?.GetEntry(spell4Id) != null;
    }

    private static bool IsReviewedCreatureSpell(uint creature2Id, uint spell4Id, IReadOnlyDictionary<uint, CreatureSpellSignature> creatureSpellSignatures)
    {
        return spell4Id != 0u
            && creatureSpellSignatures.TryGetValue(creature2Id, out CreatureSpellSignature? signature)
            && signature.Spell4Ids.Contains(spell4Id);
    }

    private static string FormatActionDataValues(IEnumerable<uint> values, IGameTableManager gameTableManager)
    {
        string[] parts = values
            .Where(value => value != 0u)
            .GroupBy(value => value)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Take(12)
            .Select(group => FormatActionDataValue(group.Key, group.Count(), gameTableManager))
            .ToArray();

        return string.Join("; ", parts);
    }

    private static string FormatActionDataValue(uint value, int count, IGameTableManager gameTableManager)
    {
        string spellName = GetSpellName(gameTableManager, value);
        string label = string.IsNullOrWhiteSpace(spellName)
            ? value.ToString(CultureInfo.InvariantCulture)
            : $"{value.ToString(CultureInfo.InvariantCulture)}:{spellName}";

        return count <= 1
            ? label
            : $"{label} x{count.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string GetSpellName(IGameTableManager gameTableManager, uint spell4Id)
    {
        Spell4Entry? spell = gameTableManager.Spell4?.GetEntry(spell4Id);
        if (spell == null)
            return string.Empty;

        Spell4BaseEntry? spellBase = gameTableManager.Spell4Base?.GetEntry(spell.Spell4BaseIdBaseSpell);
        string name = spellBase == null
            ? string.Empty
            : gameTableManager.TextEnglish?.GetEntry(spellBase.LocalizedTextIdName) ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        return spell.Description ?? string.Empty;
    }

    private static IReadOnlyList<CombatKitGroupCandidateRow> BuildCombatKitGroupCandidates(
        CombatProfileAuditDetails details,
        IReadOnlyList<SpawnPriorityRow> spawnPriorityRows,
        IGameTableManager gameTableManager,
        IReadOnlyDictionary<uint, CreatureSpellSignature> signatures)
    {
        if (signatures.Count == 0)
            return [];

        Dictionary<uint, CombatProfileCreatureAuditRow> auditRows = details.CreatureRows.ToDictionary(row => row.Creature2Id);
        Dictionary<uint, SpawnPriorityRow> spawnRows = spawnPriorityRows.ToDictionary(row => row.Creature2Id);
        Dictionary<CombatKitGroupSignal, int> signalCreatureCounts = CountSignalCreatureRows(gameTableManager);
        Dictionary<CombatKitGroupSignal, int> signalSpellBridgeCounts = CountSignalSpellBridgeCreatureRows(gameTableManager, signatures.Keys);

        Dictionary<CombatKitGroupCandidateKey, List<CreatureSpellSignature>> candidateGroups = [];
        foreach (CreatureSpellSignature signature in signatures.Values)
        {
            Creature2Entry creature = gameTableManager.Creature2.GetEntry(signature.Creature2Id);
            if (creature == null)
                continue;

            foreach (CombatKitGroupSignal signal in GetCombatKitGroupSignals(creature))
            {
                var key = new CombatKitGroupCandidateKey(signal, signature.SignatureKey);
                if (!candidateGroups.TryGetValue(key, out List<CreatureSpellSignature>? group))
                {
                    group = [];
                    candidateGroups[key] = group;
                }

                group.Add(signature);
            }
        }

        return candidateGroups
            .Select(group => BuildCombatKitGroupCandidateRow(group.Key, group.Value, auditRows, spawnRows, signalCreatureCounts, signalSpellBridgeCounts))
            .Where(row => row.CandidateCreatureCount >= 2 && row.CandidateUnmappedCreatureCount > 0)
            .OrderByDescending(row => row.CandidateUnmappedCreatureCount)
            .ThenByDescending(row => row.CandidateRuntimeSpawnCount)
            .ThenByDescending(row => row.CandidateCreatureCount)
            .ThenBy(row => GetSignalPriority(row.GroupType))
            .ThenBy(row => row.GroupId)
            .ThenBy(row => row.Spell4Ids, StringComparer.Ordinal)
            .ToList();
    }

    private static List<CreatureSpellMapRow> LoadCreatureSpellMap(string path)
    {
        var rows = new List<CreatureSpellMapRow>();
        using var parser = new TextFieldParser(path, Encoding.UTF8)
        {
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false
        };
        parser.SetDelimiters(",");

        string[] headers = parser.ReadFields() ?? [];
        Dictionary<string, int> headerMap = headers
            .Select((header, index) => new { Header = header, Index = index })
            .ToDictionary(item => item.Header, item => item.Index, StringComparer.OrdinalIgnoreCase);

        while (!parser.EndOfData)
        {
            string[] fields = parser.ReadFields() ?? [];
            rows.Add(new CreatureSpellMapRow(
                ParseUIntOrZero(GetCsvField(headerMap, fields, "creature2_id")),
                GetCsvField(headerMap, fields, "creature2_name"),
                GetCsvField(headerMap, fields, "match_status"),
                ParseUIntOrZero(GetCsvField(headerMap, fields, "spell4_id")),
                GetCsvField(headerMap, fields, "spell_name")));
        }

        return rows;
    }

    private static IReadOnlyDictionary<uint, CreatureSpellSignature> LoadReviewedCreatureSpellSignatures(string creatureSpellMapPath, IGameTableManager gameTableManager)
    {
        List<CreatureSpellMapRow> spellMapRows = LoadCreatureSpellMap(creatureSpellMapPath)
            .Where(row => IsReviewedOrUniqueSpellMapRow(row))
            .Where(row => row.Creature2Id != 0u && row.Spell4Id != 0u)
            .Where(row => gameTableManager.Spell4?.GetEntry(row.Spell4Id) != null)
            .ToList();

        return spellMapRows.Count == 0
            ? new Dictionary<uint, CreatureSpellSignature>()
            : BuildCreatureSpellSignatures(spellMapRows);
    }

    private static Dictionary<uint, CreatureSpellSignature> BuildCreatureSpellSignatures(IEnumerable<CreatureSpellMapRow> rows)
    {
        return rows
            .GroupBy(row => row.Creature2Id)
            .Select(group =>
            {
                uint[] spell4Ids = group
                    .Select(row => row.Spell4Id)
                    .Distinct()
                    .OrderBy(spell4Id => spell4Id)
                    .ToArray();
                string[] spellNames = group
                    .GroupBy(row => row.Spell4Id)
                    .OrderBy(spellGroup => spellGroup.Key)
                    .Select(spellGroup =>
                    {
                        string name = spellGroup.Select(row => row.SpellName).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
                        return string.IsNullOrWhiteSpace(name)
                            ? spellGroup.Key.ToString(CultureInfo.InvariantCulture)
                            : $"{spellGroup.Key.ToString(CultureInfo.InvariantCulture)}:{name}";
                    })
                    .ToArray();
                string[] matchStatuses = group
                    .Select(row => row.MatchStatus)
                    .Where(status => !string.IsNullOrWhiteSpace(status))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(status => status, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                string creatureName = group.Select(row => row.CreatureName).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

                return new CreatureSpellSignature(
                    group.Key,
                    creatureName,
                    spell4Ids,
                    spellNames,
                    matchStatuses,
                    string.Join(";", spell4Ids.Select(spell4Id => spell4Id.ToString(CultureInfo.InvariantCulture))));
            })
            .Where(signature => signature.Spell4Ids.Count != 0)
            .ToDictionary(signature => signature.Creature2Id);
    }

    private static Dictionary<CombatKitGroupSignal, int> CountSignalCreatureRows(IGameTableManager gameTableManager)
    {
        Dictionary<CombatKitGroupSignal, int> counts = [];
        foreach (Creature2Entry creature in gameTableManager.Creature2.Entries ?? [])
        {
            foreach (CombatKitGroupSignal signal in GetCombatKitGroupSignals(creature))
                counts[signal] = counts.GetValueOrDefault(signal) + 1;
        }

        return counts;
    }

    private static Dictionary<CombatKitGroupSignal, int> CountSignalSpellBridgeCreatureRows(IGameTableManager gameTableManager, IEnumerable<uint> creature2Ids)
    {
        Dictionary<CombatKitGroupSignal, HashSet<uint>> groups = [];
        foreach (uint creature2Id in creature2Ids)
        {
            Creature2Entry creature = gameTableManager.Creature2.GetEntry(creature2Id);
            if (creature == null)
                continue;

            foreach (CombatKitGroupSignal signal in GetCombatKitGroupSignals(creature))
            {
                if (!groups.TryGetValue(signal, out HashSet<uint>? group))
                {
                    group = [];
                    groups[signal] = group;
                }

                group.Add(creature2Id);
            }
        }

        return groups.ToDictionary(pair => pair.Key, pair => pair.Value.Count);
    }

    private static CombatKitGroupCandidateRow BuildCombatKitGroupCandidateRow(
        CombatKitGroupCandidateKey key,
        IReadOnlyList<CreatureSpellSignature> signatures,
        IReadOnlyDictionary<uint, CombatProfileCreatureAuditRow> auditRows,
        IReadOnlyDictionary<uint, SpawnPriorityRow> spawnRows,
        IReadOnlyDictionary<CombatKitGroupSignal, int> signalCreatureCounts,
        IReadOnlyDictionary<CombatKitGroupSignal, int> signalSpellBridgeCounts)
    {
        List<CreatureSpellSignature> orderedSignatures = signatures
            .OrderByDescending(signature => spawnRows.TryGetValue(signature.Creature2Id, out SpawnPriorityRow? spawnRow) ? spawnRow.SpawnCount : 0)
            .ThenBy(signature => signature.Creature2Id)
            .ToList();
        int mappedCount = signatures.Count(signature => auditRows.TryGetValue(signature.Creature2Id, out CombatProfileCreatureAuditRow? row) && row.Source != CombatProfileResolutionSource.None);
        int spawnedCount = signatures.Count(signature => spawnRows.TryGetValue(signature.Creature2Id, out SpawnPriorityRow? row) && row.SpawnCount > 0);
        int runtimeSpawnCount = signatures.Sum(signature => spawnRows.TryGetValue(signature.Creature2Id, out SpawnPriorityRow? row) ? row.SpawnCount : 0);
        string[] mappedKitIds = signatures
            .Select(signature => auditRows.TryGetValue(signature.Creature2Id, out CombatProfileCreatureAuditRow? row) ? row.KitId : string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        CreatureSpellSignature firstSignature = orderedSignatures[0];

        return new CombatKitGroupCandidateRow(
            key.Signal.Type,
            key.Signal.Id,
            signalCreatureCounts.GetValueOrDefault(key.Signal),
            signalSpellBridgeCounts.GetValueOrDefault(key.Signal),
            signatures.Count,
            signatures.Count - mappedCount,
            mappedCount,
            spawnedCount,
            runtimeSpawnCount,
            firstSignature.Spell4Ids.Count,
            string.Join(";", firstSignature.Spell4Ids.Select(spell4Id => spell4Id.ToString(CultureInfo.InvariantCulture))),
            string.Join("; ", firstSignature.SpellNames),
            string.Join(";", signatures.SelectMany(signature => signature.MatchStatuses).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(status => status, StringComparer.OrdinalIgnoreCase)),
            string.Join(";", mappedKitIds),
            string.Join(";", orderedSignatures.Take(12).Select(signature => signature.Creature2Id.ToString(CultureInfo.InvariantCulture))),
            string.Join("; ", orderedSignatures.Take(12).Select(GetCandidateSampleName)),
            BuildCombatKitGroupRecommendation(mappedKitIds));
    }

    private static string BuildCombatKitGroupRecommendation(IReadOnlyList<string> mappedKitIds)
    {
        if (mappedKitIds.Count == 1)
            return $"review-propagate-existing-kit:{mappedKitIds[0]}";

        if (mappedKitIds.Count > 1)
            return "review-existing-kit-conflict";

        return "review-create-shared-kit";
    }

    private static string GetCandidateSampleName(CreatureSpellSignature signature)
    {
        return string.IsNullOrWhiteSpace(signature.CreatureName)
            ? signature.Creature2Id.ToString(CultureInfo.InvariantCulture)
            : $"{signature.Creature2Id.ToString(CultureInfo.InvariantCulture)} {signature.CreatureName}";
    }

    private static string GetCandidateSampleName(uint creature2Id, string creatureName)
    {
        return string.IsNullOrWhiteSpace(creatureName)
            ? creature2Id.ToString(CultureInfo.InvariantCulture)
            : $"{creature2Id.ToString(CultureInfo.InvariantCulture)} {creatureName}";
    }

    private static IEnumerable<CombatKitGroupSignal> GetCombatKitGroupSignals(Creature2Entry creature)
    {
        if (creature.UnitRaceId != 0u)
            yield return new CombatKitGroupSignal("UnitRaceId", creature.UnitRaceId);
        if (creature.Creature2FamilyId != 0u)
            yield return new CombatKitGroupSignal("Creature2FamilyId", creature.Creature2FamilyId);
        if (creature.Creature2TractId != 0u)
            yield return new CombatKitGroupSignal("Creature2TractId", creature.Creature2TractId);
        if (creature.Creature2AffiliationId != 0u)
            yield return new CombatKitGroupSignal("Creature2AffiliationId", creature.Creature2AffiliationId);
        if (creature.FactionId != 0u)
            yield return new CombatKitGroupSignal("FactionId", creature.FactionId);
        if (creature.Creature2ActionSetId != 0u)
            yield return new CombatKitGroupSignal("Creature2ActionSetId", creature.Creature2ActionSetId);
    }

    private static bool IsReviewedOrUniqueSpellMapRow(CreatureSpellMapRow row)
    {
        return string.Equals(row.MatchStatus, "reviewed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(row.MatchStatus, "unique_name", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetCsvField(IReadOnlyDictionary<string, int> headerMap, string[] fields, string name)
    {
        return headerMap.TryGetValue(name, out int index) && index >= 0 && index < fields.Length
            ? fields[index]
            : string.Empty;
    }

    private static uint ParseUIntOrZero(string? value)
    {
        return uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out uint result)
            ? result
            : 0u;
    }

    private static int GetSignalPriority(string groupType)
    {
        return groupType switch
        {
            "UnitRaceId"              => 0,
            "Creature2FamilyId"       => 1,
            "Creature2TractId"        => 2,
            "Creature2AffiliationId"  => 3,
            "FactionId"               => 4,
            "Creature2ActionSetId"    => 5,
            _                         => 6
        };
    }

    private static string FormatIdList(IEnumerable<ushort> values)
    {
        return string.Join(";", values.Select(value => value.ToString(CultureInfo.InvariantCulture)));
    }

    private static string FormatIdList(IEnumerable<uint> values)
    {
        return string.Join(";", values.Select(value => value.ToString(CultureInfo.InvariantCulture)));
    }

    private static string Csv(params object?[] values)
    {
        return string.Join(",", values.Select(EscapeCsv));
    }

    private static string EscapeCsv(object? value)
    {
        string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        if (!text.Contains(',') && !text.Contains('"') && !text.Contains('\r') && !text.Contains('\n'))
            return text;

        return "\"" + text.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static string EscapeMarkdown(string? value)
    {
        return (value ?? string.Empty).Replace("|", "\\|", StringComparison.Ordinal);
    }

    private sealed record AuditOptions(
        string GameTablePath,
        string OutputDirectory,
        int MaxReviewRows,
        bool ShowHelp,
        string WorldConfigPath,
        string WorldConnectionString,
        string CreatureSpellMapPath)
    {
        public static AuditOptions Parse(IReadOnlyList<string> args)
        {
            if (args.Count == 0 || args.Any(arg => arg is "-h" or "--help" or "/?"))
                return new AuditOptions(string.Empty, string.Empty, DefaultMaxReviewRows, true, string.Empty, string.Empty, string.Empty);

            string? gameTablePath = null;
            string outputDirectory = Path.GetFullPath(Path.Combine("artifacts", "combat-profile-audit"));
            int maxReviewRows = DefaultMaxReviewRows;
            string? worldConfigPath = null;
            string? worldConnectionString = null;
            string? creatureSpellMapPath = null;

            for (int i = 0; i < args.Count; i++)
            {
                string arg = args[i];
                if (arg == "--output-dir")
                {
                    outputDirectory = Path.GetFullPath(RequireValue(args, ref i, arg));
                    continue;
                }

                if (arg.StartsWith("--output-dir=", StringComparison.OrdinalIgnoreCase))
                {
                    outputDirectory = Path.GetFullPath(arg["--output-dir=".Length..]);
                    continue;
                }

                if (arg == "--max-review-rows")
                {
                    maxReviewRows = ParsePositiveInt(RequireValue(args, ref i, arg), arg);
                    continue;
                }

                if (arg.StartsWith("--max-review-rows=", StringComparison.OrdinalIgnoreCase))
                {
                    maxReviewRows = ParsePositiveInt(arg["--max-review-rows=".Length..], "--max-review-rows");
                    continue;
                }

                if (arg == "--world-config")
                {
                    worldConfigPath = Path.GetFullPath(RequireValue(args, ref i, arg));
                    continue;
                }

                if (arg.StartsWith("--world-config=", StringComparison.OrdinalIgnoreCase))
                {
                    worldConfigPath = Path.GetFullPath(arg["--world-config=".Length..]);
                    continue;
                }

                if (arg == "--world-connection")
                {
                    worldConnectionString = RequireValue(args, ref i, arg);
                    continue;
                }

                if (arg.StartsWith("--world-connection=", StringComparison.OrdinalIgnoreCase))
                {
                    worldConnectionString = arg["--world-connection=".Length..];
                    continue;
                }

                if (arg == "--creature-spell-map")
                {
                    creatureSpellMapPath = Path.GetFullPath(RequireValue(args, ref i, arg));
                    continue;
                }

                if (arg.StartsWith("--creature-spell-map=", StringComparison.OrdinalIgnoreCase))
                {
                    creatureSpellMapPath = Path.GetFullPath(arg["--creature-spell-map=".Length..]);
                    continue;
                }

                if (arg.StartsWith("-", StringComparison.Ordinal))
                    throw new ArgumentException($"Unknown option: {arg}");

                if (gameTablePath != null)
                    throw new ArgumentException("Only one game-table path can be supplied.");

                gameTablePath = Path.GetFullPath(arg);
            }

            if (string.IsNullOrWhiteSpace(gameTablePath) && string.IsNullOrWhiteSpace(worldConfigPath))
                throw new ArgumentException("Missing game-table path.");

            return new AuditOptions(gameTablePath ?? string.Empty, outputDirectory, maxReviewRows, false, worldConfigPath ?? string.Empty, worldConnectionString ?? string.Empty, creatureSpellMapPath ?? string.Empty);
        }

        public AuditOptions ResolveRuntimeConfig()
        {
            if (string.IsNullOrWhiteSpace(WorldConfigPath))
                return this;

            if (!File.Exists(WorldConfigPath))
                throw new ArgumentException($"World config was not found: {WorldConfigPath}");

            RuntimeConfig config = RuntimeConfig.Load(WorldConfigPath);
            string gameTablePath = GameTablePath;
            if (string.IsNullOrWhiteSpace(gameTablePath) && !string.IsNullOrWhiteSpace(config.GameTablePath))
                gameTablePath = ResolveConfigRelativePath(WorldConfigPath, config.GameTablePath);

            string worldConnectionString = WorldConnectionString;
            if (string.IsNullOrWhiteSpace(worldConnectionString) && !string.IsNullOrWhiteSpace(config.WorldConnectionString))
                worldConnectionString = config.WorldConnectionString;

            if (!string.IsNullOrWhiteSpace(worldConnectionString)
                && !string.IsNullOrWhiteSpace(config.WorldProvider)
                && !string.Equals(config.WorldProvider, nameof(DatabaseProvider.MySql), StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"World config provider is not supported by this audit tool: {config.WorldProvider}");

            if (string.IsNullOrWhiteSpace(gameTablePath))
                throw new ArgumentException("World config did not contain GameTable:GameTablePath; supply a game-table path explicitly.");

            return this with
            {
                GameTablePath = gameTablePath,
                WorldConnectionString = worldConnectionString
            };
        }

        public AuditOptions ResolveInputPaths()
        {
            if (!string.IsNullOrWhiteSpace(CreatureSpellMapPath))
            {
                if (!File.Exists(CreatureSpellMapPath))
                    throw new ArgumentException($"Creature spell map was not found: {CreatureSpellMapPath}");

                return this;
            }

            string defaultPath = Path.GetFullPath(DefaultCreatureSpellMapPath);
            return File.Exists(defaultPath)
                ? this with { CreatureSpellMapPath = defaultPath }
                : this;
        }

        private static string RequireValue(IReadOnlyList<string> args, ref int index, string optionName)
        {
            if (index + 1 >= args.Count)
                throw new ArgumentException($"Missing value for {optionName}.");

            index++;
            return args[index];
        }

        private static int ParsePositiveInt(string value, string optionName)
        {
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int result) || result <= 0)
                throw new ArgumentException($"{optionName} must be a positive integer.");

            return result;
        }

        private static string ResolveConfigRelativePath(string configPath, string value)
        {
            if (Path.IsPathRooted(value))
                return Path.GetFullPath(value);

            string configDirectory = Path.GetDirectoryName(configPath) ?? Directory.GetCurrentDirectory();
            return Path.GetFullPath(Path.Combine(configDirectory, value));
        }
    }

    private sealed record RuntimeConfig(string GameTablePath, string WorldConnectionString, string WorldProvider)
    {
        public static RuntimeConfig Load(string path)
        {
            using FileStream stream = File.OpenRead(path);
            using JsonDocument document = JsonDocument.Parse(stream);
            JsonElement root = document.RootElement;

            return new RuntimeConfig(
                TryGetString(root, "GameTable", "GameTablePath") ?? string.Empty,
                TryGetString(root, "Database", "World", "ConnectionString") ?? string.Empty,
                TryGetString(root, "Database", "World", "Provider") ?? string.Empty);
        }

        private static string? TryGetString(JsonElement element, params string[] path)
        {
            JsonElement current = element;
            foreach (string part in path)
            {
                if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(part, out current))
                    return null;
            }

            return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
        }
    }

    private sealed record WorldSpawnFact(uint Creature2Id, ushort World, ushort Area);
    private sealed record WorldSpawnSummary(uint Creature2Id, int SpawnCount, IReadOnlyList<ushort> Worlds, IReadOnlyList<ushort> Areas);
    private sealed record ActionAuditCounts(int UnknownActionRows, int VisualOnlyActionRows, int ReviewActionRows);
    private readonly record struct CombatKitGroupSignal(string Type, uint Id);
    private readonly record struct CombatKitGroupCandidateKey(CombatKitGroupSignal Signal, string SignatureKey);
    private readonly record struct CombatActionRuleCandidateKey(uint State, uint Event, uint Action);
    private sealed record CreatureSpellMapRow(uint Creature2Id, string CreatureName, string MatchStatus, uint Spell4Id, string SpellName);
    private sealed record CreatureSpellSignature(
        uint Creature2Id,
        string CreatureName,
        IReadOnlyList<uint> Spell4Ids,
        IReadOnlyList<string> SpellNames,
        IReadOnlyList<string> MatchStatuses,
        string SignatureKey);
    private sealed record CombatKitGroupCandidateRow(
        string GroupType,
        uint GroupId,
        int GroupCreatureCount,
        int GroupSpellBridgeCreatureCount,
        int CandidateCreatureCount,
        int CandidateUnmappedCreatureCount,
        int CandidateMappedCreatureCount,
        int CandidateSpawnedCreatureCount,
        int CandidateRuntimeSpawnCount,
        int SpellCount,
        string Spell4Ids,
        string SpellNames,
        string MatchStatuses,
        string MappedKitIds,
        string SampleCreature2Ids,
        string SampleCreatureNames,
        string Recommendation);
    private sealed record CombatActionRuleCandidateRow(
        uint State,
        uint Event,
        uint Action,
        string Statuses,
        int CreatureRowCount,
        int CreatureCount,
        int UnmappedCreatureCount,
        int MappedCreatureCount,
        int RuntimeSpawnedCreatureCount,
        int RuntimeSpawnCount,
        int ActionSetCount,
        int ActionRowCount,
        int VisualEffectCount,
        string VisualEffectIds,
        int ZeroDataCount,
        int SameDataPairCount,
        int ActionData00NonZeroCount,
        int ActionData00DistinctCount,
        int ActionData00Spell4HitCount,
        int ActionData00SpellBridgeHitCount,
        string ActionData00Values,
        int ActionData01NonZeroCount,
        int ActionData01DistinctCount,
        int ActionData01Spell4HitCount,
        int ActionData01SpellBridgeHitCount,
        string ActionData01Values,
        string DataShape,
        string SampleActionSets,
        string SampleActionRowIds,
        string SampleCreature2Ids,
        string SampleCreatureNames,
        string Recommendation);
    private sealed record SpawnPriorityRow(
        uint Creature2Id,
        string CreatureName,
        CombatProfileResolutionSource Source,
        string ProfileId,
        string KitId,
        uint Creature2ActionSetId,
        int SpawnCount,
        string Worlds,
        string Areas,
        int UnknownActionRows,
        int VisualOnlyActionRows,
        int ReviewActionRows,
        int CombatSignalCount,
        string PrioritySignals,
        uint Creature2DifficultyId,
        uint Creature2ArcheTypeId,
        uint Creature2TierId,
        uint SoundEventIdAggro,
        uint SoundCombatLoopId,
        int AutoAttackCount,
        int SpecialAttackCount,
        string Diagnostics);
}
