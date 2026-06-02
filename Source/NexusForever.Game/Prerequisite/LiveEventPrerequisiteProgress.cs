using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite
{
    /// <summary>
    /// NF proxies for client live-event prerequisite handlers (types 166/167) until NPC
    /// entity+0x3f8/+0x438 live-event trees are modeled.
    /// </summary>
    internal static class LiveEventPrerequisiteProgress
    {
        private const uint ClientDominionWorldId = 0xa6u;
        private const uint ClientExileWorldId    = 0xa7u;

        public static bool TryGetLiveEventEntry(IGameTableManager gameTableManager, uint liveEventId, out LiveEventEntry entry)
        {
            entry = gameTableManager.LiveEvent.GetEntry(liveEventId);
            return entry != null;
        }

        /// <summary>
        /// Client <c>LiveEventTree_LookupByObjectId</c> (<c>1404a7f50</c>) returns <c>1</c> when
        /// <paramref name="liveEventId"/> exists in the NPC <c>entity+0x3f8</c> tree (presence check).
        /// </summary>
        public static uint GetTreeLookupPresence(IUnitEntity npc, uint liveEventId)
        {
            return 0u;
        }

        /// <summary>
        /// Client <c>LiveEventTree_LookupWithWorldBranch</c> (<c>1404a80b0</c>) plus
        /// <c>LiveEvent_GetWorldFactionField</c> (<c>1404a8430</c>) reads record <c>+0x68</c> when the
        /// current world id is <c>0xa6</c> (Dominion) or <c>+0x88</c> when world id is <c>0xa7</c> (Exile).
        /// </summary>
        public static uint GetFactionBranchProgress(IGameTableManager gameTableManager, IPlayer player, uint liveEventId)
        {
            if (!TryGetLiveEventEntry(gameTableManager, liveEventId, out _))
                return 0u;

            uint worldId = player.Map?.Entry?.Id ?? 0u;
            if (worldId != ClientDominionWorldId && worldId != ClientExileWorldId)
                return 0u;

            return 0u;
        }

        public static bool RequiresNpcContext(IPlayer player, IPrerequisiteParameters parameters)
        {
            return PrerequisiteNpcTargetContext.RequiresNpcTarget(player, parameters);
        }
    }
}
