using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 39: evaluator case <c>0x27</c> tests <c>unit+0x80 == 0x14</c> (the player entity type) and finishes in the shared boolean tail: Equal returns it, NotEqual its negation, any other comparison
    /// is false; <c>Value</c> is not read (see <see cref="PrerequisiteCompare.CompareFlag"/>). Retail: 200 slots, <c>Value</c> 0 in 184, Equal 161, NotEqual 39. The previous implementation compared the flag with
    /// <c>Value</c>, which made Equal 0 mean "is not a player". See Decomp/Analysis/PREREQUISITE_FLAG_COMPARISON_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.IsPlayer)]
    public class PrerequisiteCheckIsPlayer : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IWorldEntity entity = parameters.Target as IWorldEntity ?? player;
            return PrerequisiteCompare.CompareFlag(comparison, entity is IPlayer);
        }

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => subject != null && PrerequisiteCompare.CompareFlag(comparison, subject is IPlayer);
    }
}
