using NexusForever.Game.Abstract.Spell;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Raid.GeneticArchives.Script
{
    /// <summary>
    /// PROVISIONAL boss behaviour for the Genetic Archives raid, disabled by default and never retail parity.
    /// Spell ids come from the Spell4 internal description tags (`[GA] e410`, `e411`, `e412`, see
    /// INSTANCE_BOSS_ABILITY_CATALOG_20261109.md); the first (tier 1) id of each catalog row is used. Creature ids
    /// are the ones the repo already spawns or scripts, each verified by name against Creature2 (X-89 49198,
    /// Kuralak the Defiler 52969, Phage Maw 52974). Abilities come from the clips agreed by two or more narrations
    /// in INSTANCE_GENETICARCHIVES_PACKET_20261109.md (agent-read subtitles, not re-viewed). Delays and intervals are
    /// ESTIMATES: the packet measured no timing, only commentator statements.
    ///
    /// Skipped bosses:
    /// * Phagetech Prototypes (e413): single-source (one narrator); the four bots share health and take turns
    ///   powering up and down, which a per-creature plan cannot express (all four would act at once).
    /// * Phageborn Convergence: no catalog rows for its objective (e416); the council creatures are not in the repo.
    /// * Dreadphage Ohmna (e415): the packet has only a thin vocabulary from comms clips, the spew spell is
    ///   unresolved, and the only Creature2 row found (49395) is not spawned by the repo and carries a
    ///   "Deprecated" marker, so no live creature id is verified.
    /// * Gravitron Operator and the other minibosses: not in the boss catalog; Gravitron 75/50/25 is single-source
    ///   and must not be used.
    ///
    /// Not modelled: X-89 tile destruction and platform loss, Strain Bomb jump rule, Repugnant Spew cone (catalog
    /// has only Proxy rows), Mighty Leap and the raid pulse (single-source); Kuralak pillars, lasers' geometry,
    /// Vanish stealth and the red ring, egg count escalation (1/2/4/8/16), line-of-sight pillars, interrupt
    /// windows and the Contamination stacking row; Phage Maw generators, batteries, energy shield, crash, moment
    /// of opportunity, bomb counts and the ground/air phase cycle (event driven, so one flat plan only).
    /// No casts are tied to targets beyond "current target" for the tank-directed lines.
    /// </summary>
    internal static class GeneticArchivesProvisionalPlans
    {
        public const uint X89StrainBomb                 = 47353u; // [GA] e410 Strain Mauler Strain Bomb Base, CastTime 2000 ms
        public const uint KuralakPutridDischarge        = 57923u; // [GA] e412 Genetic Architect Putrid Discharge
        public const uint KuralakCultivate              = 60546u; // [GA] e412 Genetic Architect Cultivate Base (the "Cultivate Corruption" egg cast the guide names)
        public const uint KuralakDnaSiphon              = 56660u; // [GA] e412 Genetic Architect DNA Siphon (Tank) Base, CastTime 2000 ms
        public const uint KuralakOutbreak               = 60715u; // [GA] e412 Genetic Architect Outbreak, CastTime 30000 ms
        public const uint PhageMawDetonationBombs       = 60702u; // [GA] e411 Metal Maw Detonation Bombs
        public const uint PhageMawAerialBombardment     = 57306u; // [GA] e411 Metal Maw Aerial Bombardment

        public static ProvisionalEncounterPlan ExperimentX89() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(X89StrainBomb, 25d, 40d)
            ], "Strain Bomb only: the narrations also name the Shockwave line and Corruption Globule, but those rows were dropped by the lead as unbound (agreed by three narrations, one continuous fight with no phase), intervals ESTIMATED")
        ]);

        public static ProvisionalEncounterPlan Kuralak() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(KuralakPutridDischarge, 10d, 25d)
            ], "Phase 1: Putrid Discharge on three random players, frequently (lead read the 2015-11-13 raid guide https://www.youtube.com/watch?v=qVzVREgWB7A at 68 to 81 s); the Vanish row (Hide 60215) and Tainted Ventilation 60454 are not used (name-only, not found in that guide); interval ESTIMATED"),
            new ProvisionalPhase(0.73f,
            [
                new ProvisionalCast(KuralakPutridDischarge, 10d, 25d),
                new ProvisionalCast(KuralakCultivate, 5d, 40d),
                new ProvisionalCast(KuralakDnaSiphon, 20d, 45d, targetCurrentTarget: true),
                new ProvisionalCast(KuralakOutbreak, 30d, 60d)
            ], "Phase 2 at 73% (lead read the same guide at 81 s: 'Phase 2 begins at 73%'; a second clip reportedly says 72%, not found by the lead): Putrid Discharge continues, Cultivate Corruption egg cast, DNA Siphon on the tank, Outbreak occasionally (guide 133 to 143 s); intervals ESTIMATED")
        ]);

        public static ProvisionalEncounterPlan PhageMaw() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(PhageMawDetonationBombs, 15d, 40d),
                new ProvisionalCast(PhageMawAerialBombardment, 30d, 60d)
            ], "Bomb phase and air-phase mortars (agreed by three narrations; the real ground/air cycle is event driven and not modelled), intervals ESTIMATED")
        ]);
    }

    /// <summary>
    /// Provisional Experiment X-89 casts; see <see cref="GeneticArchivesProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(49198u)]
    public class ExperimentX89ProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public ExperimentX89ProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, GeneticArchivesProvisionalPlans.ExperimentX89())
        {
        }
    }

    /// <summary>
    /// Provisional Kuralak the Defiler casts; see <see cref="GeneticArchivesProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(52969u)]
    public class KuralakTheDefilerProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public KuralakTheDefilerProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, GeneticArchivesProvisionalPlans.Kuralak())
        {
        }
    }

    /// <summary>
    /// Provisional Phage Maw casts; see <see cref="GeneticArchivesProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(52974u)]
    public class PhageMawProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public PhageMawProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, GeneticArchivesProvisionalPlans.PhageMaw())
        {
        }
    }
}
