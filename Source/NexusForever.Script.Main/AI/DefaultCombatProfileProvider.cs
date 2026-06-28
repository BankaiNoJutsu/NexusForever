using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Script.Main.AI
{
    public sealed class DefaultCombatProfileProvider : IDefaultCombatProfileProvider, ICombatProfileResolutionProvider
    {
        private const string ManualProfileResourceName = "NexusForever.Script.Main.AI.CombatProfiles.json";
        private const string CombatKitResourceName = "NexusForever.Script.Main.AI.CombatKits.json";
        private const string CombatActionRulesResourceName = "NexusForever.Script.Main.AI.CombatActionRules.json";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

        private static readonly ConditionalWeakTable<IGameTableManager, DefaultCombatProfileProvider> GameTableProviders = new();
        private static readonly ManualCombatProfileData ManualProfiles = LoadManualProfileData();
        private static readonly CombatKitCatalogData CombatKits = LoadCombatKitCatalogData(ManualProfiles.DefaultProfile);
        private static readonly CombatActionRuleData CombatActionRules = LoadCombatActionRuleData();

        public static DefaultCombatProfileProvider Instance { get; } = new(null);

        private readonly CreatureActionCombatProfileResolver actionResolver;
        private readonly CombatProfileAudit audit;
        private readonly CombatProfileAuditDetails auditDetails;

        private DefaultCombatProfileProvider(IGameTableManager gameTableManager, CombatActionRuleData actionRuleData = null)
        {
            actionResolver = new CreatureActionCombatProfileResolver(gameTableManager, actionRuleData ?? CombatActionRules, ManualProfiles.DefaultProfile);
            int manualOverrideCount = ManualProfiles.CreatureProfiles.Count;
            int reviewedKitCount = CombatKits.CreatureProfiles.Count;
            int actionDerivedCount = actionResolver.Audit.ActionDerivedProfileCount;
            int mappedCreatureCount = CountMappedCreatureRows(gameTableManager, actionResolver.ResolvedCreatureIds);
            audit = new CombatProfileAudit(
                manualOverrideCount,
                reviewedKitCount,
                actionDerivedCount,
                actionResolver.Audit.ActionVisualOnlyRowCount,
                actionResolver.Audit.ActionIgnoredRuleRowCount,
                actionResolver.Audit.ActionRejectedRuleRowCount,
                actionResolver.Audit.ActionUnknownRowCount,
                actionResolver.Audit.ActionMissingSpellRowCount,
                mappedCreatureCount,
                CountUnmappedCreatureRows(gameTableManager, actionResolver.ResolvedCreatureIds));
            auditDetails = new CombatProfileAuditDetails(
                audit,
                BuildCreatureAuditRows(gameTableManager),
                actionResolver.ActionRows);
        }

        public static DefaultCombatProfileProvider ForGameTables(IGameTableManager gameTableManager)
        {
            return gameTableManager == null
                ? Instance
                : GameTableProviders.GetValue(gameTableManager, manager => new DefaultCombatProfileProvider(manager));
        }

        public static DefaultCombatProfileProvider ForGameTables(IGameTableManager gameTableManager, IReadOnlyList<CombatActionRule> actionRules)
        {
            return new DefaultCombatProfileProvider(gameTableManager, new CombatActionRuleData(actionRules ?? []));
        }

        public CombatProfile GetProfile(ICreatureEntity creature)
        {
            return GetResolution(creature).Profile;
        }

        public CombatProfileResolution GetResolution(ICreatureEntity creature)
        {
            uint creature2Id = creature?.CreatureId ?? 0u;
            return GetResolution(creature2Id);
        }

        public CombatProfileResolution GetResolution(uint creature2Id, uint creature2ActionSetId = 0u)
        {
            if (creature2Id == 0u)
                return CombatProfileResolution.None(creature2Id, creature2ActionSetId);

            if (ManualProfiles.CreatureProfiles.TryGetValue(creature2Id, out ResolvedCombatProfile manualProfile))
            {
                return new CombatProfileResolution(
                    CombatProfileResolutionSource.ManualOverride,
                    manualProfile.Profile,
                    creature2Id,
                    ProfileId: manualProfile.Id,
                    Diagnostics: manualProfile.Diagnostics);
            }

            if (CombatKits.CreatureProfiles.TryGetValue(creature2Id, out ResolvedCombatProfile kitProfile))
            {
                return new CombatProfileResolution(
                    CombatProfileResolutionSource.ReviewedKitMapping,
                    kitProfile.Profile,
                    creature2Id,
                    KitId: kitProfile.Id,
                    Diagnostics: kitProfile.Diagnostics);
            }

            CombatProfileResolution actionResolution = actionResolver.GetResolution(creature2Id);
            return actionResolution ?? CombatProfileResolution.None(creature2Id, creature2ActionSetId);
        }

        public CombatProfile GetDefaultProfile()
        {
            return ManualProfiles.DefaultProfile;
        }

        public CombatProfileAudit GetAudit()
        {
            return audit;
        }

        public CombatProfileAuditDetails GetAuditDetails()
        {
            return auditDetails;
        }

        private static ManualCombatProfileData LoadManualProfileData()
        {
            CombatProfileDataFile data = LoadResource<CombatProfileDataFile>(ManualProfileResourceName);
            if (data?.Profiles == null || data.Creatures == null)
                return ManualCombatProfileData.Empty;

            Dictionary<string, CombatProfileDefinition> profileDefinitions = data.Profiles
                .Where(profile => !string.IsNullOrWhiteSpace(profile.Name))
                .ToDictionary(profile => profile.Name, StringComparer.OrdinalIgnoreCase);

            CombatProfile defaultProfile = CombatProfile.Default;
            if (!string.IsNullOrWhiteSpace(data.DefaultProfile) && profileDefinitions.TryGetValue(data.DefaultProfile, out CombatProfileDefinition defaultDefinition))
                defaultProfile = ToCombatProfile(defaultDefinition, CombatProfile.Default);

            Dictionary<string, CombatProfile> profiles = profileDefinitions
                .ToDictionary(pair => pair.Key, pair => ToCombatProfile(pair.Value, defaultProfile), StringComparer.OrdinalIgnoreCase);

            Dictionary<uint, ResolvedCombatProfile> creatureProfiles = [];
            foreach (CreatureProfileDefinition creature in data.Creatures)
            {
                if (creature.Creature2Id == 0u || string.IsNullOrWhiteSpace(creature.Profile))
                    continue;

                if (!profiles.TryGetValue(creature.Profile, out CombatProfile profile))
                    continue;

                creatureProfiles[creature.Creature2Id] = new ResolvedCombatProfile(profile, creature.Profile, BuildDiagnostics(creature.Source, creature.Evidence, creature.Note));
            }

            return new ManualCombatProfileData(creatureProfiles, defaultProfile);
        }

        private static CombatKitCatalogData LoadCombatKitCatalogData(CombatProfile defaultProfile)
        {
            CombatKitCatalogFile data = LoadResource<CombatKitCatalogFile>(CombatKitResourceName);
            if (data?.Kits == null || data.Creatures == null)
                return CombatKitCatalogData.Empty;

            Dictionary<string, CombatBehaviorDefinition> behaviors = data.Behaviors
                .Where(behavior => !string.IsNullOrWhiteSpace(behavior.Id))
                .ToDictionary(behavior => behavior.Id, StringComparer.OrdinalIgnoreCase);

            Dictionary<string, ResolvedCombatProfile> kits = [];
            foreach (CombatKitDefinition kit in data.Kits.Where(kit => !string.IsNullOrWhiteSpace(kit.Id)))
            {
                CombatProfile profile = defaultProfile;
                if (!string.IsNullOrWhiteSpace(kit.Behavior) && behaviors.TryGetValue(kit.Behavior, out CombatBehaviorDefinition behavior))
                    profile = ToCombatProfile(behavior, profile);

                profile = ToCombatProfile(kit, profile);
                kits[kit.Id] = new ResolvedCombatProfile(profile, kit.Id, BuildDiagnostics(kit.Source, kit.Evidence, kit.Note));
            }

            Dictionary<uint, ResolvedCombatProfile> creatureProfiles = [];
            foreach (CreatureCombatKitMappingDefinition creature in data.Creatures)
            {
                if (creature.Creature2Id == 0u || string.IsNullOrWhiteSpace(creature.Kit))
                    continue;

                if (!kits.TryGetValue(creature.Kit, out ResolvedCombatProfile kit))
                    continue;

                creatureProfiles[creature.Creature2Id] = new ResolvedCombatProfile(
                    kit.Profile,
                    kit.Id,
                    BuildDiagnostics(creature.Source, creature.Evidence, creature.Note));
            }

            return new CombatKitCatalogData(creatureProfiles);
        }

        private static CombatActionRuleData LoadCombatActionRuleData()
        {
            CombatActionRuleFile data = LoadResource<CombatActionRuleFile>(CombatActionRulesResourceName);
            return new CombatActionRuleData(data?.Rules ?? []);
        }

        private static T LoadResource<T>(string resourceName)
        {
            using Stream stream = typeof(DefaultCombatProfileProvider).Assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                return default;

            return JsonSerializer.Deserialize<T>(stream, JsonOptions);
        }

        private static CombatProfile ToCombatProfile(CombatProfileFieldsDefinition definition, CombatProfile fallback)
        {
            if (definition == null)
                return fallback;

            return fallback with
            {
                AutoAttackSpell4Ids   = definition.AutoAttackSpell4Ids ?? fallback.AutoAttackSpell4Ids,
                SpecialAttacks        = definition.SpecialAttacks?.Select(ToCombatSpecialAttack).ToList() ?? fallback.SpecialAttacks,
                AggroSpell4Id         = definition.AggroSpell4Id ?? fallback.AggroSpell4Id,
                ChaseDistance         = definition.ChaseDistance ?? fallback.ChaseDistance,
                AggroRange            = definition.AggroRange ?? fallback.AggroRange,
                MinimumLeashRange     = definition.MinimumLeashRange ?? fallback.MinimumLeashRange,
                AssistRange           = definition.AssistRange ?? fallback.AssistRange,
                Stationary            = definition.Stationary ?? fallback.Stationary,
                TraceCombat           = definition.TraceCombat ?? fallback.TraceCombat,
                AllowNonPlayerTargets      = definition.AllowNonPlayerTargets ?? fallback.AllowNonPlayerTargets,
                AssistSummoner             = definition.AssistSummoner ?? fallback.AssistSummoner,
                SummonerAssistRange        = definition.SummonerAssistRange ?? fallback.SummonerAssistRange,
                SummonerFollowDistance     = definition.SummonerFollowDistance ?? fallback.SummonerFollowDistance,
                SummonerFollowRepathDistance = definition.SummonerFollowRepathDistance ?? fallback.SummonerFollowRepathDistance,
                SummonerTierSourceBaseSpell4Id = definition.SummonerTierSourceBaseSpell4Id ?? fallback.SummonerTierSourceBaseSpell4Id,
                SummonerTieredAutoAttackBaseSpell4Id = definition.SummonerTieredAutoAttackBaseSpell4Id ?? fallback.SummonerTieredAutoAttackBaseSpell4Id
            };
        }

        private static CombatSpecialAttack ToCombatSpecialAttack(CombatSpecialAttackDefinition definition)
        {
            return new CombatSpecialAttack(
                definition.Spell4Id,
                definition.CooldownSeconds,
                definition.MaxRange,
                definition.FaceTarget ?? true,
                definition.Weight ?? 1d);
        }

        private static IReadOnlyList<string> BuildDiagnostics(params string[] values)
        {
            List<string> diagnostics = values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();

            return diagnostics.Count == 0 ? [] : diagnostics;
        }

        private static int CountMappedCreatureRows(IGameTableManager gameTableManager, IEnumerable<uint> actionResolvedCreatureIds)
        {
            if (gameTableManager?.Creature2?.Entries == null)
                return ManualProfiles.CreatureProfiles.Count + CombatKits.CreatureProfiles.Count + actionResolvedCreatureIds.Count();

            HashSet<uint> mappedCreatureIds = ManualProfiles.CreatureProfiles.Keys
                .Concat(CombatKits.CreatureProfiles.Keys)
                .Concat(actionResolvedCreatureIds)
                .ToHashSet();

            return gameTableManager.Creature2.Entries
                .Count(creature => creature != null && mappedCreatureIds.Contains(creature.Id));
        }

        private static int CountUnmappedCreatureRows(IGameTableManager gameTableManager, IEnumerable<uint> actionResolvedCreatureIds)
        {
            if (gameTableManager?.Creature2?.Entries == null)
                return 0;

            HashSet<uint> mappedCreatureIds = ManualProfiles.CreatureProfiles.Keys
                .Concat(CombatKits.CreatureProfiles.Keys)
                .Concat(actionResolvedCreatureIds)
                .ToHashSet();

            return gameTableManager.Creature2.Entries
                .Count(creature => creature != null && !mappedCreatureIds.Contains(creature.Id));
        }

        private IReadOnlyList<CombatProfileCreatureAuditRow> BuildCreatureAuditRows(IGameTableManager gameTableManager)
        {
            if (gameTableManager?.Creature2?.Entries == null)
                return [];

            return gameTableManager.Creature2.Entries
                .Where(creature => creature != null)
                .OrderBy(creature => creature.Id)
                .Select(creature =>
                {
                    CombatProfileResolution resolution = GetResolution(creature.Id, creature.Creature2ActionSetId);
                    return new CombatProfileCreatureAuditRow(
                        creature.Id,
                        GetCreatureName(gameTableManager, creature),
                        creature.Creature2ActionSetId,
                        resolution.Source,
                        resolution.ProfileId,
                        resolution.KitId,
                        resolution.Profile?.AutoAttackSpell4Ids?.Count ?? 0,
                        resolution.Profile?.SpecialAttacks?.Count ?? 0,
                        resolution.Diagnostics ?? []);
                })
                .ToList();
        }

        private static string GetCreatureName(IGameTableManager gameTableManager, Creature2Entry creature)
        {
            if (creature == null)
                return null;

            string name = gameTableManager?.TextEnglish?.GetEntry(creature.LocalizedTextIdName);
            if (!string.IsNullOrWhiteSpace(name))
                return name;

            return string.IsNullOrWhiteSpace(creature.Description) ? null : creature.Description;
        }

        private sealed class CreatureActionCombatProfileResolver
        {
            private readonly Dictionary<uint, CombatProfileResolution> resolutions = [];
            private readonly List<CombatProfileActionAuditRow> actionRows = [];
            private readonly IGameTableManager gameTableManager;

            public CombatProfileAudit Audit { get; }
            public IReadOnlyList<CombatProfileActionAuditRow> ActionRows => actionRows;
            public IEnumerable<uint> ResolvedCreatureIds => resolutions.Keys;

            public CreatureActionCombatProfileResolver(IGameTableManager gameTableManager, CombatActionRuleData ruleData, CombatProfile defaultProfile)
            {
                this.gameTableManager = gameTableManager;

                if (gameTableManager?.Creature2?.Entries == null || gameTableManager.Creature2Action?.Entries == null)
                {
                    Audit = new CombatProfileAudit(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                    return;
                }

                Dictionary<uint, List<Creature2ActionEntry>> actionsBySet = gameTableManager.Creature2Action.Entries
                    .Where(action => action?.CreatureActionSetId > 0u)
                    .GroupBy(action => action.CreatureActionSetId)
                    .ToDictionary(group => group.Key, group => group.OrderBy(action => action.OrderIndex).ThenBy(action => action.Id).ToList());

                int visualOnlyRows = 0;
                int ignoredRuleRows = 0;
                int rejectedRuleRows = 0;
                int unknownRows = 0;
                int missingSpellRows = 0;
                int actionDerivedProfiles = 0;

                foreach (Creature2Entry creature in gameTableManager.Creature2.Entries.Where(creature => creature?.Creature2ActionSetId > 0u))
                {
                    if (!actionsBySet.TryGetValue(creature.Creature2ActionSetId, out List<Creature2ActionEntry> actions))
                        continue;

                    List<uint> autoAttacks = [];
                    List<CombatSpecialAttack> specialAttacks = [];
                    foreach (Creature2ActionEntry action in actions)
                    {
                        CombatActionRule rule = SelectBestRule(ruleData.Rules, action);
                        if (rule == null)
                        {
                            if (IsVisualOnly(action))
                            {
                                visualOnlyRows++;
                                AddActionRow(creature, action, CombatActionAuditStatus.VisualOnly, diagnostic: "VisualEffect row with empty action data.");
                            }
                            else
                            {
                                unknownRows++;
                                AddActionRow(creature, action, CombatActionAuditStatus.Unknown, diagnostic: "No reviewed Creature2Action rule matched this row.");
                            }

                            continue;
                        }

                        if (rule.Kind == CombatActionRuleKind.Ignore)
                        {
                            ignoredRuleRows++;
                            AddActionRow(creature, action, CombatActionAuditStatus.IgnoredRule, rule, diagnostic: "Matched a reviewed ignore rule.");
                            continue;
                        }

                        if (!CanActivateCombat(rule))
                        {
                            rejectedRuleRows++;
                            AddActionRow(creature, action, CombatActionAuditStatus.RejectedRule, rule, diagnostic: GetActivationRejectionDiagnostic(rule));
                            continue;
                        }

                        uint spell4Id = GetSpell4Id(rule, action);
                        if (spell4Id == 0u || gameTableManager.Spell4?.GetEntry(spell4Id) == null)
                        {
                            missingSpellRows++;
                            AddActionRow(creature, action, CombatActionAuditStatus.MissingSpell, rule, spell4Id, "Matched rule, but the resolved Spell4 row was missing.");
                            continue;
                        }

                        if (rule.Kind == CombatActionRuleKind.AutoAttack)
                        {
                            autoAttacks.Add(spell4Id);
                            AddActionRow(creature, action, CombatActionAuditStatus.ActivatedAutoAttack, rule, spell4Id, "Matched an exact/evidence-backed auto-attack rule.");
                        }
                        else if (rule.Kind == CombatActionRuleKind.SpecialAttack)
                        {
                            specialAttacks.Add(new CombatSpecialAttack(spell4Id, rule.CooldownSeconds.Value, rule.MaxRange, rule.FaceTarget ?? true, rule.Weight ?? 1d));
                            AddActionRow(creature, action, CombatActionAuditStatus.ActivatedSpecialAttack, rule, spell4Id, "Matched an exact/evidence-backed special-attack rule.");
                        }
                    }

                    if (autoAttacks.Count == 0 && specialAttacks.Count == 0)
                        continue;

                    CombatProfile profile = defaultProfile with
                    {
                        AutoAttackSpell4Ids = autoAttacks.Count > 0 ? autoAttacks : defaultProfile.AutoAttackSpell4Ids,
                        SpecialAttacks      = specialAttacks
                    };
                    resolutions[creature.Id] = new CombatProfileResolution(
                        CombatProfileResolutionSource.ProvenActionResolver,
                        profile,
                        creature.Id,
                        Creature2ActionSetId: creature.Creature2ActionSetId,
                        Diagnostics: ["Creature2Action matched a proven combat action rule."]);
                    actionDerivedProfiles++;
                }

                Audit = new CombatProfileAudit(0, 0, actionDerivedProfiles, visualOnlyRows, ignoredRuleRows, rejectedRuleRows, unknownRows, missingSpellRows, actionDerivedProfiles, 0);
            }

            public CombatProfileResolution GetResolution(ICreatureEntity creature)
            {
                if (creature == null)
                    return null;

                return GetResolution(creature.CreatureId);
            }

            public CombatProfileResolution GetResolution(uint creature2Id)
            {
                return resolutions.TryGetValue(creature2Id, out CombatProfileResolution resolution)
                    ? resolution
                    : null;
            }

            private void AddActionRow(
                Creature2Entry creature,
                Creature2ActionEntry action,
                CombatActionAuditStatus status,
                CombatActionRule rule = null,
                uint spell4Id = 0u,
                string diagnostic = null)
            {
                actionRows.Add(new CombatProfileActionAuditRow(
                    creature.Id,
                    GetCreatureName(gameTableManager, creature),
                    creature.Creature2ActionSetId,
                    action.Id,
                    action.OrderIndex,
                    action.State,
                    action.Event,
                    action.Action,
                    action.VisualEffectId,
                    action.ActionData00,
                    action.ActionData01,
                    status,
                    rule?.Source,
                    rule?.Evidence,
                    spell4Id,
                    diagnostic));
            }

            private static bool IsVisualOnly(Creature2ActionEntry action)
            {
                return action.VisualEffectId != 0u
                    && action.ActionData00 == 0u
                    && action.ActionData01 == 0u;
            }

            private static CombatActionRule SelectBestRule(IReadOnlyList<CombatActionRule> rules, Creature2ActionEntry action)
            {
                return rules
                    .Where(rule => Matches(rule, action))
                    .OrderByDescending(GetRuleSpecificity)
                    .FirstOrDefault();
            }

            private static bool Matches(CombatActionRule rule, Creature2ActionEntry action)
            {
                return rule != null
                    && (!rule.State.HasValue || rule.State.Value == action.State)
                    && (!rule.Event.HasValue || rule.Event.Value == action.Event)
                    && (!rule.Action.HasValue || rule.Action.Value == action.Action);
            }

            private static int GetRuleSpecificity(CombatActionRule rule)
            {
                return (rule.State.HasValue ? 1 : 0)
                    + (rule.Event.HasValue ? 1 : 0)
                    + (rule.Action.HasValue ? 1 : 0);
            }

            private static bool CanActivateCombat(CombatActionRule rule)
            {
                if (rule.Kind is not (CombatActionRuleKind.AutoAttack or CombatActionRuleKind.SpecialAttack))
                    return false;

                if (!rule.HasExactActionKey || !rule.HasEvidence || !IsSupportedSpell4IdField(rule.Spell4IdField))
                    return false;

                return rule.Kind == CombatActionRuleKind.AutoAttack
                    || (rule.CooldownSeconds.HasValue && rule.CooldownSeconds.Value > 0d);
            }

            private static string GetActivationRejectionDiagnostic(CombatActionRule rule)
            {
                if (rule.Kind is not (CombatActionRuleKind.AutoAttack or CombatActionRuleKind.SpecialAttack))
                    return "Rule kind does not activate combat.";

                if (!rule.HasExactActionKey)
                    return "Active rules must include exact State, Event, and Action values.";

                if (!rule.HasEvidence)
                    return "Active rules must include source and evidence labels.";

                if (!IsSupportedSpell4IdField(rule.Spell4IdField))
                    return "Active rules must resolve Spell4 from actionData00 or actionData01.";

                if (rule.Kind == CombatActionRuleKind.SpecialAttack && (!rule.CooldownSeconds.HasValue || rule.CooldownSeconds.Value <= 0d))
                    return "Special-attack rules must define a positive cooldown.";

                return "Rule did not pass combat activation guardrails.";
            }

            private static bool IsSupportedSpell4IdField(string spell4IdField)
            {
                return string.Equals(spell4IdField, "actiondata00", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(spell4IdField, "action_data_00", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(spell4IdField, "actiondata01", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(spell4IdField, "action_data_01", StringComparison.OrdinalIgnoreCase);
            }

            private static uint GetSpell4Id(CombatActionRule rule, Creature2ActionEntry action)
            {
                if (string.Equals(rule.Spell4IdField, "actiondata00", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(rule.Spell4IdField, "action_data_00", StringComparison.OrdinalIgnoreCase))
                    return action.ActionData00;

                if (string.Equals(rule.Spell4IdField, "actiondata01", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(rule.Spell4IdField, "action_data_01", StringComparison.OrdinalIgnoreCase))
                    return action.ActionData01;

                return 0u;
            }
        }

        private sealed record ResolvedCombatProfile(CombatProfile Profile, string Id, IReadOnlyList<string> Diagnostics);

        private sealed record ManualCombatProfileData(IReadOnlyDictionary<uint, ResolvedCombatProfile> CreatureProfiles, CombatProfile DefaultProfile)
        {
            public static ManualCombatProfileData Empty { get; } = new(new Dictionary<uint, ResolvedCombatProfile>(), CombatProfile.Default);
        }

        private sealed record CombatKitCatalogData(IReadOnlyDictionary<uint, ResolvedCombatProfile> CreatureProfiles)
        {
            public static CombatKitCatalogData Empty { get; } = new(new Dictionary<uint, ResolvedCombatProfile>());
        }

        private sealed record CombatActionRuleData(IReadOnlyList<CombatActionRule> Rules);

        private sealed class CombatProfileDataFile
        {
            public string DefaultProfile { get; set; }
            public List<CombatProfileDefinition> Profiles { get; set; } = [];
            public List<CreatureProfileDefinition> Creatures { get; set; } = [];
        }

        private sealed class CombatKitCatalogFile
        {
            public List<CombatBehaviorDefinition> Behaviors { get; set; } = [];
            public List<CombatKitDefinition> Kits { get; set; } = [];
            public List<CreatureCombatKitMappingDefinition> Creatures { get; set; } = [];
        }

        private sealed class CombatActionRuleFile
        {
            public List<CombatActionRule> Rules { get; set; } = [];
        }

        private class CombatProfileFieldsDefinition
        {
            public IReadOnlyList<uint> AutoAttackSpell4Ids { get; set; }
            public IReadOnlyList<CombatSpecialAttackDefinition> SpecialAttacks { get; set; }
            public uint? AggroSpell4Id { get; set; }
            public float? ChaseDistance { get; set; }
            public float? AggroRange { get; set; }
            public float? MinimumLeashRange { get; set; }
            public float? AssistRange { get; set; }
            public bool? Stationary { get; set; }
            public bool? TraceCombat { get; set; }
            public bool? AllowNonPlayerTargets { get; set; }
            public bool? AssistSummoner { get; set; }
            public float? SummonerAssistRange { get; set; }
            public float? SummonerFollowDistance { get; set; }
            public float? SummonerFollowRepathDistance { get; set; }
            public uint? SummonerTierSourceBaseSpell4Id { get; set; }
            public uint? SummonerTieredAutoAttackBaseSpell4Id { get; set; }
        }

        private sealed class CombatProfileDefinition : CombatProfileFieldsDefinition
        {
            public string Name { get; set; }
        }

        private sealed class CombatBehaviorDefinition : CombatProfileFieldsDefinition
        {
            public string Id { get; set; }
        }

        private sealed class CombatKitDefinition : CombatProfileFieldsDefinition
        {
            public string Id { get; set; }
            public string Behavior { get; set; }
            public string Source { get; set; }
            public string Evidence { get; set; }
            public string Note { get; set; }
        }

        private sealed class CombatSpecialAttackDefinition
        {
            public uint Spell4Id { get; set; }
            public double CooldownSeconds { get; set; }
            public float? MaxRange { get; set; }
            public bool? FaceTarget { get; set; }
            public double? Weight { get; set; }
        }

        private sealed class CreatureProfileDefinition
        {
            public uint Creature2Id { get; set; }
            public string Profile { get; set; }
            public string Source { get; set; }
            public string Evidence { get; set; }
            public string Note { get; set; }
        }

        private sealed class CreatureCombatKitMappingDefinition
        {
            public uint Creature2Id { get; set; }
            public string Kit { get; set; }
            public string Source { get; set; }
            public string Evidence { get; set; }
            public string Note { get; set; }
        }

    }
}
