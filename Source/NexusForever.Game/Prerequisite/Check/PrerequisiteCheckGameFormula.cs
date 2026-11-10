using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 170 (enum name <c>GameFormula</c>, an older guess): evaluator case <c>0xaa</c> of <c>PrerequisiteManager_EvaluateTypeSlot</c> (<c>1404a2100</c>) calls manager vtable <c>+0x90</c>
    /// (<c>14049c880</c>) with the unit, the comparison and <b>Value</b>. A null unit is false. The flag is false when the object at the local-player global <c>+0x7188</c> is absent, true when Value is 0,
    /// and otherwise "that object's id (virtual <c>+0x68</c>) equals Value"; Equal returns it, NotEqual its negation, any other comparison false. ObjectId is never read and no table is consulted.
    /// </summary>
    /// <remarks>
    /// The object is read as the active client-side interaction. All 14 retail rows gate creature activate spells or animation states of transport NPCs whose spell is a "Transport to ... Yes/No CSI"
    /// spell, with NotEqual, and Value equals that spell's own <c>Spell4Base.clientSideInteractionId</c> in 5 of the 7 spells (62688 -> 1029, 62734 -> 1031, 63342 -> 1050, 87494 -> 1363, 87510 -> 1367):
    /// "offer the transport unless its confirmation is already open". The object at <c>+0x7188</c> itself was not identified natively.
    /// The previous implementation looked Value up in GameFormula.tbl and compared a column with ObjectId.
    /// The server models an open interaction as a pending spell waiting on a client-side interaction response.
    /// Status: Implemented, inferred. See Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md. No retail parity claim.
    /// </remarks>
    [PrerequisiteCheck(PrerequisiteType.GameFormula)]
    public class PrerequisiteCheckGameFormula : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            return Evaluate(comparison, value, id => SpellPrerequisiteHelper.HasActiveClientSideInteraction(subject, id));
        }

        /// <summary>
        /// Native rule: the flag is "an interaction is active and Value is 0 or its id"; only Equal and NotEqual can be met.
        /// </summary>
        internal static bool Evaluate(PrerequisiteComparison comparison, uint value, Func<uint, bool> hasActiveInteraction)
            => PrerequisiteCompare.CompareFlag(comparison, hasActiveInteraction(value));
    }
}
