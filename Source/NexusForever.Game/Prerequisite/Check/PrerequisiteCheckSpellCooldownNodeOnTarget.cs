using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 103 (enum name <c>SpellCooldownNodeOnTarget</c>, an older guess): evaluator case <c>0x67</c> of <c>PrerequisiteManager_EvaluateTypeSlot</c> (<c>1404a2100</c>) calls
    /// <c>FUN_14046a190(unit, secondUnitId, ObjectId)</c>. A null unit is false. With no second unit NotEqual is true and everything else false. Otherwise the flag is "a node of the list at
    /// <c>unit+0x15d0</c> has virtual <c>+0x30</c> equal to <b>ObjectId</b> and virtual <c>+0x20</c> equal to the second unit's id"; Equal returns it, NotEqual its negation. Value is never read.
    /// </summary>
    /// <remarks>
    /// The list at <c>+0x15d0</c> holds the unit's active spell effects and virtual <c>+0x30</c> is the <c>Spell4Effects</c> id, as for type 102 (Decomp/Analysis/PREREQUISITE_ACTIVE_EFFECT_ID_20261107.md).
    /// That virtual <c>+0x20</c> is the unit that applied the effect is inferred, not read: 23 of the 46 spell references are target-apply prerequisites of the form "NotEqual, this same effect row", for
    /// example Spell4 29061 "Landmine Proximity" whose CCStateSet row 50511 is gated by (NotEqual, 50511), evaluated with the target as the unit and the caster as the second unit, which reads as
    /// "do not apply again while my own copy of this effect is on the target".
    /// The server does not track effect rows applied by other units, so the effect is approximated by its spell: the unit must hold tracked state of the spell that owns the effect row, registered to
    /// the second unit as caster. The previous implementation looked up a cooldown node by Value (0 on 33 of 36 retail slots) on the player and ignored who the second unit was.
    /// Status: Implemented, inferred. See Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md. No retail parity claim.
    /// </remarks>
    [PrerequisiteCheck(PrerequisiteType.SpellCooldownNodeOnTarget)]
    public class PrerequisiteCheckSpellCooldownNodeOnTarget : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckSpellCooldownNodeOnTarget(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            IWorldEntity second = parameters?.Target;
            if (second == null)
                return comparison == PrerequisiteComparison.NotEqual;

            Spell4EffectsEntry effect = gameTableManager?.Spell4Effects?.GetEntry(objectId);
            bool active = effect != null && subject.GetActiveSpellStackCount(effect.SpellId, second.Guid) > 0u;
            return PrerequisiteCompare.CompareFlag(comparison, active);
        }
    }
}
