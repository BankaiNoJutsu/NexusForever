using NexusForever.Network.Message;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerSpellCastPositionSync)]
    public class ServerSpellCastPositionSync : IWritable
    {
        // Legacy raw field names remain compatible. Native 0x07FD promotes a pending
        // client cast token to its assigned server cast ID, then applies this pose.
        public uint Time { get; set; }
        public uint CastingId { get; set; }
        public uint CasterId { get; set; }
        public Position Position { get; set; } = new Position();
        public uint Unknown16 { get; set; }

        public uint ContextToken { get => Time; set => Time = value; }
        public uint PrimaryTargetId { get => CasterId; set => CasterId = value; }
        public float Yaw
        {
            get => BitConverter.UInt32BitsToSingle(Unknown16);
            set => Unknown16 = BitConverter.SingleToUInt32Bits(value);
        }

        public List<InitialPosition> InitialPositionData { get; set; } = new();
        public List<TelegraphPosition> TelegraphPositionData { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Time);
            writer.Write(CastingId);
            writer.Write(CasterId);
            Position.Write(writer);
            writer.Write(Unknown16);

            writer.Write(InitialPositionData.Count, 8u);
            InitialPositionData.ForEach(u => u.Write(writer));

            writer.Write(TelegraphPositionData.Count, 8u);
            TelegraphPositionData.ForEach(u => u.Write(writer));
        }
    }
}
