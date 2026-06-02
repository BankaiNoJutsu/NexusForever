using NexusForever.Network.Message;
using System.Numerics;

namespace NexusForever.Network.World.Message.Model.PublicEvent
{
    /// <summary>
    /// Server map-tracked unit position update (<c>0x0849</c>).
    /// Consumer: <c>MapTrackedUnitUpdate_ApplyAndDispatch</c> @ <c>1403f4170</c>;
    /// UI label/icon via <c>Lua_GameLib_GetMapTrackedUnitData</c> @ <c>140511c80</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="TrackingSlotId"/> is a 15-bit <c>TrackingSlot.tbl</c> row id (not a public-event objective id).
    /// Retail binding chain: <c>TrackedUnitId</c> -> <see cref="TrackingSlotId"/> ->
    /// <c>TrackingSlot.PublicEventObjectiveId</c> for map marker text/icon.
    /// </para>
    /// <para>Server producer timing and tracked-unit id allocation remain blocked; do not emit from quest progress guessing.</para>
    /// </remarks>
    [Message(GameMessageOpcode.ServerMapTrackedUnitUpdate)]
    public class ServerMapTrackedUnitUpdate : IWritable
    {
        public uint TrackedUnitId { get; set; }
        public Vector3 Position { get; set; }

        /// <summary>15-bit <c>TrackingSlot.tbl</c> id; resolves label/icon for <see cref="TrackedUnitId"/>.</summary>
        public uint TrackingSlotId { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(TrackedUnitId);
            writer.Write(Position.X);
            writer.Write(Position.Y);
            writer.Write(Position.Z);
            writer.Write(TrackingSlotId, 15u);
        }
    }
}
