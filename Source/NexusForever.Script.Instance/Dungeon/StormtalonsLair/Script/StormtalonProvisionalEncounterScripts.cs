using NexusForever.Game.Abstract.Spell;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.StormtalonsLair.Script
{
    /// <summary>
    /// PROVISIONAL boss behaviour for Stormtalon's Lair, disabled by default and never retail parity.
    /// Spell ids come from the Spell4 internal description tags (`[STL] e312/e313/e314`, see
    /// INSTANCE_BOSS_ABILITY_CATALOG_20261109.md): developer-authored names tying each row to a boss and ability.
    /// Phase thresholds come from the observed guides (single-source or disputed where noted).
    /// First delays and intervals are ESTIMATES: the guides say "periodically" and no source here gives retail timing.
    /// Not modelled: disciples, wisps, adds, tornadoes, interrupt-armor rules and the tank/target rules of each cast.
    /// </summary>
    internal static class StormtalonProvisionalPlans
    {
        // Spell4 rows by tag (tier 1 base rows).
        public const uint BladeWindThunderCross     = 38868u; // [STL] e312 Invoker Thunder Cross, CastTime 1500 ms
        public const uint BladeWindLightningStrike  = 38836u; // [STL] e312 Invoker Lightning Strike Base
        public const uint AethrosTorrent            = 36104u; // [STL] e313 Aethros Torrent Special 4 Base, CastTime 2000 ms
        public const uint StormtalonStaticCharge    = 39696u; // [STL] e314 Stormtalon Static Charge
        public const uint StormtalonStaticWave      = 39717u; // [STL] e314 Stormtalon Static Wave Base
        public const uint StormtalonLightningRod    = 39852u; // [STL] e314 Stormtalon Lightning Rod Base

        public static ProvisionalEncounterPlan BladeWind() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(BladeWindThunderCross, 15d, 25d)
            ], "Phase 1: Thundercross periodically (two clips agree on the ability, interval ESTIMATED)"),
            new ProvisionalPhase(0.6f,
            [
                new ProvisionalCast(BladeWindLightningStrike, 6d, 12d),
                new ProvisionalCast(BladeWindThunderCross, 14d, 30d)
            ], "Phase 2 at 60% (single source, disputed: two clips say about 50%); two Lightning Strikes per Thundercross, timing ESTIMATED")
        ]);

        public static ProvisionalEncounterPlan Aethros() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(AethrosTorrent, 12d, 25d)
            ], "Phase 1: Torrent periodically (two clips agree on the ability, interval ESTIMATED); later phases need adds and are not modelled")
        ]);

        public static ProvisionalEncounterPlan Stormtalon() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(StormtalonStaticCharge, 10d, 20d)
            ], "Opening: Static Charge periodically (single guide), interval ESTIMATED"),
            new ProvisionalPhase(0.9f,
            [
                new ProvisionalCast(StormtalonStaticCharge, 10d, 20d),
                new ProvisionalCast(StormtalonStaticWave, 6d, 30d)
            ], "From about 90%: Static Wave (single guide), interval ESTIMATED"),
            new ProvisionalPhase(0.5f,
            [
                new ProvisionalCast(StormtalonStaticCharge, 10d, 20d),
                new ProvisionalCast(StormtalonLightningRod, 4d, 25d)
            ], "From about 50%: Lightning Rod (single guide), interval ESTIMATED")
        ]);
    }

    /// <summary>
    /// Provisional Blade-Wind the Invoker casts; see <see cref="StormtalonProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(17160u, 33405u)]
    public class BladeWindProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public BladeWindProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, StormtalonProvisionalPlans.BladeWind())
        {
        }
    }

    /// <summary>
    /// Provisional Aethros casts; see <see cref="StormtalonProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(17166u, 32703u)]
    public class AethrosProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public AethrosProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, StormtalonProvisionalPlans.Aethros())
        {
        }
    }

    /// <summary>
    /// Provisional Stormtalon casts; see <see cref="StormtalonProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(17163u, 33406u)]
    public class StormtalonProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public StormtalonProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, StormtalonProvisionalPlans.Stormtalon())
        {
        }
    }
}
