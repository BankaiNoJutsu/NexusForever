using NexusForever.Game.Static.Account;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerAccountOperationResult)]
    public class ServerAccountOperationResult : IWritable
    {
        public AccountOperation Operation { get; set; }
        public AccountOperationResult Result { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Operation, 32u);
            writer.Write(Result, 32u);
        }
    }
}
