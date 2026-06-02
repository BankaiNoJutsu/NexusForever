using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Values from this packet are handled in the same client function as <see cref="ServerEntityThreatUpdate"/>.
    /// Additionally it will set the target id on the entity object in the client.
    /// </summary>
    /// <remarks>Native registration (<c>Network_RegisterServerOpcode_0908</c> @ <c>1400751a3</c>) uses
    /// <c>ServerSpellUInt32TripletListRow_ReadPayload</c> @ <c>140080bf0</c> (three uint32 fields), shared with
    /// <see cref="ServerEntityThreatUpdate"/> and entity-stat aux <c>0x0889</c>.</remarks>
    [Message(GameMessageOpcode.ServerEntityTargetUnit)]
    public class ServerEntityTargetUnit : IWritable
    {
        public uint UnitId { get; set; }
        public uint NewTargetId { get; set; }
        public uint ThreatLevel { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(UnitId);
            writer.Write(NewTargetId);
            writer.Write(ThreatLevel);
        }
    }
}
