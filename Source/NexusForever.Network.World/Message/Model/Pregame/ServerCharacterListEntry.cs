using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Pregame
{
    [Message(GameMessageOpcode.ServerCharacterListEntry)]
    public class ServerCharacterListEntry : IWritable
    {
        public ServerCharacterList.Character Character { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            Character.Write(writer);
        }
    }
}