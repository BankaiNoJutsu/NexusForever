using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite
{
    /// <summary>
    /// NF proxy for client <c>PlayerGlobal_LookupObjectIdInTree</c> (<c>1403d407b</c>) used from
    /// prerequisite type 177 (<c>1404a1830</c>) after the NPC gate.
    /// </summary>
    internal static class PlayerGlobalTreePrerequisiteHelper
    {
        public static uint GetTreeLookupPresence(IPlayer player, IGameTableManager gameTableManager, uint objectId)
        {
            if (gameTableManager.Challenge.GetEntry(objectId) == null)
                return 0u;

            if (player.ChallengeManager.IsChallengeActivated((ushort)objectId))
                return 1u;

            return player.ChallengeManager.GetCompletionCount((ushort)objectId) > 0u ? 1u : 0u;
        }
    }
}
