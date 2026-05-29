using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerMovementControlRemove)]
    public class ServerMovementControlRemove : IWritable
    {
        /// <summary>
        /// Runtime sender <c>Player.SetControl</c> emits this empty packet when movement control
        /// is cleared from the current controlled entity.
        /// No WildStar64 client reader anchor has been recovered for this server packet.
        /// </summary>
        public void Write(GamePacketWriter writer)
        {
        }
    }
}
