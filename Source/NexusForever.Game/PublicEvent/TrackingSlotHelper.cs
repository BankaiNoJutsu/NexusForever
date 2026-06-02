using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.PublicEvent
{
    /// <summary>
    /// Resolves <c>TrackingSlot.tbl</c> rows for map-tracked unit wire ids.
    /// </summary>
    public static class TrackingSlotHelper
    {
        public static TrackingSlotEntry TryGetEntry(IGameTableManager gameTableManager, uint trackingSlotId)
        {
            if (gameTableManager == null || trackingSlotId == 0u)
                return null;

            return gameTableManager.TrackingSlot.GetEntry(trackingSlotId & 0x7FFFu);
        }

        public static uint TryGetPublicEventObjectiveId(IGameTableManager gameTableManager, uint trackingSlotId)
        {
            return TryGetEntry(gameTableManager, trackingSlotId)?.PublicEventObjectiveId ?? 0u;
        }
    }
}
