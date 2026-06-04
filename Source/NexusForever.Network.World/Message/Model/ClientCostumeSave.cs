using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

using NexusForever.Game.Static.Costume;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientCostumeSave)]
    public class ClientCostumeSave : IReadable
    {
        public class CostumeItem : IReadable
        {
            public uint ItemId { get; private set; }
            public uint[] Dyes { get; } = new uint[3];

            public void Read(GamePacketReader reader)
            {
                ItemId = reader.ReadUInt(18u);
                for (int i = 0; i < 3; i++)
                    Dyes[i] = reader.ReadUInt();
            }
        }

        public int Index { get; private set; }
        public CostumeType Type { get; private set; }
        public ulong ResidenceId { get; private set; }
        public List<CostumeItem> Items { get; } = new();
        public uint VisibilityMask { get; private set; }
        public bool Token { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Index       = reader.ReadInt();
            Type        = reader.ReadEnum<CostumeType>(2u);
            ResidenceId = reader.ReadULong();

            for (int i = 0; i < Costume.MaxCostumeItems; i++)
            {
                var part = new CostumeItem();
                part.Read(reader);
                Items.Add(part);
            }

            VisibilityMask = reader.ReadUInt();
            Token = reader.ReadBit();
        }
    }
}
