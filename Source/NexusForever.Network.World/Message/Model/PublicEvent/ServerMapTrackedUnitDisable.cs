using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.PublicEvent
{
    /// <summary>
    /// Removes a map-tracked unit marker (<c>0x0848</c>).
    /// Consumer: <c>MapTrackedUnitDisable_ApplyAndDispatch</c> @ <c>1403f4200</c>.
    /// </summary>
    /// <remarks>Server disable timing and tracked-unit id allocation remain blocked.</remarks>
    [Message(GameMessageOpcode.ServerMapTrackedUnitDisable)]
    public class ServerMapTrackedUnitDisable : IWritable
    {
        public uint TrackedUnitId { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(TrackedUnitId);
        }
    }
}
