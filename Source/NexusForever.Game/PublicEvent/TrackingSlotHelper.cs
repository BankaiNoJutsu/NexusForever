using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.PublicEvent
{
    /// <summary>
    /// Resolves <c>TrackingSlot.tbl</c> rows for map-tracked unit wire ids.
    /// This remains a one-way lookup helper until producer timing and slot selection are verified.
    /// </summary>
    public static class TrackingSlotHelper
    {
        public static TrackingSlotEntry TryGetEntry(IGameTableManager gameTableManager, uint trackingSlotId)
        {
            if (gameTableManager == null || trackingSlotId == 0u)
                return null;

            GameTable<TrackingSlotEntry> trackingSlot = gameTableManager.TrackingSlot;
            if (trackingSlot == null)
                return null;

            return trackingSlot.GetEntry(trackingSlotId & 0x7FFFu);
        }

        public static uint TryGetPublicEventObjectiveId(IGameTableManager gameTableManager, uint trackingSlotId)
        {
            return TryGetEntry(gameTableManager, trackingSlotId)?.PublicEventObjectiveId ?? 0u;
        }
    }
}
