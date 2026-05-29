using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Shared
{
    /// <summary>
    /// Native reader: <c>MatchingPvpStateInfo_ReadPayload</c> (<c>140099130</c>).
    /// Reads one 3-bit <see cref="PvpGameState"/> followed by one 32-bit elapsed-time field.
    /// Reused directly by opcode <c>0x05EA</c> and nested under
    /// <c>ServerMatchingMatchPvpStateInitial_ReadPayload</c> (<c>140099190</c>).
    /// </summary>
    public class StateInfo : IWritable
    {
        public PvpGameState State { get; set; }
        public uint TimeElapsed { get; set; } // in milliseconds

        public void Write(GamePacketWriter writer)
        {
            writer.Write(State, 3u);
            writer.Write(TimeElapsed);
        }
    }
}
