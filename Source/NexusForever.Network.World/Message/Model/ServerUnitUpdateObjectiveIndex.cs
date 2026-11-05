using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Updates the cached objective index of an existing non-local unit.
    /// </summary>
    /// <remarks>Build 16042 reads two 32-bit fields at <c>0x14007A040</c>. Server emission timing remains open.</remarks>
    [Message(GameMessageOpcode.ServerUnitUpdateObjectiveIndex)]
    public class ServerUnitUpdateObjectiveIndex : IWritable
    {
        public uint UnitId { get; set; }
        public uint ObjectiveIndex { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(UnitId);
            writer.Write(ObjectiveIndex);
        }
    }
}
