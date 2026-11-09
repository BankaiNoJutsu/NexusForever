using NexusForever.Game.Abstract.Spell;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.SanctuaryOfTheSwordmaiden.Script
{
    /// <summary>
    /// PROVISIONAL boss behaviour for Sanctuary of the Swordmaiden, disabled by default and never retail parity.
    /// Spell ids come from the Spell4 internal description tags (`[TS] e480`, `e483`, `e486`, `e492`, see
    /// INSTANCE_BOSS_ABILITY_CATALOG_20261109.md); creature ids are the ones the repo credit scripts already use,
    /// each re-checked by Creature2 name (Ondu 28719 is dropped: its name is "[deprTEMPLATE]Ondu Lifeweaver").
    /// Abilities come from INSTANCE_SANCTUARY_PACKET_20261109.md (agent-read subtitles and client text, not
    /// re-viewed); the packet states no cast order, interval or health threshold for any boss, so every delay and
    /// interval is an ESTIMATE and every plan is a single phase without thresholds.
    /// Skipped bosses: Deadringer Shallaos (the only catalog row under e479 is a "Jump to Chime" vignette
    /// flavour helper, and the packet ties no other spell to him).
    /// Not modelled: Resonance aggro, Ondu's totem heal channel, enrage, shield and bomb phase, Skash's hammer
    /// phase, eggs and eyeballs, spider adds, Overgrowth, Rayna's air-lift phase, Totems of Flame, Selene's
    /// hold-out event, light zones, grapples, shadow stuns and Darkness Effigy adds, optional minibosses, elites.
    /// </summary>
    internal static class SanctuaryProvisionalPlans
    {
        public const uint OnduRootPhase           = 42526u; // [TS] e480 Ondu PBAE Root 1 Root Phase, CastTime 2000 ms
        public const uint OnduCloverBase          = 43253u; // [TS] e480 Ondu Clover Base
        public const uint SkashSummonTheSwarm     = 42135u; // [TS] e483 Skash Summon the Swarm Base
        public const uint RaynaFlameLeash         = 42171u; // [TS] e486 Rayna Flame Leash Base, CastTime 2567 ms
        public const uint RaynaMoltenWave1        = 42264u; // [TS] e486 Rayna Molten Wave 1 Base, CastTime 3000 ms
        public const uint RaynaSmolderingPool     = 42418u; // [TS] e486 Rayna Smoldering Pool Base, CastTime 2000 ms
        public const uint SeleneShadowMeld        = 42880u; // [TS] e492 Selene Shadow Meld Base
        public const uint SeleneLightsOut         = 42757u; // [TS] e492 Selene Lights Out! Base
        public const uint SeleneShadePrison       = 42386u; // [TS] e492 Selene Shade Prison Base, CastTime 3000 ms

        public static ProvisionalEncounterPlan Ondu() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(OnduCloverBase, 10d, 20d),
                new ProvisionalCast(OnduRootPhase, 40d, 60d)
            ], "Clover Leaf ground pools and the root phase from two agreeing guides and the Plague Splatter challenge text, intervals ESTIMATED")
        ]);

        public static ProvisionalEncounterPlan Skash() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(SkashSummonTheSwarm, 15d, 45d)
            ], "Spider swarm adds from two agreeing guides (spell chosen by name, adds themselves not modelled), interval ESTIMATED")
        ]);

        public static ProvisionalEncounterPlan Rayna() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(RaynaFlameLeash, 8d, 30d, targetCurrentTarget: true),
                new ProvisionalCast(RaynaSmolderingPool, 12d, 25d),
                new ProvisionalCast(RaynaMoltenWave1, 25d, 45d)
            ], "Tether, lava pools and fire waves from two agreeing guides and the Molten Wave challenge text (pool spell chosen by name), intervals ESTIMATED")
        ]);

        public static ProvisionalEncounterPlan Selene() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(SeleneShadowMeld, 10d, 60d),
                new ProvisionalCast(SeleneLightsOut, 30d, 60d),
                new ProvisionalCast(SeleneShadePrison, 45d, 60d, targetCurrentTarget: true)
            ], "Shadow Meld, Lights Out! and Shade Prison from one guide (Lights Out! and Shade Prison also named by challenge text); phase order and thresholds are single-source and unused, intervals ESTIMATED")
        ]);
    }

    /// <summary>
    /// Provisional Ondu Lifeweaver casts; see <see cref="SanctuaryProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(28720u, 28721u)]
    public class OnduProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public OnduProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, SanctuaryProvisionalPlans.Ondu())
        {
        }
    }

    /// <summary>
    /// Provisional Moldwood Overlord Skash casts; see <see cref="SanctuaryProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(28728u, 28727u)]
    public class SkashProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public SkashProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, SanctuaryProvisionalPlans.Skash())
        {
        }
    }

    /// <summary>
    /// Provisional Rayna Darkspeaker casts; see <see cref="SanctuaryProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(28733u, 28732u)]
    public class RaynaProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public RaynaProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, SanctuaryProvisionalPlans.Rayna())
        {
        }
    }

    /// <summary>
    /// Provisional Spiritmother Selene casts; see <see cref="SanctuaryProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(28736u, 28735u)]
    public class SeleneProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public SeleneProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, SanctuaryProvisionalPlans.Selene())
        {
        }
    }
}
