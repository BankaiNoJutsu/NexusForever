using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;
using System.Numerics;

namespace NexusForever.Network.World.Message.Model.Crafting
{
    [Message(GameMessageOpcode.ServerCraftingCurrentCraft)]
    public class ServerCraftingCurrentCraft : IWritable
    {
        /// <summary>
        /// Native reader <c>ServerCraftingCurrentCraft_ReadPayload</c> (<c>1400a46b0</c>) maps
        /// opcode <c>0x0854</c> as schematic id, packed craft stats, glyph and count fields,
        /// five modifier item ids, crafted item id, and discovery coordinate/vector/radius data.
        /// </summary>
        public uint TradeskillSchematic2Id { get; set; }
        public CraftStats CraftStats { get; set; } = new CraftStats();
        /// <summary>
        /// Reader-confirmed current-craft glyph field. Keep separate from persisted item rune slots
        /// until a producer maps how crafting state populates it.
        /// </summary>
        public uint GlyphData { get; set; }
        public uint SchematicCount { get; set; }
        public uint AdditiveCount { get; set; }
        public uint Unused { get; set; } = 0;
        public uint[] Item2IdModifiers { get; set; } = new uint[5];
        public uint Item2Id { get; set; }
        public Vector2 DiscoveryCoordinates { get; set; }
        public Vector2 DiscoveryVectorMultiplier { get; set; }
        public float DiscoveryRadiusMultiplier { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(TradeskillSchematic2Id, 15);
            CraftStats.Write(writer);
            writer.Write(GlyphData);
            writer.Write(SchematicCount);
            writer.Write(AdditiveCount);
            writer.Write(Unused);
            foreach (var id in Item2IdModifiers)
            {
                writer.Write(id);
            }
            writer.Write(Item2Id, 18);
            writer.Write(DiscoveryCoordinates.X);
            writer.Write(DiscoveryCoordinates.Y);
            writer.Write(DiscoveryVectorMultiplier.X);
            writer.Write(DiscoveryVectorMultiplier.Y);
            writer.Write(DiscoveryRadiusMultiplier);
        }
    }
}
