using NexusForever.Game.Abstract.Spell;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.Skullcano.Script
{
    /// <summary>
    /// PROVISIONAL boss behaviour for Skullcano, disabled by default and never retail parity.
    /// Spell ids come from the Spell4 internal description tags (`[SK] e321 to e324`, see
    /// INSTANCE_BOSS_ABILITY_CATALOG_20261109.md); creature ids are the ones the repo scripts already use
    /// (normal then veteran, order not stated there). Abilities come from the two agreeing clips in
    /// INSTANCE_SKULLCANO_PACKET_20261109.md (agent-read, not re-viewed). Delays and intervals are ESTIMATES;
    /// no health thresholds are used because Mordechai's 33% and 10% are single-source. Not modelled: food,
    /// mushrooms, ink phases, monkey, squids, lasers, pools, turrets, the Gruh'ar miniboss.
    /// </summary>
    internal static class SkullcanoProvisionalPlans
    {
        public const uint TuggaMoltenRain       = 41088u; // [SK] e321 Tugga Molten Rain Base, CastTime 3600 ms
        public const uint TuggaMagmaTotemPair   = 41052u; // [SK] e321 Tugga Magma Totem Pair Base
        public const uint TuggaIntoTheStew      = 63378u; // [SK] e321 Tugga Into the Stew Base
        public const uint ThunderfootThunderPound = 36437u; // [SK] e322 Thunderfoot Thunder Pound Primary Grow Shroom
        public const uint ThunderfootPulverize  = 36385u; // [SK] e322 Thunderfoot Pulverize Base N
        public const uint OctogHarpoon          = 31352u; // [SK] e323 Bosun Octog Harpoon Base
        public const uint OctogInkTwoTargets    = 52872u; // [SK] e323 Bosun Octog Ink Two Targets
        public const uint MordechaiMoltenTorrent = 39469u; // [SK] e324 Mordechai Terraformer Molten Torrent Base, CastTime 1500 ms
        public const uint MordechaiDisperseGrenades = 39566u; // [SK] e324 Mordechai Disperse Grenades Base

        // Quartermaster Gruh'ar (Creature2 24490 word 9 = 1, 24896 word 9 = 5). Added 2026-11-09 after the Jabbithole creature_spells check
        // (INSTANCE_JABBITHOLE_SQL_CHECK_20261109.md). The Skullcano packet reads a 2014-09-29 guide: Kneecap on a player, repeated Dead Eye
        // shots, then "Fatal Shot" which must be interrupted. Jabbithole lists Kneecap 41901 (1 s cast) on both Gruh'ar rows and Fatal Shot 63677
        // (3 s cast) on the veteran row only; the Dead Eye rows (41899, 63663) do not match by id in the 16042 table and are not used.
        public const uint GruharKneecap  = 41901u; // Jabbithole Gruh'ar, CastTime 1000 ms
        public const uint GruharFatalShot = 63677u; // Jabbithole Gruh'ar (veteran row), CastTime 3000 ms

        public static ProvisionalEncounterPlan Gruhar(bool high) => new(
        [
            new ProvisionalPhase(null,
            high
                ? [new ProvisionalCast(GruharKneecap, 6d, 25d, targetCurrentTarget: true), new ProvisionalCast(GruharFatalShot, 20d, 35d, targetCurrentTarget: true)]
                : [new ProvisionalCast(GruharKneecap, 6d, 25d, targetCurrentTarget: true)],
            "Kneecap (and Fatal Shot on the second row) from one 2014 guide and Jabbithole's creature spell list; intervals ESTIMATED, row set per creature id is a GUESS")
        ]);

        public static ProvisionalEncounterPlan Tugga() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(TuggaMoltenRain, 12d, 28d),
                new ProvisionalCast(TuggaMagmaTotemPair, 20d, 35d),
                new ProvisionalCast(TuggaIntoTheStew, 30d, 45d)
            ], "Phase 1 abilities from the guide (totem, Molten Rain, Into the Stew), intervals ESTIMATED")
        ]);

        public static ProvisionalEncounterPlan Thunderfoot() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(ThunderfootThunderPound, 8d, 18d),
                new ProvisionalCast(ThunderfootPulverize, 15d, 25d, targetCurrentTarget: true)
            ], "Ground pounds periodically (guide, 5-minute overview), intervals ESTIMATED")
        ]);

        public static ProvisionalEncounterPlan Octog() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(OctogHarpoon, 10d, 25d),
                new ProvisionalCast(OctogInkTwoTargets, 20d, 35d)
            ], "Hook shot and ink periodically (guide, 5-minute overview), intervals ESTIMATED")
        ]);

        public static ProvisionalEncounterPlan Mordechai() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(MordechaiMoltenTorrent, 12d, 30d),
                new ProvisionalCast(MordechaiDisperseGrenades, 20d, 35d)
            ], "Terraformer lasers and grenade turrets periodically (clips agree on the lasers), intervals ESTIMATED")
        ]);
    }

    /// <summary>
    /// Provisional Stew-Shaman Tugga casts; see <see cref="SkullcanoProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(24493u, 24898u)]
    public class TuggaProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public TuggaProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, SkullcanoProvisionalPlans.Tugga())
        {
        }
    }

    /// <summary>
    /// Provisional Thunderfoot casts; see <see cref="SkullcanoProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(24475u, 24893u)]
    public class ThunderfootProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public ThunderfootProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, SkullcanoProvisionalPlans.Thunderfoot())
        {
        }
    }

    /// <summary>
    /// Provisional Bosun Octog casts; see <see cref="SkullcanoProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(24486u, 24894u)]
    public class OctogProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public OctogProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, SkullcanoProvisionalPlans.Octog())
        {
        }
    }

    /// <summary>
    /// Provisional Quartermaster Gruh'ar casts (Creature2 word 9 = 1 row); see <see cref="SkullcanoProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(24490u)]
    public class GruharProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public GruharProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, SkullcanoProvisionalPlans.Gruhar(false))
        {
        }
    }

    /// <summary>
    /// Provisional Quartermaster Gruh'ar casts (Creature2 word 9 = 5 row); see <see cref="SkullcanoProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(24896u)]
    public class GruharVeteranProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public GruharVeteranProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, SkullcanoProvisionalPlans.Gruhar(true))
        {
        }
    }

    /// <summary>
    /// Provisional Mordechai Redmoon casts; see <see cref="SkullcanoProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(24489u, 24895u)]
    public class MordechaiProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public MordechaiProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, SkullcanoProvisionalPlans.Mordechai())
        {
        }
    }
}
