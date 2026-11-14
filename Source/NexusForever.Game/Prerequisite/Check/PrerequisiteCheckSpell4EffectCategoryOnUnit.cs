using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 93 (legacy enum name <c>Spell4EffectCategoryOnUnit</c>): verified native indexed case <c>0x5d</c> at <c>1404a2b0b</c> retains the primary unit. A null unit is
    /// false; otherwise it walks the unit active-spell list at <c>+0x15c8</c> and compares a dword of each spell's data (<c>[[[node+0x38]+0x138]+0x70]+0xf8</c>) with the slot <b>ObjectId</b>; Equal returns
    /// whether one matched, NotEqual the negation, any other comparison false. Value is never read.
    /// </summary>
    /// <remarks>
    /// Verified native builder <c>140568540</c> copies <c>Spell4Base+0x58</c> (SpellClass in the owning shipped reader) to runtime <c>+0x70+0xf8</c>;
    /// independent Lua GetClass reads the same field. ObjectId is the full class value; Value and relationship are unread. The enum name does not imply an effect-group test.
    /// Source tracked-state membership remains an approximation of the native receiving-target list. See Decomp/Analysis/PREREQUISITE_93_PRIMARY_SUBJECT_20261114.md.
    /// Evidence tier: Verified native selector/field/comparison; Implemented / Source wrapper; retained list coverage inferred. No retail parity claim.
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
            => MeetsEntity(player, comparison, value, objectId);

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
