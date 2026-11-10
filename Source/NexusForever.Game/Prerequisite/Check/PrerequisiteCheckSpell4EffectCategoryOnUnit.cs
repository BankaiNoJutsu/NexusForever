using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 93 (enum name <c>Spell4EffectCategoryOnUnit</c>, an older guess): evaluator case <c>0x5d</c> of <c>PrerequisiteManager_EvaluateTypeSlot</c> (<c>1404a2100</c>) is inline. A null unit is
    /// false; otherwise it walks the unit active-spell list at <c>+0x15c8</c> and compares a dword of each spell's data (<c>[[[node+0x38]+0x138]+0x70]+0xf8</c>) with the slot <b>ObjectId</b>; Equal returns
    /// whether one matched, NotEqual the negation, any other comparison false. Value is never read.
    /// </summary>
    /// <remarks>
    /// The compared dword was not identified natively. That it is <c>Spell4Base.SpellClass</c> is inferred from the retail rows: all 5 slots use ObjectId 36 or 38, which are exactly BuffDispellable and
    /// DebuffDispellable, and the only spells gated by those rows are the tiers of Medic "Antidote" (a SpellDispel) through the caster-apply prerequisite of its follow-up proxies.
    /// The previous implementation read Value (0 on every retail slot) as a Spell4EffectGroupList id. <c>14049f690</c>, previously cited here, is the type 6 QuestState handler.
    /// Status: Implemented, inferred. See Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md. No retail parity claim.
    /// </remarks>
    [PrerequisiteCheck(PrerequisiteType.Spell4EffectCategoryOnUnit)]
    public class PrerequisiteCheckSpell4EffectCategoryOnUnit : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckSpell4EffectCategoryOnUnit(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsEntity(parameters.Target ?? player, comparison, value, objectId);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsEntity(subject, comparison, value, objectId);

        private bool MeetsEntity(IUnitEntity unit, PrerequisiteComparison comparison, uint value, uint objectId)
        {
            if (unit == null)
                return false;

            return PrerequisiteCompare.CompareFlag(comparison, SpellPrerequisiteHelper.HasTrackedSpellOfClass(unit, objectId, gameTableManager));
        }
    }
}
