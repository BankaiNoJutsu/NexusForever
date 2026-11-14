using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 196 (Racial): verified native case <c>0xc4</c> at <c>1404a2217</c> retains primary through default manager virtual <c>+0x38</c> to <c>14049c520</c>.
    /// A null unit fails; otherwise the UnitRace record ID at <c>*[unit+0xd0]</c> (0 when absent) is compared with full <c>Value</c> for Equal/NotEqual only. ObjectId/relationship are unread.
    /// Independent native Lua GetUnitRaceId names this field; GetRaceId reads a separate field. The Source CreatureEntry.UnitRaceId bridge is retained; complete native writer/dynamic assignment coverage is unqualified.
    /// Evidence tier: Verified native selector/predicate/attribute; Implemented / Source wrapper; retained bridge correlated. See Decomp/Analysis/PREREQUISITE_196_PRIMARY_SUBJECT_20261114.md. No retail parity claim.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Racial)]
    public class PrerequisiteCheckUnitRace : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            uint unitRaceId = subject.CreatureEntry?.UnitRaceId ?? 0u;
            return QuestObjectivePrerequisiteHelper.CompareCompletion(comparison, unitRaceId == value);
        }
    }
}
