using NexusForever.Game.Abstract.Spell;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Raid.RedMoonTerror.Script
{
    /// <summary>
    /// PROVISIONAL boss behaviour for the Red Moon Terror, disabled by default and never retail parity.
    /// Only Star-Eater the Voracious is scripted. The lead found an official developer preview of her fight
    /// (WildStar - Redmoon Mutiny Preview Stream, 2016-11-09, https://www.youtube.com/watch?v=5HfpsQr6slE,
    /// about 2640 to 2705 s): she pulls players with a hook shot, players with jetpacks and ground slam must smash
    /// the orange-star power-ups into her to break her shield so she cannot cast Supernova and kill everyone.
    /// The Spell4 rows come from the `[RMT] e4556` description tags and match the stream: `Hookshot - Pull`
    /// 85530 and `Supernova - Base` 85713 (CastTime 1500 ms), whose names reuse the Bosun Octog rows' naming
    /// (the shield rows `Astral Shield` 85714/85724/85745 are not cast by the plan). Creature ids 72758 and 75658
    /// were verified by name against Creature2. Delays and intervals are ESTIMATES: the stream gives no timing.
    /// Not modelled: the jetpack and ground slam player abilities, the stars and the shield mechanic, phases,
    /// and the other Red Moon Terror bosses (Engineers, Swabbie and Robomination have no lead-verified clip
    /// and tag-to-creature binding; see INSTANCE_LEAD_VERIFICATION_20261109.md).
    /// </summary>
    internal static class RedMoonTerrorProvisionalPlans
    {
        public const uint StarEaterHookshotPull = 85530u; // [RMT] e4556 Bosun Octog Hookshot Pull BEAX
        public const uint StarEaterSupernova    = 85713u; // [RMT] e4556 Bosun Octog Supernova Base, CastTime 1500 ms

        public static ProvisionalEncounterPlan StarEater() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(StarEaterHookshotPull, 8d, 25d),
                new ProvisionalCast(StarEaterSupernova, 45d, 60d)
            ], "Hook shot pull and Supernova (official 2016-11-09 developer stream, lead read at about 2640 to 2676 s), intervals ESTIMATED")
        ]);
    }

    /// <summary>
    /// Provisional Star-Eater the Voracious casts; see <see cref="RedMoonTerrorProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(72758u, 75658u)]
    public class StarEaterProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public StarEaterProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, RedMoonTerrorProvisionalPlans.StarEater())
        {
        }
    }
}
