using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Script.Main.AI
{
    public sealed record CombatSpecialAttack(
        uint Spell4Id,
        double CooldownSeconds,
        float? MaxRange = null,
        bool FaceTarget = true,
        double Weight = 1d);

    public sealed record CombatProfile(
        IReadOnlyList<uint> AutoAttackSpell4Ids,
        IReadOnlyList<CombatSpecialAttack> SpecialAttacks,
        uint AggroSpell4Id,
        float ChaseDistance,
        float? AggroRange,
        float? MinimumLeashRange,
        float AssistRange,
        bool Stationary,
        bool TraceCombat,
        bool AllowNonPlayerTargets,
        bool AssistSummoner = false,
        float SummonerAssistRange = 0f,
        float SummonerFollowDistance = 0f,
        float SummonerFollowRepathDistance = 0f,
        uint SummonerTierSourceBaseSpell4Id = 0u,
        uint SummonerTieredAutoAttackBaseSpell4Id = 0u)
    {
        public static CombatProfile Default { get; } = new(
            AutoAttackSpell4Ids: [5649u, 5652u],
            SpecialAttacks: [],
            AggroSpell4Id: 0u,
            ChaseDistance: 5f,
            AggroRange: null,
            MinimumLeashRange: null,
            AssistRange: 0f,
            Stationary: false,
            TraceCombat: false,
            AllowNonPlayerTargets: false,
            AssistSummoner: false,
            SummonerAssistRange: 0f,
            SummonerFollowDistance: 0f,
            SummonerFollowRepathDistance: 0f,
            SummonerTierSourceBaseSpell4Id: 0u,
            SummonerTieredAutoAttackBaseSpell4Id: 0u);
    }

    public interface ICombatProfileProvider
    {
        CombatProfile GetProfile(ICreatureEntity creature);
    }

    public interface ICombatProfileResolutionProvider : ICombatProfileProvider
    {
        CombatProfileResolution GetResolution(ICreatureEntity creature);
        CombatProfileAudit GetAudit();
        CombatProfileAuditDetails GetAuditDetails();
    }

    public interface IDefaultCombatProfileProvider : ICombatProfileProvider
    {
        CombatProfile GetDefaultProfile();
    }

    public enum CombatProfileResolutionSource
    {
        None,
        ManualOverride,
        ReviewedKitMapping,
        ProvenActionResolver
    }

    public sealed record CombatProfileResolution(
        CombatProfileResolutionSource Source,
        CombatProfile Profile,
        uint Creature2Id,
        string ProfileId = null,
        string KitId = null,
        uint Creature2ActionSetId = 0u,
        IReadOnlyList<string> Diagnostics = null)
    {
        public static CombatProfileResolution None(uint creature2Id = 0u, uint creature2ActionSetId = 0u, IReadOnlyList<string> diagnostics = null)
        {
            return new CombatProfileResolution(
                CombatProfileResolutionSource.None,
                null,
                creature2Id,
                Creature2ActionSetId: creature2ActionSetId,
                Diagnostics: diagnostics);
        }
    }

    public enum CombatActionRuleKind
    {
        Ignore,
        AutoAttack,
        SpecialAttack
    }

    public sealed class CombatActionRule
    {
        public uint? State { get; set; }
        public uint? Event { get; set; }
        public uint? Action { get; set; }
        public string Spell4IdField { get; set; }
        public CombatActionRuleKind Kind { get; set; }
        public double? CooldownSeconds { get; set; }
        public float? MaxRange { get; set; }
        public bool? FaceTarget { get; set; }
        public double? Weight { get; set; }
        public string Source { get; set; }
        public string Evidence { get; set; }
        public string Note { get; set; }

        public bool HasExactActionKey => State.HasValue && Event.HasValue && Action.HasValue;
        public bool HasEvidence => !string.IsNullOrWhiteSpace(Source) && !string.IsNullOrWhiteSpace(Evidence);
    }

    public sealed record CombatProfileAudit(
        int ManualOverrideCreatureCount,
        int ReviewedKitMappingCreatureCount,
        int ActionDerivedProfileCount,
        int ActionVisualOnlyRowCount,
        int ActionIgnoredRuleRowCount,
        int ActionRejectedRuleRowCount,
        int ActionUnknownRowCount,
        int ActionMissingSpellRowCount,
        int MappedCreatureCount,
        int UnmappedCreatureCount);

    public enum CombatActionAuditStatus
    {
        VisualOnly,
        IgnoredRule,
        RejectedRule,
        Unknown,
        MissingSpell,
        ActivatedAutoAttack,
        ActivatedSpecialAttack
    }

    public sealed record CombatProfileCreatureAuditRow(
        uint Creature2Id,
        string CreatureName,
        uint Creature2ActionSetId,
        CombatProfileResolutionSource Source,
        string ProfileId,
        string KitId,
        int AutoAttackCount,
        int SpecialAttackCount,
        IReadOnlyList<string> Diagnostics);

    public sealed record CombatProfileActionAuditRow(
        uint Creature2Id,
        string CreatureName,
        uint Creature2ActionSetId,
        uint Creature2ActionId,
        uint OrderIndex,
        uint State,
        uint Event,
        uint Action,
        uint VisualEffectId,
        uint ActionData00,
        uint ActionData01,
        CombatActionAuditStatus Status,
        string RuleSource,
        string RuleEvidence,
        uint Spell4Id,
        string Diagnostic);

    public sealed record CombatProfileAuditDetails(
        CombatProfileAudit Summary,
        IReadOnlyList<CombatProfileCreatureAuditRow> CreatureRows,
        IReadOnlyList<CombatProfileActionAuditRow> ActionRows);
}
