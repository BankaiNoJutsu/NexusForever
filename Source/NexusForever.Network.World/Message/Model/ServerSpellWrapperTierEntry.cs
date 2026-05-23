using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Abilities;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Spell wrapper tier-entry follow-up for opcode <c>0x0818</c>.
    /// Client reader <c>ServerOpcode0818_ReadLeadingUInt32AndTierEntry</c> @ <c>140095e60</c> reads one leading
    /// <see cref="uint"/> then one <see cref="ServerSpellList.TierEntry"/>; consumer path <c>SpellWrapper_ApplyEntityVariantTierEntryAndBroadcast</c> @ <c>1403ee403</c>.
    /// The leading field is treated as a slot or index candidate, not a casting id.
    /// </summary>
    [Message(GameMessageOpcode.ServerSpellWrapperTierEntry)]
    public class ServerSpellWrapperTierEntry : IWritable
    {
        public uint SlotOrIndex { get; set; }
        public ServerSpellList.TierEntry TierEntry { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(SlotOrIndex);
            TierEntry.Write(writer);
        }
    }
}
