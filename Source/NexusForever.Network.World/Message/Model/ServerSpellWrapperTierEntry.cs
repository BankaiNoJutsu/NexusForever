using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Abilities;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Spell wrapper tier-entry follow-up for opcode <c>0x0818</c>.
    /// Client reader <c>ServerOpcode0818_ReadSpellWrapperIdAndTierEntry</c> @ <c>140095e60</c> reads
    /// <see cref="SpellWrapperId"/> then one <see cref="ServerSpellList.TierEntry"/>. Dispatcher case
    /// <c>Opcode0818_DispatchWrapperTierEntry</c> @ <c>1403ee403</c> resolves the wrapper id before applying
    /// the nested tier-entry payload.
    /// </summary>
    [Message(GameMessageOpcode.ServerSpellWrapperTierEntry)]
    public class ServerSpellWrapperTierEntry : IWritable
    {
        public uint SpellWrapperId { get; set; }
        public ServerSpellList.TierEntry TierEntry { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(SpellWrapperId);
            TierEntry.Write(writer);
        }
    }
}
