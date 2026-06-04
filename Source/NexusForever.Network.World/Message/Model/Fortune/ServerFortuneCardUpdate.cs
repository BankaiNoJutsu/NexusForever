using NexusForever.Game.Static.Fortune;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Fortune
{
    [Message(GameMessageOpcode.ServerFortuneCardUpdate)]
    public class ServerFortuneCardUpdate : IWritable
    {
        public bool HasUpdate { get; set; } = true; // client ignores the operation/card flags when false
        public FortuneOperation Operation { get; set; }   
        public bool[] CardFlipped { get; set; } = new bool[3];

        public void Write(GamePacketWriter writer)
        {
            writer.Write(HasUpdate);
            writer.Write(Operation, 3u);
            for(uint i = 0; i < 3; i++)
            {
                writer.Write(CardFlipped[i]);
            }
        }
    }
}
