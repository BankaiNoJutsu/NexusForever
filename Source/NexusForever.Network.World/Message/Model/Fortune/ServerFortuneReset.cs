using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Fortune
{
    [Message(GameMessageOpcode.ServerFortuneReset)]
    public class ServerFortuneReset : IWritable
    {
        // Fortune_ApplyReset (WildStar64.exe 1407291f0) only maps code 3 to the click-empty reset path.
        public uint ResetCode { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(ResetCode, 3u);
        }
    }
}
