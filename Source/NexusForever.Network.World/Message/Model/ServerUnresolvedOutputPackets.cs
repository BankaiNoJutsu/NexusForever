using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    public abstract class ServerUnresolvedRawPayload : IWritable
    {
        public byte[] Payload { get; }

        protected ServerUnresolvedRawPayload(uint expectedLength, byte[] payload = null)
        {
            Payload = payload ?? new byte[checked((int)expectedLength)];
            if (Payload.Length != expectedLength)
                throw new ArgumentException($"Payload must be exactly {expectedLength} bytes.", nameof(payload));
        }

        public void Write(GamePacketWriter writer)
        {
            writer.WriteBytes(Payload);
        }
    }

    public abstract class ServerUnresolvedUIntPayload : IWritable
    {
        private readonly uint bits;

        public uint Value { get; set; }

        protected ServerUnresolvedUIntPayload(uint bits, uint value = 0u)
        {
            this.bits = bits;
            Value     = value;
        }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value, bits);
        }
    }

    public abstract class ServerUnresolvedUIntFlagPayload : IWritable
    {
        public uint Value { get; set; }
        public bool Flag { get; set; }

        protected ServerUnresolvedUIntFlagPayload(uint value = 0u, bool flag = false)
        {
            Value = value;
            Flag  = flag;
        }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value);
            writer.Write(Flag);
        }
    }

    [Message(GameMessageOpcode.Server0x00B0)]
    public class Server0x00B0 : ServerUnresolvedUIntPayload
    {
        public Server0x00B0(uint value = 0u) : base(18u, value) { }
    }

    [Message(GameMessageOpcode.Server0x00B7)]
    public class Server0x00B7 : ServerUnresolvedRawPayload
    {
        public Server0x00B7(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x00CA)]
    public class Server0x00CA : ServerUnresolvedRawPayload
    {
        public Server0x00CA(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x00CB)]
    public class Server0x00CB : ServerUnresolvedRawPayload
    {
        public Server0x00CB(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x00CC)]
    public class Server0x00CC : ServerUnresolvedRawPayload
    {
        public Server0x00CC(byte[] payload = null) : base(0x4u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x00CD)]
    public class Server0x00CD : ServerUnresolvedRawPayload
    {
        public Server0x00CD(byte[] payload = null) : base(0x4u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x00CE)]
    public class Server0x00CE : ServerUnresolvedRawPayload
    {
        public Server0x00CE(byte[] payload = null) : base(0x8u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x00D1)]
    public class Server0x00D1 : ServerUnresolvedRawPayload
    {
        public Server0x00D1(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x00DF)]
    public class Server0x00DF : ServerUnresolvedRawPayload
    {
        public Server0x00DF(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x00EE)]
    public class Server0x00EE : ServerUnresolvedRawPayload
    {
        public Server0x00EE(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0101)]
    public class Server0x0101 : ServerUnresolvedRawPayload
    {
        public Server0x0101(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x010D)]
    public class Server0x010D : ServerUnresolvedRawPayload
    {
        public Server0x010D(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0110)]
    public class Server0x0110 : ServerUnresolvedRawPayload
    {
        public Server0x0110(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0139)]
    public class Server0x0139 : ServerUnresolvedRawPayload
    {
        public Server0x0139(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0143)]
    public class Server0x0143 : ServerUnresolvedRawPayload
    {
        public Server0x0143(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x014D)]
    public class Server0x014D : ServerUnresolvedRawPayload
    {
        public Server0x014D(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0157)]
    public class Server0x0157 : ServerUnresolvedRawPayload
    {
        public Server0x0157(byte[] payload = null) : base(0x8u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0160)]
    public class Server0x0160 : ServerUnresolvedRawPayload
    {
        public Server0x0160(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x016B)]
    public class Server0x016B : ServerUnresolvedRawPayload
    {
        public Server0x016B(byte[] payload = null) : base(0x20u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x016D)]
    public class Server0x016D : ServerUnresolvedRawPayload
    {
        public Server0x016D(byte[] payload = null) : base(0x4u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x016E)]
    public class Server0x016E : ServerUnresolvedRawPayload
    {
        public Server0x016E(byte[] payload = null) : base(0x4u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0181)]
    public class Server0x0181 : ServerUnresolvedRawPayload
    {
        public Server0x0181(byte[] payload = null) : base(0x4u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0183)]
    public class Server0x0183 : ServerUnresolvedUIntFlagPayload
    {
        public Server0x0183(uint value = 0u, bool flag = false) : base(value, flag) { }
    }

    [Message(GameMessageOpcode.Server0x0186)]
    public class Server0x0186 : ServerUnresolvedRawPayload
    {
        public Server0x0186(byte[] payload = null) : base(0x4u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0187)]
    public class Server0x0187 : ServerUnresolvedRawPayload
    {
        public Server0x0187(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x019A)]
    public class Server0x019A : ServerUnresolvedRawPayload
    {
        public Server0x019A(byte[] payload = null) : base(0x8u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x019C)]
    public class Server0x019C : ServerUnresolvedRawPayload
    {
        public Server0x019C(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x01A4)]
    public class Server0x01A4 : ServerUnresolvedRawPayload
    {
        public Server0x01A4(byte[] payload = null) : base(0x14u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x01A6)]
    public class Server0x01A6 : ServerUnresolvedRawPayload
    {
        public Server0x01A6(byte[] payload = null) : base(0x4u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x01A7)]
    public class Server0x01A7 : ServerUnresolvedRawPayload
    {
        public Server0x01A7(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x01A8)]
    public class Server0x01A8 : ServerUnresolvedRawPayload
    {
        public Server0x01A8(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x01A9)]
    public class Server0x01A9 : ServerUnresolvedRawPayload
    {
        public Server0x01A9(byte[] payload = null) : base(0x8u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x01B2)]
    public class Server0x01B2 : ServerUnresolvedRawPayload
    {
        public Server0x01B2(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x01B8)]
    public class Server0x01B8 : ServerUnresolvedRawPayload
    {
        public Server0x01B8(byte[] payload = null) : base(0x68u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x01C1)]
    public class Server0x01C1 : ServerUnresolvedRawPayload
    {
        public Server0x01C1(byte[] payload = null) : base(0x20u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x01C4)]
    public class Server0x01C4 : ServerUnresolvedRawPayload
    {
        public Server0x01C4(byte[] payload = null) : base(0x20u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x01EF)]
    public class Server0x01EF : ServerUnresolvedRawPayload
    {
        public Server0x01EF(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x025F)]
    public class Server0x025F : ServerUnresolvedRawPayload
    {
        public Server0x025F(byte[] payload = null) : base(0x2Cu, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0260)]
    public class Server0x0260 : ServerUnresolvedRawPayload
    {
        public Server0x0260(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0261)]
    public class Server0x0261 : ServerUnresolvedRawPayload
    {
        public Server0x0261(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0263)]
    public class Server0x0263 : ServerUnresolvedRawPayload
    {
        public Server0x0263(byte[] payload = null) : base(0x24u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0264)]
    public class Server0x0264 : ServerUnresolvedRawPayload
    {
        public Server0x0264(byte[] payload = null) : base(0x18u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x026A)]
    public class Server0x026A : ServerUnresolvedRawPayload
    {
        public Server0x026A(byte[] payload = null) : base(0x50u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0347)]
    public class Server0x0347 : ServerUnresolvedRawPayload
    {
        public Server0x0347(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0348)]
    public class Server0x0348 : ServerUnresolvedRawPayload
    {
        public Server0x0348(byte[] payload = null) : base(0x30u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x034C)]
    public class Server0x034C : ServerUnresolvedRawPayload
    {
        public Server0x034C(byte[] payload = null) : base(0x1Cu, payload) { }
    }

    [Message(GameMessageOpcode.Server0x034D)]
    public class Server0x034D : ServerUnresolvedRawPayload
    {
        public Server0x034D(byte[] payload = null) : base(0x40u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x034E)]
    public class Server0x034E : ServerUnresolvedRawPayload
    {
        public Server0x034E(byte[] payload = null) : base(0x28u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0351)]
    public class Server0x0351 : ServerUnresolvedRawPayload
    {
        public Server0x0351(byte[] payload = null) : base(0x20u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x037F)]
    public class Server0x037F : ServerUnresolvedRawPayload
    {
        public Server0x037F(byte[] payload = null) : base(0x18u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x03EF)]
    public class Server0x03EF : ServerUnresolvedRawPayload
    {
        public Server0x03EF(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0414)]
    public class Server0x0414 : ServerUnresolvedRawPayload
    {
        public Server0x0414(byte[] payload = null) : base(0x18u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x042A)]
    public class Server0x042A : ServerUnresolvedRawPayload
    {
        public Server0x042A(byte[] payload = null) : base(0xCu, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0431)]
    public class Server0x0431 : ServerUnresolvedRawPayload
    {
        public Server0x0431(byte[] payload = null) : base(0x18u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0436)]
    public class Server0x0436 : ServerUnresolvedRawPayload
    {
        public Server0x0436(byte[] payload = null) : base(0x60u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0438)]
    public class Server0x0438 : ServerUnresolvedRawPayload
    {
        public Server0x0438(byte[] payload = null) : base(0x28u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0441)]
    public class Server0x0441 : ServerUnresolvedRawPayload
    {
        public Server0x0441(byte[] payload = null) : base(0x38u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x045A)]
    public class Server0x045A : ServerUnresolvedRawPayload
    {
        public Server0x045A(byte[] payload = null) : base(0x20u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0461)]
    public class Server0x0461 : ServerUnresolvedRawPayload
    {
        public Server0x0461(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0468)]
    public class Server0x0468 : ServerUnresolvedRawPayload
    {
        public Server0x0468(byte[] payload = null) : base(0x28u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x04FE)]
    public class Server0x04FE : ServerUnresolvedRawPayload
    {
        public Server0x04FE(byte[] payload = null) : base(0x18u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0501)]
    public class Server0x0501 : ServerUnresolvedRawPayload
    {
        public Server0x0501(byte[] payload = null) : base(0x30u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0506)]
    public class Server0x0506 : ServerUnresolvedRawPayload
    {
        public Server0x0506(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0514)]
    public class Server0x0514 : ServerUnresolvedRawPayload
    {
        public Server0x0514(byte[] payload = null) : base(0x18u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0516)]
    public class Server0x0516 : ServerUnresolvedRawPayload
    {
        public Server0x0516(byte[] payload = null) : base(0x30u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0519)]
    public class Server0x0519 : ServerUnresolvedRawPayload
    {
        public Server0x0519(byte[] payload = null) : base(0x30u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x051F)]
    public class Server0x051F : ServerUnresolvedRawPayload
    {
        public Server0x051F(byte[] payload = null) : base(0x18u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x053A)]
    public class Server0x053A : ServerUnresolvedRawPayload
    {
        public Server0x053A(byte[] payload = null) : base(0x20u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x053B)]
    public class Server0x053B : ServerUnresolvedRawPayload
    {
        public Server0x053B(byte[] payload = null) : base(0x20u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0567)]
    public class Server0x0567 : ServerUnresolvedRawPayload
    {
        public Server0x0567(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x056B)]
    public class Server0x056B : ServerUnresolvedRawPayload
    {
        public Server0x056B(byte[] payload = null) : base(0x20u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x056C)]
    public class Server0x056C : ServerUnresolvedRawPayload
    {
        public Server0x056C(byte[] payload = null) : base(0x28u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x056D)]
    public class Server0x056D : ServerUnresolvedRawPayload
    {
        public Server0x056D(byte[] payload = null) : base(0x18u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x05A1)]
    public class Server0x05A1 : ServerUnresolvedRawPayload
    {
        public Server0x05A1(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x06DF)]
    public class Server0x06DF : ServerUnresolvedRawPayload
    {
        public Server0x06DF(byte[] payload = null) : base(0x20u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x06F7)]
    public class Server0x06F7 : ServerUnresolvedRawPayload
    {
        public Server0x06F7(byte[] payload = null) : base(0x8u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0718)]
    public class Server0x0718 : ServerUnresolvedRawPayload
    {
        public Server0x0718(byte[] payload = null) : base(0x20u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x074A)]
    public class Server0x074A : ServerUnresolvedRawPayload
    {
        public Server0x074A(byte[] payload = null) : base(0x18u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x077E)]
    public class Server0x077E : ServerUnresolvedRawPayload
    {
        public Server0x077E(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x078C)]
    public class Server0x078C : ServerUnresolvedRawPayload
    {
        public Server0x078C(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x07CD)]
    public class Server0x07CD : ServerUnresolvedRawPayload
    {
        public Server0x07CD(byte[] payload = null) : base(0x28u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x07D3)]
    public class Server0x07D3 : ServerUnresolvedRawPayload
    {
        public Server0x07D3(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x07D5)]
    public class Server0x07D5 : ServerUnresolvedRawPayload
    {
        public Server0x07D5(byte[] payload = null) : base(0x14u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x080F)]
    public class Server0x080F : ServerUnresolvedRawPayload
    {
        public Server0x080F(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0810)]
    public class Server0x0810 : ServerUnresolvedRawPayload
    {
        public Server0x0810(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0812)]
    public class Server0x0812 : ServerUnresolvedRawPayload
    {
        public Server0x0812(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0846)]
    public class Server0x0846 : ServerUnresolvedRawPayload
    {
        public Server0x0846(byte[] payload = null) : base(0x4u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x084B)]
    public class Server0x084B : ServerUnresolvedRawPayload
    {
        public Server0x084B(byte[] payload = null) : base(0x18u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0855)]
    public class Server0x0855 : ServerUnresolvedRawPayload
    {
        public Server0x0855(byte[] payload = null) : base(0xCu, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0889)]
    public class Server0x0889 : ServerUnresolvedRawPayload
    {
        public Server0x0889(byte[] payload = null) : base(0xCu, payload) { }
    }

    [Message(GameMessageOpcode.Server0x08A0)]
    public class Server0x08A0 : ServerUnresolvedRawPayload
    {
        public Server0x08A0(byte[] payload = null) : base(0x48u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x08A8)]
    public class Server0x08A8 : ServerUnresolvedRawPayload
    {
        public Server0x08A8(byte[] payload = null) : base(0x14u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x08CC)]
    public class Server0x08CC : ServerUnresolvedRawPayload
    {
        public Server0x08CC(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x08F4)]
    public class Server0x08F4 : ServerUnresolvedRawPayload
    {
        public Server0x08F4(byte[] payload = null) : base(0xCu, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0939)]
    public class Server0x0939 : ServerUnresolvedRawPayload
    {
        public Server0x0939(byte[] payload = null) : base(0x18u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x093D)]
    public class Server0x093D : ServerUnresolvedRawPayload
    {
        public Server0x093D(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x093E)]
    public class Server0x093E : ServerUnresolvedRawPayload
    {
        public Server0x093E(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0969)]
    public class Server0x0969 : ServerUnresolvedRawPayload
    {
        public Server0x0969(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x096A)]
    public class Server0x096A : ServerUnresolvedRawPayload
    {
        public Server0x096A(byte[] payload = null) : base(0x30u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x096B)]
    public class Server0x096B : ServerUnresolvedRawPayload
    {
        public Server0x096B(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x096C)]
    public class Server0x096C : ServerUnresolvedRawPayload
    {
        public Server0x096C(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x096E)]
    public class Server0x096E : ServerUnresolvedRawPayload
    {
        public Server0x096E(byte[] payload = null) : base(0x1Cu, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0971)]
    public class Server0x0971 : ServerUnresolvedRawPayload
    {
        public Server0x0971(byte[] payload = null) : base(0x8u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0976)]
    public class Server0x0976 : ServerUnresolvedRawPayload
    {
        public Server0x0976(byte[] payload = null) : base(0x60u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0977)]
    public class Server0x0977 : ServerUnresolvedRawPayload
    {
        public Server0x0977(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0978)]
    public class Server0x0978 : ServerUnresolvedRawPayload
    {
        public Server0x0978(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x097A)]
    public class Server0x097A : ServerUnresolvedRawPayload
    {
        public Server0x097A(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x097B)]
    public class Server0x097B : ServerUnresolvedRawPayload
    {
        public Server0x097B(byte[] payload = null) : base(0x4u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x097D)]
    public class Server0x097D : ServerUnresolvedRawPayload
    {
        public Server0x097D(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x097E)]
    public class Server0x097E : ServerUnresolvedRawPayload
    {
        public Server0x097E(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0980)]
    public class Server0x0980 : ServerUnresolvedRawPayload
    {
        public Server0x0980(byte[] payload = null) : base(0x4u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0986)]
    public class Server0x0986 : ServerUnresolvedRawPayload
    {
        public Server0x0986(byte[] payload = null) : base(0x8u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0989)]
    public class Server0x0989 : ServerUnresolvedRawPayload
    {
        public Server0x0989(byte[] payload = null) : base(0x1u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x098A)]
    public class Server0x098A : ServerUnresolvedRawPayload
    {
        public Server0x098A(byte[] payload = null) : base(0x4u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x098C)]
    public class Server0x098C : ServerUnresolvedRawPayload
    {
        public Server0x098C(byte[] payload = null) : base(0x8u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x098D)]
    public class Server0x098D : ServerUnresolvedRawPayload
    {
        public Server0x098D(byte[] payload = null) : base(0x8u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x098E)]
    public class Server0x098E : ServerUnresolvedRawPayload
    {
        public Server0x098E(byte[] payload = null) : base(0x10u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x098F)]
    public class Server0x098F : ServerUnresolvedRawPayload
    {
        public Server0x098F(byte[] payload = null) : base(0x20u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0990)]
    public class Server0x0990 : ServerUnresolvedRawPayload
    {
        public Server0x0990(byte[] payload = null) : base(0x8u, payload) { }
    }

    [Message(GameMessageOpcode.Server0x0991)]
    public class Server0x0991 : ServerUnresolvedRawPayload
    {
        public Server0x0991(byte[] payload = null) : base(0x60u, payload) { }
    }
}
