using NexusForever.Game.Static.Entity;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerRewardPropertySet)]
    public class ServerRewardPropertySet : IWritable
    {
        public class RewardProperty : IWritable
        {
            public class SubRewardProperty : IWritable
            {
                public byte Id { get; set; }
                public uint Data { get; set; }
                public RewardPropertyModifierValueType Type { get; set; }
                public float Value { get; set; }

                public void Write(GamePacketWriter writer)
                {
                    writer.Write(Id, 4u);
                    writer.Write(Data);
                    writer.Write(Type, 2u);

                    switch (Type)
                    {
                        case RewardPropertyModifierValueType.AdditiveScalar:
                        case RewardPropertyModifierValueType.MultiplicativeScalar:
                            writer.Write(Value);
                            break;
                        case RewardPropertyModifierValueType.Discrete:
                            writer.Write((uint)Value);
                            break;
                    }
                }
            }

            public RewardPropertyType Id { get; set; }
            public uint Data { get; set; }
            public RewardPropertyModifierValueType Type { get; set; }
            public float Value { get; set; }
            public List<SubRewardProperty> SubRewardProperties { get; set; } = new();

            public void Write(GamePacketWriter writer)
            {
                writer.Write(Id, 6u);
                writer.Write(Data);
                writer.Write(Type, 2u);

                switch (Type)
                {
                    case RewardPropertyModifierValueType.AdditiveScalar:
                    case RewardPropertyModifierValueType.MultiplicativeScalar:
                        writer.Write(Value);
                        break;
                    case RewardPropertyModifierValueType.Discrete:
                        writer.Write((uint)Value);
                        break;
                }

                writer.Write(SubRewardProperties.Count, 8u);
                SubRewardProperties.ForEach(u => u.Write(writer));
            }
        }

        public List<RewardProperty> Properties { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write((byte)Properties.Count);
            Properties.ForEach(u => u.Write(writer));
        }
    }
}
