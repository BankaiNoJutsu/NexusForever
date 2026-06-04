using NexusForever.Network.Message;

using NexusForever.Game.Static.Costume;

namespace NexusForever.Network.World.Message.Model.Shared
{
    public class Costume : IWritable
    {
        public const byte MaxCostumeItems = 7;

        public uint Index { get; set; }
        public uint VisibilityMask { get; set; }
        public CostumeType Type { get; set; }
        public uint[] Item2Ids { get; set; } = new uint[7];
        public uint[] DyeData { get; set; } = new uint[7];

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Index);
            writer.Write(VisibilityMask);
            writer.Write(Type, 2u);

            for (int i = 0; i < 7; i++)
                writer.Write(Item2Ids[i]);
            for (int i = 0; i < 7; i++)
                writer.Write(DyeData[i]);
        }
    }
}
