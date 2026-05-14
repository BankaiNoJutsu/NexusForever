using NexusForever.Game.Static.Entity;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerResurrectionShow)]
    public class ServerResurrectionShow : IWritable
    {
        public uint GhostId { get; set; }
        public uint RezCost { get; set; }

        /// <summary>
        /// Set the amount of time, in milliseconds, 
        /// </summary>
        public uint TimeUntilRezMs { get; set; }

        public bool Dead { get; set; }
        public ResurrectionType ShowRezFlags { get; set; } // 8
        public bool HasCasterRezRequest { get; set; }

        /// <summary>
        /// Remaining wake-here cooldown, in milliseconds. The service-token option is controlled by <see cref="ShowRezFlags"/>.
        /// </summary>
        public uint TimeUntilWakeHereMs { get; set; }

        public uint TimeUntilForceRezMs { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(GhostId);
            writer.Write(RezCost);
            writer.Write(TimeUntilRezMs);
            writer.Write(Dead);
            writer.Write(ShowRezFlags, 8u);
            writer.Write(HasCasterRezRequest);
            writer.Write(TimeUntilWakeHereMs);
            writer.Write(TimeUntilForceRezMs);
        }
    }
}
