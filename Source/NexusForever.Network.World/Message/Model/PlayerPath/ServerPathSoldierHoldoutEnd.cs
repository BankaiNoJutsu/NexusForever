using NexusForever.Game.Static.PlayerPath;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.PlayerPath
{
    [Message(GameMessageOpcode.ServerPathSoldierHoldoutEnd)]
    public class ServerPathSoldierHoldoutEnd : IWritable
    {
        // Result values are source-aligned, but failure ordering and generic
        // holdout end producer semantics remain blocked pending smoke proof.
        public ushort PathSoldierEventId { get; set; }
        public PlayerPathSoldierResult Reason { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(PathSoldierEventId, 14);
            writer.Write(Reason, 32u);
        }
    }
}
