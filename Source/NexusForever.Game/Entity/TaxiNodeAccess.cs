using NexusForever.Game.Static.Reputation;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Entity
{
    public static class TaxiNodeAccess
    {
        public const uint MaxNodeId = 0x3fff;

        public static bool IsAllowed(TaxiNodeEntry node, Faction faction)
        {
            if (node == null || node.Id == 0 || node.Id > MaxNodeId)
                return false;
            return node.TaxiNodeFactionEnum switch
            {
                0 => true,
                1 => faction == Faction.Exile,
                2 => faction == Faction.Dominion,
                _ => false
            };
        }
    }
}
