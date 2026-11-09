using NexusForever.Game.Abstract.Spell;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.ColdbloodCitadel.Script
{
    /// <summary>
    /// PROVISIONAL boss behaviour for Coldblood Citadel, disabled by default and never retail parity.
    /// Spell ids come from the Spell4 internal description tags (`[CBC] e5313` and `e5314`, see
    /// INSTANCE_BOSS_ABILITY_CATALOG_20261109.md); creature ids are the ones the repo already uses
    /// (Darksister Golag 75472, Katla 75473, Ulfrid 75474, Hailstone Gatecrasher 75508, all verified by name
    /// against Creature2). The abilities come from ONE developer-narrated PTR clip (2017-01-23 livestream,
    /// INSTANCE_COLDBLOOD_CITADEL_PACKET_20261109.md, agent-read, dev cheats, "not a real run"); no second clip
    /// exists, so this is weaker evidence than the other provisional scripts. Delays and intervals are
    /// ESTIMATES (the clip gives no numbers); no health thresholds are used because the packet states none.
    /// Row-to-ability matching is by name only (the packet itself marks several as guesses).
    /// Skipped: the Risen Harizog (75459) because the catalog holds only Stasis Spell 87761 and Jump Down 87762
    /// under e5315 and neither matches an ability the clip reports (Soulfrost Surge, Primal Echoes).
    /// Not modelled: the Hailstone proximity freeze aura and its snare, Pell channelers, the group interrupt,
    /// the soul-bonded shared health pools, vector slides, the miniboss, the sisters' activation order and
    /// shared-health or shield rules, Golag's frontal hit and snare, Katla's blood magic beyond the tornado,
    /// the resurrection cutscene and the element hand-over to Harizog.
    /// </summary>
    internal static class ColdbloodCitadelProvisionalPlans
    {
        public const uint HailstoneIceSpikesAura = 88129u; // [CBC] e5313 Ice Elemental - Howling Winds - Ice Spikes (Aura), name matches the "spike lover" remark
        public const uint UlfridSeekingShadow    = 88336u; // [CBC] e5314 Coven Fight - Seeking Shadow - Base (chasing shadowy spikes, name match only)
        public const uint KatlaBloodTornadoAura  = 88107u; // [CBC] e5314 Coven Fight - Blood Tornado - Aura (first of two rows, name match only)
        public const uint GolagSoulfrostTrail    = 88328u; // [CBC] e5314 Coven Fight - Soulfrost Trail - Base (trail behind her)

        public static ProvisionalEncounterPlan HailstoneGatecrasher() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(HailstoneIceSpikesAura, 10d, 25d)
            ], "Ice spikes and a Berserker Rage from the single 2017 developer clip, timings ESTIMATED, rage trigger unknown")
        ]);

        public static ProvisionalEncounterPlan Ulfrid() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(UlfridSeekingShadow, 8d, 20d)
            ], "Chasing shadow spikes from the single 2017 developer clip, interval ESTIMATED")
        ]);

        public static ProvisionalEncounterPlan Katla() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(KatlaBloodTornadoAura, 8d, 25d)
            ], "Blood tornadoes from the single 2017 developer clip, interval ESTIMATED")
        ]);

        public static ProvisionalEncounterPlan Golag() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(GolagSoulfrostTrail, 8d, 25d)
            ], "Soul Frost trail from the single 2017 developer clip, interval ESTIMATED")
        ]);
    }

    /// <summary>
    /// Provisional Hailstone Gatecrasher casts; see <see cref="ColdbloodCitadelProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(75508u)]
    public class HailstoneGatecrasherProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public HailstoneGatecrasherProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, ColdbloodCitadelProvisionalPlans.HailstoneGatecrasher())
        {
        }
    }

    /// <summary>
    /// Provisional Darksister Ulfrid casts; see <see cref="ColdbloodCitadelProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(75474u)]
    public class UlfridProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public UlfridProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, ColdbloodCitadelProvisionalPlans.Ulfrid())
        {
        }
    }

    /// <summary>
    /// Provisional Darksister Katla casts; see <see cref="ColdbloodCitadelProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(75473u)]
    public class KatlaProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public KatlaProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, ColdbloodCitadelProvisionalPlans.Katla())
        {
        }
    }

    /// <summary>
    /// Provisional Darksister Golag casts; see <see cref="ColdbloodCitadelProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(75472u)]
    public class GolagProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public GolagProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, ColdbloodCitadelProvisionalPlans.Golag())
        {
        }
    }
}
