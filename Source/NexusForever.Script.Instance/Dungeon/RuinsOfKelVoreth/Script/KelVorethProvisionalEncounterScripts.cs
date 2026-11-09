using NexusForever.Game.Abstract.Spell;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.Script
{
    /// <summary>
    /// PROVISIONAL boss behaviour for the Ruins of Kel Voreth, disabled by default and never retail parity.
    /// Spell ids come from the Spell4 internal description tags (`[KV] e449/e444/e446`, see
    /// INSTANCE_BOSS_ABILITY_CATALOG_20261109.md). Creature ids were verified by name against Creature2.
    /// Delays and intervals are ESTIMATES. Only thresholds that the second-source packet confirms are used:
    /// Trogun at 50% (two clips read by the lead, two more agent-read). Grond's 75% and 50% unlocks and Drokk's 85/65/45/25 were not confirmed
    /// by other clips and are not modelled. Not modelled: orbs, volcanoes, fleas, traps, constructs, tethers.
    /// </summary>
    internal static class KelVorethProvisionalPlans
    {
        public const uint TrogunVolcanicStrike  = 52059u; // [KV] e449 Forgemaster Trogun Volcanic Strike Base
        public const uint TrogunForgemastersCall = 52639u; // [KV] e449 Forgemaster Trogun Forgemaster's Call Base
        public const uint TrogunExaniteWeapon   = 38882u; // [KV] e449 Forgemaster Trogun Exanite Weapon Base
        public const uint GrondThrash           = 37376u; // [KV] e444 Grond Thrash Base
        public const uint GrondHeadbutt         = 40176u; // [KV] e444 Grond Headbutt Base
        public const uint DrokkHomingBarrage    = 39422u; // [KV] e446 Drokk Homing Barrage Auto Attack Base

        public static ProvisionalEncounterPlan Trogun() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(TrogunVolcanicStrike, 12d, 25d, targetCurrentTarget: true),
                new ProvisionalCast(TrogunForgemastersCall, 20d, 40d)
            ], "Opening: Volcanic Strike tracks the tank, Forgemaster's Call periodically (guides), intervals ESTIMATED"),
            new ProvisionalPhase(0.5f,
            [
                new ProvisionalCast(TrogunExaniteWeapon, 1d, 0d),
                new ProvisionalCast(TrogunVolcanicStrike, 15d, 25d, targetCurrentTarget: true),
                new ProvisionalCast(TrogunForgemastersCall, 25d, 40d)
            ], "At 50% (lead read two clips: the 2015-10-13 guide https://www.youtube.com/watch?v=_K6WIpU09G4 at about 80 s and the 2014-05-25 beta run https://www.youtube.com/watch?v=K4_L4d4kl0s at about 593 s 'about fifty percent you will again return to the forge'; two more clips agent-read): back to the forge channelling Exanite Weapon, then the opening casts continue; timing ESTIMATED")
        ]);

        public static ProvisionalEncounterPlan Grond() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(GrondThrash, 10d, 25d),
                new ProvisionalCast(GrondHeadbutt, 18d, 30d)
            ], "Thrash and Headbutt periodically (guides), intervals ESTIMATED; Bellow and Mutilate unlock thresholds are unconfirmed and not modelled")
        ]);

        // Darkwitch Gurka (Creature2 33049 word 9 = 1, 33050 word 9 = 5). Added 2026-11-09 after the Jabbithole creature_spells check
        // (INSTANCE_JABBITHOLE_SQL_CHECK_20261109.md): it lists both abilities on Gurka, which upgrades the second-source packet's weak
        // name correlation (Deadly Defilement and Blinding Dark are spoken in the SywoGG clip at about 619 s and 666 s). Which row set belongs
        // to which creature id is a GUESS: the lower Jabbithole creature (hp 128375, level 20) lists 44304/44315, the veteran (hp 626329, level 50)
        // lists 63510/63508, and Creature2 word 9 = 1 is taken as the first and 5 as the second.
        public const uint GurkaDeadlyDefilementLow  = 44304u; // Jabbithole Gurka (level 20), CastTime 1500 ms
        public const uint GurkaBlindingDarkLow      = 44315u; // Jabbithole Gurka (level 20), CastTime 4000 ms
        public const uint GurkaDeadlyDefilementHigh = 63510u; // Jabbithole Gurka (veteran), CastTime 1000 ms
        public const uint GurkaBlindingDarkHigh     = 63508u; // Jabbithole Gurka (veteran), CastTime 4000 ms

        public static ProvisionalEncounterPlan Gurka(bool high) => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(high ? GurkaDeadlyDefilementHigh : GurkaDeadlyDefilementLow, 8d, 20d),
                new ProvisionalCast(high ? GurkaBlindingDarkHigh : GurkaBlindingDarkLow, 18d, 30d, targetCurrentTarget: true)
            ], "Deadly Defilement chasing telegraphs and Blinding Dark (interrupt) named in one clip and listed on Gurka by Jabbithole; intervals ESTIMATED, row set per creature id is a GUESS")
        ]);

        public static ProvisionalEncounterPlan Drokk() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(DrokkHomingBarrage, 8d, 15d)
            ], "Phase 1: Homing Barrage on random players (one guide), interval ESTIMATED; phase thresholds are unconfirmed and not modelled")
        ]);
    }

    /// <summary>
    /// Provisional Forgemaster Trogun casts; see <see cref="KelVorethProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(32531u, 32533u)]
    public class TrogunProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public TrogunProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, KelVorethProvisionalPlans.Trogun())
        {
        }
    }

    /// <summary>
    /// Provisional Grond the Corpsemaker casts; see <see cref="KelVorethProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(32534u, 32535u)]
    public class GrondProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public GrondProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, KelVorethProvisionalPlans.Grond())
        {
        }
    }

    /// <summary>
    /// Provisional Darkwitch Gurka casts (Creature2 word 9 = 1 row); see <see cref="KelVorethProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(33049u)]
    public class GurkaProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public GurkaProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, KelVorethProvisionalPlans.Gurka(false))
        {
        }
    }

    /// <summary>
    /// Provisional Darkwitch Gurka casts (Creature2 word 9 = 5 row); see <see cref="KelVorethProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(33050u)]
    public class GurkaVeteranProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public GurkaVeteranProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, KelVorethProvisionalPlans.Gurka(true))
        {
        }
    }

    /// <summary>
    /// Provisional Slavemaster Drokk casts; see <see cref="KelVorethProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(32536u, 32539u)]
    public class DrokkProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public DrokkProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, KelVorethProvisionalPlans.Drokk())
        {
        }
    }
}
