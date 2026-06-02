using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 43: handler table <c>14049e610</c> checks active quest objective flag at client
    /// global <c>DAT_140c65888+0x67</c>. NF uses <see cref="IQuestManager.IsActiveObjectiveId"/>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.IsObjectiveActive)]
    public class PrerequisiteCheckIsObjectiveActive : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint active = player.QuestManager.IsActiveObjectiveId(objectId) ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, active, value);
        }
    }
}
