using NexusForever.Game.Abstract.Reputation;
using NexusForever.Game.Reputation;
using NexusForever.Game.Static.Reputation;

namespace NexusForever.Game.Prerequisite
{
    /// <summary>
    /// NF proxy for client <c>PlayerFactionService_IsFactionOrAncestor</c> (<c>1407176b0</c>) used by
    /// prerequisite type 128 (<c>Prerequisite_CheckFaction</c> <c>14049c720</c>).
    /// </summary>
    internal static class FactionPrerequisiteHelper
    {
        public static bool IsFactionOrAncestor(Faction playerFaction, uint requiredFactionId)
        {
            IFactionNode node = FactionManager.Instance.GetFaction(playerFaction);
            if (node == null)
                return false;

            return node.GetAscendant((Faction)requiredFactionId) != null;
        }
    }
}
