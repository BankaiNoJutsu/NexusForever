using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native registration in <c>Network_RegisterServerOpcode_0351</c> binds opcode <c>0x05DC</c>
    /// to shared <c>ServerUInt5_ReadPayload</c> (<c>14007e950</c>).
    /// The current single 5-bit <see cref="Game.Static.Matching.MatchType"/> field matches that shared surface.
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingMatchLeft)]
    public class ServerMatchingMatchLeft : IWritable
    {
        public Game.Static.Matching.MatchType Type { get; set; } // Not used by client, but server did fill this

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Type, 5u); 
        }
    }
}
