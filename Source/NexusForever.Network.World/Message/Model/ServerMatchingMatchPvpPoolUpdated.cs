using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native registration in <c>Network_RegisterServerOpcode_0351</c> binds opcode <c>0x05E5</c>
    /// to local reader slot <c>LAB_140099270</c> with registered object size <c>8</c>.
    /// That matches the current two-uint32 lives-remaining surface, but the local slot has not
    /// yet been promoted to a durable function label.
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingMatchPvpPoolUpdated)]
    public class ServerMatchingMatchPvpPoolUpdated : IWritable
    {
        public uint LivesRemainingTeam1 { get; set; }
        public uint LivesRemainingTeam2 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(LivesRemainingTeam1);
            writer.Write(LivesRemainingTeam2);
        }
    }
}
