using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Reader-backed structural payload for opcode <c>0x0468</c>.
    /// Native reader <c>ServerGroupTargetIdentityPrimeLevelList_ReadPayload</c> (<c>140084280</c>)
    /// parses group id, target identity, a uint32 row count, and counted
    /// <see cref="PrimeLevelInfo"/> rows through <c>MatchingPrimeLevelInfo_ReadPayload</c>
    /// (<c>1400ad150</c>). Producer semantics remain blocked.
    /// </summary>
    [Message(GameMessageOpcode.ServerGroupTargetIdentityPrimeLevelList)]
    public class ServerGroupTargetIdentityPrimeLevelList : IWritable
    {
        public ulong GroupId { get; set; }

        public Identity TargetPlayer { get; set; } = new();

        public List<PrimeLevelInfo> PrimeLevels { get; } = [];

        public void Write(GamePacketWriter writer)
        {
            writer.Write(GroupId);
            TargetPlayer.Write(writer);
            writer.Write((uint)PrimeLevels.Count);
            foreach (PrimeLevelInfo primeLevel in PrimeLevels)
                primeLevel.Write(writer);
        }
    }
}
