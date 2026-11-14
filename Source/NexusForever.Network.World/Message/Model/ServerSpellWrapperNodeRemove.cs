using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Spell wrapper node removal follow-up for opcode <c>0x0819</c>.
    /// Verified native: reader <c>14007a040</c>; dispatcher <c>1403ee3af</c> resolves field one as the wrapper ID
    /// and matches field zero against receiving-unit target nodes. Helper <c>140718af0</c> runs only for
    /// Spell4Base CastMethod 10, pruning flag-2 active children while preserving finite children.
    /// Original server emission policy remains unqualified; no retail parity claim.
    /// </summary>
    [Message(GameMessageOpcode.ServerSpellWrapperNodeRemove)]
    public class ServerSpellWrapperNodeRemove : IWritable
    {
        public uint NodeEntityId { get; set; }
        public uint SpellWrapperId { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(NodeEntityId);
            writer.Write(SpellWrapperId);
        }
    }
}
