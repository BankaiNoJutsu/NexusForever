using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Values from this packet are stored against the entity object in the client.
    /// This is different from <see cref="ServerEntityThreatListUpdate"/> where values are stored in a "global" threat list which is only used for the current target.
    /// </summary>
    /// <remarks>Native registration (<c>Network_RegisterServerOpcode_090A</c> @ <c>1400751d4</c>) uses
    /// <c>ServerSpellUInt32TripletListRow_ReadPayload</c> @ <c>140080bf0</c> (three uint32 fields), shared with
    /// <see cref="ServerEntityTargetUnit"/> and entity-stat aux <c>0x0889</c>.</remarks>
    [Message(GameMessageOpcode.ServerEntityThreatUpdate)]
    public class ServerEntityThreatUpdate : IWritable
    {
        public uint UnitId { get; set; }
        public uint TargetId { get; set; }
        public uint ThreatLevel { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(UnitId);
            writer.Write(TargetId);
            writer.Write(ThreatLevel);
        }
    }
}
