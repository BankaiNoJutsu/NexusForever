using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Abilities;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Historical placeholder for opcode <c>0x0818</c>.
    /// Current client evidence reads one leading <see cref="uint"/> followed by one <see cref="ServerSpellList.TierEntry"/> payload,
    /// and no longer supports the earlier <c>TargetInfo</c> assumption.
    /// </summary>
    [Message(GameMessageOpcode.ServerSpellTargetInfo)]
    public class ServerSpellTargetInfo : IWritable
    {
       public uint LeadingValue { get; set; }
       public ServerSpellList.TierEntry TierEntry { get; set; } = new();

       public void Write(GamePacketWriter writer)
       {
           writer.Write(LeadingValue);
           TierEntry.Write(writer);
       }
    }
}
