using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 96: handler table[96] <c>14049f810</c> loads the active eval context and walks its
    /// linked list (head at context <c>+0x70</c>) for a node whose <c>+0x20</c> key matches <c>objectId</c>,
    /// then compares the float at <c>+0x28</c> via the float ApplyComparison path (<c>140642a00</c>).
    /// NF proxies the eval walk when <see cref="IPrerequisiteParameters.Item"/> is in scope.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.EvalContextFloatByObjectId)]
    public class PrerequisiteCheckEvalContextFloatByObjectId : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            return PrerequisiteEvalListHelper.TryCompareLinkedEvalScalar(parameters, comparison, value, objectId);
        }
    }
}
