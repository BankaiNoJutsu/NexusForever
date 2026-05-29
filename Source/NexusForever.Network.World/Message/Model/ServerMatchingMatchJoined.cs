using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native registration in <c>Network_RegisterServerOpcode_0351</c> binds opcode <c>0x05C6</c>
    /// to local reader slot <c>LAB_14007a530</c> with registered size <c>4</c>.
    /// That slot is registration-only evidence for a shared one-<c>uint14</c> surface, so keep
    /// the current model conservative.
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingMatchJoined)]
    public class ServerMatchingMatchJoined : IWritable
    {
        public uint MatchingGameMapId { get; set; } // Id of map the player has just joined

        public void Write(GamePacketWriter writer)
        {
            writer.Write(MatchingGameMapId, 0xEu);
        }
    }
}
