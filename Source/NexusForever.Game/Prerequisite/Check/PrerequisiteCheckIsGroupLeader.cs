using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 42: handler table <c>14049e540</c> uses group-manager float compare path
    /// (<c>1404a2010</c>). NF proxies party leader via <see cref="IGroupStateManager"/>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.IsGroupLeader)]
    public class PrerequisiteCheckIsGroupLeader : IPrerequisiteCheck
    {
        private readonly IGroupStateManager groupStateManager;

        public PrerequisiteCheckIsGroupLeader(IGroupStateManager groupStateManager)
        {
            this.groupStateManager = groupStateManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint isLeader = 0u;
            if (player.GroupAssociation != 0ul
                && groupStateManager.TryGetGroup(player.GroupAssociation, out GroupLootState group)
                && group.Leader.Equals(player.Identity))
            {
                isLeader = 1u;
            }

            return PrerequisiteCompare.Compare(comparison, isLeader, value);
        }
    }
}
