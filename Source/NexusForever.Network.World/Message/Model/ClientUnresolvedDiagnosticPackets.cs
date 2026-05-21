using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.Client0x003D)]
    public class Client0x003D : IReadable
    {
        public byte[] Payload { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Payload = reader.ReadBytes(0x18u);
        }
    }

    [Message(GameMessageOpcode.Client0x00C8)]
    public class Client0x00C8 : IReadable
    {
        public uint Value { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    [Message(GameMessageOpcode.Client0x00ED)]
    public class Client0x00ED : IReadable
    {
        public byte[] Payload { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Payload = reader.ReadBytes(0x20u);
        }
    }

    [Message(GameMessageOpcode.Client0x011B)]
    public class Client0x011B : IReadable
    {
        public byte Value { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Value = reader.ReadByte();
        }
    }

    [Message(GameMessageOpcode.Client0x011D)]
    public class Client0x011D : IReadable
    {
        public uint Value { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    [Message(GameMessageOpcode.Client0x012D)]
    public class Client0x012D : IReadable
    {
        public ulong Value { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Value = reader.ReadULong();
        }
    }

    [Message(GameMessageOpcode.Client0x0142)]
    public class Client0x0142 : IReadable
    {
        public byte[] Payload { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Payload = reader.ReadBytes(0x10u);
        }
    }

    [Message(GameMessageOpcode.Client0x0550)]
    public class Client0x0550 : IReadable
    {
        public uint Value { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    [Message(GameMessageOpcode.Client0x062A)]
    public class Client0x062A : IReadable
    {
        public uint Value { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    [Message(GameMessageOpcode.Client0x0634)]
    public class Client0x0634 : IReadable
    {
        public uint Value { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    [Message(GameMessageOpcode.Client0x063E)]
    public class Client0x063E : IReadable
    {
        public string Text { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Text = reader.ReadWideString();
        }
    }

    [Message(GameMessageOpcode.Client0x0701)]
    public class Client0x0701 : IReadable
    {
        public ulong Value { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Value = reader.ReadULong();
        }
    }

    [Message(GameMessageOpcode.Client0x0760)]
    public class Client0x0760 : IReadable
    {
        public byte[] Payload { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Payload = reader.ReadBytes(0x58u);
        }
    }

    [Message(GameMessageOpcode.Client0x0762)]
    public class Client0x0762 : IReadable
    {
        public byte[] Payload { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Payload = reader.ReadBytes(0x10u);
        }
    }

    [Message(GameMessageOpcode.Client0x07B6)]
    public class Client0x07B6 : IReadable
    {
        public byte[] Payload { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Payload = reader.ReadBytes(0x20u);
        }
    }

    [Message(GameMessageOpcode.Client0x07E3)]
    public class Client0x07E3 : IReadable
    {
        public uint Value { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    [Message(GameMessageOpcode.Client0x0928)]
    public class Client0x0928 : IReadable
    {
        public ulong Value { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Value = reader.ReadULong();
        }
    }
}
