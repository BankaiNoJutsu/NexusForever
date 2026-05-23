using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Spell wrapper node removal follow-up for opcode <c>0x0819</c>.
    /// Client reader <c>ServerTwoUInt32_ReadPayload</c> @ <c>14007a040</c>; consumer path <c>SpellWrapperNode_PruneChildrenAndRefresh</c> @ <c>1403ee3af</c>
    /// matches <see cref="NodeEntityId"/> against wrapper nodes and resolves <see cref="SpellWrapperId"/> from payload field one.
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
