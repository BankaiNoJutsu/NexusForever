using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 177: native evaluator case <c>0xB1</c> calls the integer helper <c>FUN_1404a2090(comparison, *(int*)(unit+0x38), Value)</c> for a non-null unit: the unit`s base level compared with <c>Value</c>,
    /// <c>ObjectId</c> unused. (Type 1 reads the scaled level at <c>+0x3C</c> first; the server has no separate effective level, so both read <see cref="IWorldEntity.Level"/>.) A null unit is false.
    /// Supersedes the earlier challenge-presence proxy. See Decomp/Analysis/PREREQUISITE_TRUE_LEVEL_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.TrueLevel)]
    public class PrerequisiteCheckTrueLevel : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            return PrerequisiteCompare.Compare(comparison, subject.Level, value);
        }
    }
}
