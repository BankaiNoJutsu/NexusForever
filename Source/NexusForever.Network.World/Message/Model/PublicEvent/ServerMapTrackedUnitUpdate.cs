using NexusForever.Network.Message;
using System.Numerics;

namespace NexusForever.Network.World.Message.Model.PublicEvent
{
    /// <summary>
    /// Server map-tracked unit position update (<c>0x0849</c>).
    /// Consumer: <c>MapTrackedUnitUpdate_ApplyAndDispatch</c> @ <c>1403f4170</c>;
    /// UI label/icon via global <c>GetMapTrackedUnitData</c> @ <c>140511c80</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="TrackingSlotId"/> is the 32-bit <c>TrackingSlot.tbl</c> row id and UI marker key.
    /// The global getter uses it directly to resolve the row's label and icon.
    /// The trailing <see cref="WireUInt15"/> is cached separately; its meaning remains unresolved.
    /// </para>
    /// <para>Server producer timing and slot selection remain blocked; do not emit from quest progress guessing.</para>
    /// </remarks>
    [Message(GameMessageOpcode.ServerMapTrackedUnitUpdate)]
    public class ServerMapTrackedUnitUpdate : IWritable
    {
        public uint TrackingSlotId { get; set; }
        public Vector3 Position { get; set; }

        /// <summary>Neutral trailing 15-bit value; not the metadata lookup key.</summary>
        public uint WireUInt15 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(TrackingSlotId);
            writer.Write(Position.X);
            writer.Write(Position.Y);
            writer.Write(Position.Z);
            writer.Write(WireUInt15, 15u);
        }
    }
}
