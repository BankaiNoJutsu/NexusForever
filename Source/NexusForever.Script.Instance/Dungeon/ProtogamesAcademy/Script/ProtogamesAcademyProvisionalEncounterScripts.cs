using NexusForever.Game.Abstract.Spell;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.ProtogamesAcademy.Script
{
    /// <summary>
    /// PROVISIONAL boss behaviour for Protogames Academy, disabled by default and never retail parity.
    /// Spell ids come from the Spell4 internal description tags (`[UPJ] e4346`, see
    /// INSTANCE_BOSS_ABILITY_CATALOG_20261109.md); the creature id is the one the repo credit script already
    /// uses, re-checked by Creature2 name. Abilities come from INSTANCE_PROTOGAMES_ACADEMY_PACKET_20261109.md
    /// (agent-read subtitles, not re-viewed). The packet states no cast order, interval or health threshold,
    /// so the delay and interval are ESTIMATES and the plan is a single phase without thresholds.
    /// Wrathbone's random-telegraph-that-splits ability is mapped to the Rock cast by name only and rests on
    /// one guide's description; treat the spell choice as doubtful.
    /// Skipped bosses: Super-Invulnotron (the packet says the fight-to-name binding is a guess and its only
    /// self-buff row is not a seen ability); Invulnotron, Gromka, Iruki Boldbeard, Seek-N-Slaughter and Icebox
    /// Mk. 2 (their credit scripts filter by script name, so no repo creature id exists to attach to, and the
    /// packet ties only Iruki and Gromka loosely to a fight).
    /// Not modelled: interrupt-armor cycles, moments of opportunity, the invulnerable meteor phase, the tank
    /// follow telegraph, the 30 second interruptible cast, fire elemental adds and their shield timer, the
    /// 66% and 33% transition rows (no clip states thresholds).
    /// </summary>
    internal static class ProtogamesAcademyProvisionalPlans
    {
        public const uint WrathboneRockCast = 77086u; // [UPJ] e4346 Room 3 Boss Rock (Cast)

        public static ProvisionalEncounterPlan Wrathbone() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(WrathboneRockCast, 12d, 30d)
            ], "Random telegraphs that explode into smaller ones (single guide description, spell chosen by name), interval ESTIMATED")
        ]);
    }

    /// <summary>
    /// Provisional Wrathbone casts; see <see cref="ProtogamesAcademyProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(67944u)]
    public class WrathboneProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public WrathboneProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, ProtogamesAcademyProvisionalPlans.Wrathbone())
        {
        }
    }
}
