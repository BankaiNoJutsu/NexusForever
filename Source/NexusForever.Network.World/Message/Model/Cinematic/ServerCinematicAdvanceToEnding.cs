using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Cinematic
{
    [Message(GameMessageOpcode.ServerCinematicAdvanceToEnding)] 
    public class ServerCinematicAdvanceToEnding : IWritable
    {
        public byte Unused { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Unused);
        }
    }
}
