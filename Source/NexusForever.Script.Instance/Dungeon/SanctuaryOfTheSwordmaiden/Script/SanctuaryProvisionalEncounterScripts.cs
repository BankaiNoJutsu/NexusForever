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
        public const uint OnduCloverBase          = 43253u; // [TS] e480 Ondu Clover Base
        public const uint SkashCorruptionHeartseeker = 45081u; // [TS] e483 Skash Corruption Heartseeker Base, "heart seeker" is said in two retail clips read by the lead
        public const uint RaynaFlameLeash         = 42171u; // [TS] e486 Rayna Flame Leash Base, CastTime 2567 ms
        public const uint RaynaMoltenWave1        = 42264u; // [TS] e486 Rayna Molten Wave 1 Base, CastTime 3000 ms
        public const uint SeleneLightsOut         = 42757u; // [TS] e492 Selene Lights Out! Base
        public const uint SeleneShadePrison       = 42386u; // [TS] e492 Selene Shade Prison Base, CastTime 3000 ms

        public static ProvisionalEncounterPlan Ondu() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(OnduCloverBase, 10d, 20d)
            ], "Clover Leaf ground pools (named by one guide, https://www.youtube.com/watch?v=8lJX5YzUk8c at about 1078 s, and by the Plague Splatter challenge text); the root phase row 42526 was dropped by the lead as not confirmed, interval ESTIMATED")
        ]);

        public static ProvisionalEncounterPlan Skash() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(SkashCorruptionHeartseeker, 15d, 45d)
            ], "Corruption Heartseeker (named in two retail clips read by the lead); the Summon the Swarm row 42135 was dropped as not confirmed, interval ESTIMATED")
        ]);

        public static ProvisionalEncounterPlan Rayna() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(RaynaFlameLeash, 8d, 30d, targetCurrentTarget: true),
                new ProvisionalCast(RaynaMoltenWave1, 25d, 45d)
            ], "Flame leash ('leash' said by three channels) and Molten Wave (the Prime challenge text); the Smoldering Pool row 42418 was dropped by the lead as not confirmed, intervals ESTIMATED")
        ]);

        public static ProvisionalEncounterPlan Selene() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(SeleneLightsOut, 30d, 60d),
                new ProvisionalCast(SeleneShadePrison, 45d, 60d, targetCurrentTarget: true)
            ], "Lights Out! and Shade Prison (both named by client text, Shade Prison also said in one clip); Shadow Meld 42880 was dropped by the lead as not confirmed; original note: from one guide (Lights Out! and Shade Prison also named by challenge text); phase order and thresholds are single-source and unused, intervals ESTIMATED")
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
