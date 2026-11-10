using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 104 (enum name <c>SpellCooldownNodeInServiceSet</c>, an older guess): evaluator case <c>0x68</c> of <c>PrerequisiteManager_EvaluateTypeSlot</c> (<c>1404a2100</c>) calls
    /// <c>14046a210(unit, ObjectId)</c>. The spell service (virtual <c>+0x40</c>, <c>140561e00</c>) returns the effect ids stored under key <b>ObjectId</b> in the map at service <c>+0x740</c>; the flag
    /// is "an active effect on the unit (list at <c>+0x15d0</c>, effect id from virtual <c>+0x30</c>) is in that set". Equal returns it, NotEqual its negation, any other comparison and a null unit
    /// false. Value is never read.
    /// </summary>
    /// <remarks>
    /// The map is built by <c>Spell4Manager_BuildRuntimeData</c> (<c>14055e180</c>): for every Spell4Effects row it reads the row's Spell4EffectGroupList and files the effect under each group id in
    /// it. ObjectId is therefore a spell effect group. Retail: row 23816 is "owner or neighbour, and NotEqual group 12" on the animation state of the "Eldan Statue" and "Warhorn" housing plugs, and
    /// the 17 effects in group 12 belong to the "Housing Statue Buff" and "Housing Warhorn Buff" spells: the plug is offered while none of those buffs is active. The other two rows (groups 4 and 14)
    /// have no live reference.
    /// The previous implementation asked whether the player knew a spell using cooldown id Value (0 on every slot).
    /// The server finds group effects through spells tracked on the unit (<see cref="SpellPrerequisiteHelper.HasActiveSpellEffectGroup"/>), not through a per-unit effect list.
    /// Status: Implemented from the native body and the table builder. See Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md. No retail parity claim.
    /// </remarks>
    [PrerequisiteCheck(PrerequisiteType.SpellCooldownNodeInServiceSet)]
    public class PrerequisiteCheckSpellCooldownNodeInServiceSet : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckSpellCooldownNodeInServiceSet(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            return Evaluate(comparison, SpellPrerequisiteHelper.HasActiveSpellEffectGroup(subject, objectId, gameTableManager));
        }

        internal static bool Evaluate(PrerequisiteComparison comparison, bool hasEffectInGroup)
            => PrerequisiteCompare.CompareFlag(comparison, hasEffectInGroup);
    }
}
