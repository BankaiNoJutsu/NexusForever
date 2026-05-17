using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Spell broadcast follow-up carrying one <see cref="TargetInfo"/> payload.
    /// Current evidence places this near NPC-created telegraph buff cases, but the exact trigger is still under investigation.
    /// </summary>
    [Message(GameMessageOpcode.ServerSpellTargetInfo)]
    public class ServerSpellTargetInfo : IWritable
    {
       public uint CastingId { get; set; }
       public TargetInfo TargetInfo { get; set; }

       public void Write(GamePacketWriter writer)
       {
           writer.Write(CastingId);
           TargetInfo.Write(writer);
       }
    }
}
